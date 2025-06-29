using System;
using System.Collections.Generic;
using System.Linq;
using ETABSv1;
using LoadData; 

namespace LoadData
{
    public class LoadSynchronizationManager
    {
        private LoadSyncRegistry _registry;
        private cSapModel _sapModel;

        public LoadSynchronizationManager(string syncFilePath)
        {
            _registry = new LoadSyncRegistry(syncFilePath);
        }

        public SyncReport SyncFromRevitToEtabs(List<LoadAssignment> revitLoads, cSapModel sapModel)
        {
            _sapModel = sapModel;
            SyncReport report = new SyncReport();

            try
            {
                // STEP 1: Create load patterns first
                CreateLoadPatterns(revitLoads);

                // STEP 2: Apply loads
                foreach (var load in revitLoads)
                {
                    try
                    {
                        ApplyLoadToEtabs(load);
                        report.NewLoads++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying load {load.UniqueIdentifier}: {ex.Message}");
                        // Don't throw, just log and continue with next load
                    }
                }

                report.TotalLoads = revitLoads.Count;
                _registry.SaveRegistry();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error in sync: {ex.Message}");
                throw; // Re-throw critical errors
            }

            return report;
        }

        private void CreateLoadPatterns(List<LoadAssignment> loads)
        {
            try
            {
                HashSet<string> uniquePatterns = new HashSet<string>();

                // Collect unique load patterns
                foreach (var load in loads)
                {
                    if (!string.IsNullOrEmpty(load.LoadPattern))
                    {
                        uniquePatterns.Add(load.LoadPattern);
                    }
                }

                // Get existing patterns from ETABS
                int patternCount = 0;
                string[] existingPatterns = null;
                int ret = _sapModel.LoadPatterns.GetNameList(ref patternCount, ref existingPatterns);

                HashSet<string> existingSet = new HashSet<string>(existingPatterns ?? new string[0]);

                // Create missing patterns
                foreach (string pattern in uniquePatterns)
                {
                    if (!existingSet.Contains(pattern))
                    {
                        eLoadPatternType patternType = DeterminePatternType(pattern);
                        ret = _sapModel.LoadPatterns.Add(pattern, patternType);

                        if (ret != 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Warning: Failed to create load pattern '{pattern}', error code: {ret}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Created load pattern: {pattern}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating load patterns: {ex.Message}");
                throw; // This is critical - re-throw
            }
        }

        private eLoadPatternType DeterminePatternType(string patternName)
        {
            string pattern = patternName.ToUpper();

            if (pattern.Contains("DEAD") || pattern.Contains("DL") || pattern.Contains("D"))
                return eLoadPatternType.Dead;
            else if (pattern.Contains("LIVE") || pattern.Contains("LL") || pattern.Contains("L"))
                return eLoadPatternType.Live;
            else if (pattern.Contains("WIND") || pattern.Contains("W"))
                return eLoadPatternType.Wind;
            else if (pattern.Contains("SEISMIC") || pattern.Contains("EARTHQUAKE") || pattern.Contains("EQ") || pattern.Contains("E"))
                return eLoadPatternType.Quake;
            else if (pattern.Contains("SNOW") || pattern.Contains("S"))
                return eLoadPatternType.Snow;
            else
                return eLoadPatternType.Other;
        }

        private List<LoadAssignment> ExtractLoadsFromEtabs()
        {
            return new List<LoadAssignment>();
        }

        private void ApplyLoadToEtabs(LoadAssignment load)
        {
            if (_sapModel == null)
            {
                throw new InvalidOperationException("ETABS model is not initialized.");
            }

            // REVERTED: Back to the original working direction mapping
            int etabsDir = 0;

            if (load.LoadType.Contains("Load") && !load.LoadType.Contains("Moment"))
            {
                etabsDir = load.Dir + 4;
            }
            else if (load.LoadType.Contains("MomentLoad"))
            {
                etabsDir = load.Dir + 10;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Unknown load type for direction mapping: {load.LoadType}");
                etabsDir = load.Dir + 4;
            }

            if (IsFrameElement(load.ElementID))
            {
                ApplyFrameLoad(load, etabsDir);
            }
            else if (IsAreaElement(load.ElementID))
            {
                ApplyAreaLoad(load, etabsDir);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Warning: Unknown element type for {load.ElementID}");
            }
        }

        private bool IsFrameElement(string elementID)
        {
            try
            {
                int frameCount = 0;
                string[] frameNames = null;
                _sapModel.FrameObj.GetNameList(ref frameCount, ref frameNames);

                if (frameNames != null)
                {
                    return frameNames.Contains(elementID);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking frame element {elementID}: {ex.Message}");
            }
            return false;
        }

        private bool IsAreaElement(string elementID)
        {
            try
            {
                int areaCount = 0;
                string[] areaNames = null;
                _sapModel.AreaObj.GetNameList(ref areaCount, ref areaNames);

                if (areaNames != null)
                {
                    return areaNames.Contains(elementID);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking area element {elementID}: {ex.Message}");
            }
            return false;
        }

        private void ApplyFrameLoad(LoadAssignment load, int etabsDir)
        {
            try
            {
                if (load.LoadType.Contains("Point"))
                {
                    // Apply Point Load to Frame
                    int myType = load.LoadType.Contains("Moment") ? 2 : 1; // 1=Force, 2=Moment
                    double distance = load.DistanceFromStart ?? 0.0;

                    // Keep original value - let ETABS handle the sign
                    double loadValue = load.Value;

                    int ret = _sapModel.FrameObj.SetLoadPoint(
                        load.ElementID,          // Name
                        load.LoadPattern,        // LoadPat
                        myType,                  // MyType
                        etabsDir,               // Dir
                        distance,               // Dist
                        loadValue               // Val
                    );

                    if (ret != 0)
                    {
                        throw new Exception($"Failed to apply point load to frame {load.ElementID}. ETABS API returned {ret}");
                    }
                }
                else if (load.LoadType.Contains("Linear"))
                {
                    // Apply Distributed Load to Frame
                    int myType = load.LoadType.Contains("Moment") ? 2 : 1; // 1=Force, 2=Moment
                    double startDist = load.StartDistance ?? 0.0;
                    double endDist = load.EndDistance ?? 1.0;
                    bool relativeDistance = load.RelativeDistance == "Relative";

                    // Keep original value - let ETABS handle the sign
                    double loadValue = load.Value;

                    int ret = _sapModel.FrameObj.SetLoadDistributed(
                        load.ElementID,          // Name
                        load.LoadPattern,        // LoadPat
                        myType,                  // MyType
                        etabsDir,               // Dir
                        startDist,              // Dist1
                        endDist,                // Dist2
                        loadValue,              // Val1
                        loadValue,              // Val2
                        "Global",               // CSys
                        relativeDistance        // RelDist
                    );

                    if (ret != 0)
                    {
                        throw new Exception($"Failed to apply distributed load to frame {load.ElementID}. ETABS API returned {ret}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Unsupported frame load type: {load.LoadType}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error applying frame load to {load.ElementID}: {ex.Message}");
            }
        }

        private void ApplyAreaLoad(LoadAssignment load, int etabsDir)
        {
            try
            {
                if (load.LoadType.Contains("Uniform"))
                {
                    // Keep original value - let ETABS handle the sign
                    double loadValue = load.Value;

                    int ret = _sapModel.AreaObj.SetLoadUniform(
                        load.ElementID,          // Name
                        load.LoadPattern,        // LoadPat
                        loadValue,              // Value
                        etabsDir               // Dir
                    );

                    if (ret != 0)
                    {
                        throw new Exception($"Failed to apply uniform load to area {load.ElementID}. ETABS API returned {ret}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Unsupported area load type: {load.LoadType}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error applying area load to {load.ElementID}: {ex.Message}");
            }
        }
    }
}
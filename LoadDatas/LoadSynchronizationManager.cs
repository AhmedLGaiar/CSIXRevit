using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using ETABSv1;
using loadData; // For SyncReport

namespace CSIXRevit.LoadData
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
                Debug.WriteLine("=== STARTING LOAD IMPORT ===");
                Debug.WriteLine($"Total loads to import: {revitLoads.Count}");

                // STEP 1: Create load patterns first (CRITICAL)
                CreateLoadPatterns(revitLoads);

                // STEP 2: Clear existing loads
                ClearExistingLoads(revitLoads);

                // STEP 3: Apply loads one by one with detailed logging
                foreach (var load in revitLoads)
                {
                    try
                    {
                        Debug.WriteLine($"\n--- Processing Load ---");
                        Debug.WriteLine($"Element: {load.ElementID}");
                        Debug.WriteLine($"Pattern: {load.LoadPattern}");
                        Debug.WriteLine($"Type: {load.LoadType}");
                        Debug.WriteLine($"Value: {load.Value}");

                        ApplyLoadToEtabs(load);
                        report.NewLoads++;

                        Debug.WriteLine($"✓ SUCCESS: Load applied to {load.ElementID}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"✗ ERROR: Failed to apply load to {load.ElementID}: {ex.Message}");
                        // Continue with next load instead of stopping
                    }
                }

                report.TotalLoads = revitLoads.Count;
                _registry.SaveRegistry();

                Debug.WriteLine($"\n=== IMPORT COMPLETED ===");
                Debug.WriteLine($"Total processed: {revitLoads.Count}");
                Debug.WriteLine($"Successfully applied: {report.NewLoads}");
                Debug.WriteLine($"Failed: {revitLoads.Count - report.NewLoads}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CRITICAL ERROR: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }

            return report;
        }

        private void CreateLoadPatterns(List<LoadAssignment> loads)
        {
            try
            {
                Debug.WriteLine("\n=== CREATING LOAD PATTERNS ===");

                HashSet<string> uniquePatterns = new HashSet<string>();
                foreach (var load in loads)
                {
                    if (!string.IsNullOrEmpty(load.LoadPattern))
                    {
                        uniquePatterns.Add(load.LoadPattern);
                    }
                }

                Debug.WriteLine($"Unique patterns to create: {string.Join(", ", uniquePatterns)}");

                // Get existing patterns
                int patternCount = 0;
                string[] existingPatterns = null;
                int ret = _sapModel.LoadPatterns.GetNameList(ref patternCount, ref existingPatterns);

                HashSet<string> existingSet = new HashSet<string>(existingPatterns ?? new string[0]);
                Debug.WriteLine($"Existing patterns: {string.Join(", ", existingSet)}");

                // Create missing patterns
                foreach (string pattern in uniquePatterns)
                {
                    if (!existingSet.Contains(pattern))
                    {
                        eLoadPatternType patternType = eLoadPatternType.Other; // Use simple type
                        ret = _sapModel.LoadPatterns.Add(pattern, patternType);

                        if (ret == 0)
                        {
                            Debug.WriteLine($"✓ Created pattern: {pattern}");
                        }
                        else
                        {
                            Debug.WriteLine($"✗ Failed to create pattern {pattern}, code: {ret}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Pattern {pattern} already exists");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR creating patterns: {ex.Message}");
                throw;
            }
        }

        private void ClearExistingLoads(List<LoadAssignment> loads)
        {
            try
            {
                Debug.WriteLine("\n=== CLEARING EXISTING LOADS ===");

                var elementIds = loads.Select(l => l.ElementID).Distinct().ToList();
                Debug.WriteLine($"Elements to clear: {string.Join(", ", elementIds)}");

                foreach (string elementId in elementIds)
                {
                    try
                    {
                        if (IsFrameElement(elementId))
                        {
                            int ret1 = _sapModel.FrameObj.DeleteLoadDistributed(elementId, "All");
                            int ret2 = _sapModel.FrameObj.DeleteLoadPoint(elementId, "All");
                            Debug.WriteLine($"Cleared frame {elementId}: Dist={ret1}, Point={ret2}");
                        }
                        else if (IsAreaElement(elementId))
                        {
                            int ret = _sapModel.AreaObj.DeleteLoadUniform(elementId, "All");
                            Debug.WriteLine($"Cleared area {elementId}: {ret}");
                        }
                        else
                        {
                            Debug.WriteLine($"WARNING: Element {elementId} not found in model");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error clearing {elementId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in clear: {ex.Message}");
            }
        }

        private void ApplyLoadToEtabs(LoadAssignment load)
        {
            if (_sapModel == null)
            {
                throw new InvalidOperationException("ETABS model not initialized");
            }

            if (IsFrameElement(load.ElementID))
            {
                ApplyFrameLoad(load);
            }
            else if (IsAreaElement(load.ElementID))
            {
                ApplyAreaLoad(load);
            }
            else
            {
                throw new Exception($"Element {load.ElementID} not found in ETABS model");
            }
        }

        private bool IsFrameElement(string elementID)
        {
            try
            {
                int frameCount = 0;
                string[] frameNames = null;
                _sapModel.FrameObj.GetNameList(ref frameCount, ref frameNames);

                bool isFrame = frameNames != null && frameNames.Contains(elementID);
                Debug.WriteLine($"Element {elementID} is frame: {isFrame}");
                return isFrame;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking frame {elementID}: {ex.Message}");
                return false;
            }
        }

        private bool IsAreaElement(string elementID)
        {
            try
            {
                int areaCount = 0;
                string[] areaNames = null;
                _sapModel.AreaObj.GetNameList(ref areaCount, ref areaNames);

                bool isArea = areaNames != null && areaNames.Contains(elementID);
                Debug.WriteLine($"Element {elementID} is area: {isArea}");
                return isArea;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking area {elementID}: {ex.Message}");
                return false;
            }
        }

        private void ApplyFrameLoad(LoadAssignment load)
        {
            try
            {
                if (load.LoadType.Contains("Linear") || load.LoadType.Contains("Distributed"))
                {
                    // Apply distributed load - SIMPLE AND RELIABLE
                    double loadValue = Math.Abs(load.Value); // Force positive value

                    Debug.WriteLine($"Applying distributed load:");
                    Debug.WriteLine($"  Element: {load.ElementID}");
                    Debug.WriteLine($"  Pattern: {load.LoadPattern}");
                    Debug.WriteLine($"  Value: {loadValue} (original: {load.Value})");
                    Debug.WriteLine($"  Direction: Local Z (3)");

                    int ret = _sapModel.FrameObj.SetLoadDistributed(
                        load.ElementID,     // Name
                        load.LoadPattern,   // LoadPat
                        1,                  // MyType (1=Force)
                        3,                  // Dir (3=Local Z)
                        0.0,               // Dist1 (start)
                        1.0,               // Dist2 (end)
                        loadValue,         // Val1 (start value)
                        loadValue          // Val2 (end value)
                                           // Using 8-parameter version (no CSys, no RelDist)
                    );

                    Debug.WriteLine($"SetLoadDistributed result: {ret} (0=success)");

                    if (ret != 0)
                    {
                        throw new Exception($"SetLoadDistributed failed with code {ret}");
                    }
                }
                else if (load.LoadType.Contains("Point"))
                {
                    // Apply point load
                    double loadValue = Math.Abs(load.Value);
                    double distance = load.DistanceFromStart ?? 0.5;

                    Debug.WriteLine($"Applying point load:");
                    Debug.WriteLine($"  Element: {load.ElementID}");
                    Debug.WriteLine($"  Pattern: {load.LoadPattern}");
                    Debug.WriteLine($"  Value: {loadValue}");
                    Debug.WriteLine($"  Distance: {distance}");

                    int ret = _sapModel.FrameObj.SetLoadPoint(
                        load.ElementID,
                        load.LoadPattern,
                        1,                  // MyType (1=Force)
                        3,                  // Dir (3=Local Z)
                        distance,
                        loadValue
                    );

                    Debug.WriteLine($"SetLoadPoint result: {ret} (0=success)");

                    if (ret != 0)
                    {
                        throw new Exception($"SetLoadPoint failed with code {ret}");
                    }
                }
                else
                {
                    throw new Exception($"Unsupported load type: {load.LoadType}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in ApplyFrameLoad: {ex.Message}");
                throw;
            }
        }

        private void ApplyAreaLoad(LoadAssignment load)
        {
            try
            {
                if (load.LoadType.Contains("Uniform"))
                {
                    double loadValue = Math.Abs(load.Value);

                    Debug.WriteLine($"Applying area uniform load:");
                    Debug.WriteLine($"  Element: {load.ElementID}");
                    Debug.WriteLine($"  Pattern: {load.LoadPattern}");
                    Debug.WriteLine($"  Value: {loadValue}");

                    int ret = _sapModel.AreaObj.SetLoadUniform(
                        load.ElementID,
                        load.LoadPattern,
                        loadValue,
                        3                   // Dir (3=Local Z)
                    );

                    Debug.WriteLine($"SetLoadUniform result: {ret} (0=success)");

                    if (ret != 0)
                    {
                        throw new Exception($"SetLoadUniform failed with code {ret}");
                    }
                }
                else
                {
                    throw new Exception($"Unsupported area load type: {load.LoadType}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in ApplyAreaLoad: {ex.Message}");
                throw;
            }
        }

        private List<LoadAssignment> ExtractLoadsFromEtabs()
        {
            return new List<LoadAssignment>();
        }
    }
}


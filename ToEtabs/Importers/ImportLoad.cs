using ETABSv1;
using ToEtabs.JsonHandler;
using ToEtabs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using LoadData;
using ToEtabs.Helpers;
using FromEtabs;
using System.Diagnostics;

namespace ToEtabs.Importers
{
    public static class ImportLoad
    {
        private static cSapModel _etabsModel;
        private static readonly List<string> _processedLoadPatterns = new List<string>();

        /// <summary>
        /// Main entry point for loading all load data into ETABS
        /// </summary>
        public static void ImportAllLoadData(JsonExport loadData, ref cSapModel etabsModel)
        {
            _etabsModel = etabsModel;
            _processedLoadPatterns.Clear();

            try
            {
                // Execution order matters - patterns must be created before combinations/element loads
                ImportLoadPatterns(loadData.LoadDefinitions);
                ImportLoadCombinations(loadData.LoadCombinations);
            }
            catch (Exception ex)
            {
                Logger.Error($"Fatal error during load import: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Applies element-specific loads after elements have been created
        /// </summary>
        public static void ApplyElementLoads(List<StructuralElement> elements)
        {
            if (_etabsModel == null)
                throw new InvalidOperationException("ETABS model not initialized. Call ImportAllLoadData first.");

            int successCount = 0;
            int errorCount = 0;

            foreach (var element in elements.Where(e => e.Loads?.Count > 0))
            {
                foreach (var load in element.Loads)
                {
                    try
                    {
                        if (!_processedLoadPatterns.Contains(load.LoadPattern))
                        {
                            Logger.Warning($"Load pattern {load.LoadPattern} not found - skipping load");
                            continue;
                        }

                        LoadHelper.ConvertLoadToSI(load);
                        ApplySingleLoad(element, load);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Logger.Error($"Error applying load to {element.Id} ({element.Type}): {ex.Message}");
                    }
                }
            }

            Logger.Info($"Applied {successCount} element loads. {errorCount} failed.");
        }

        #region Load Patterns and Combinations
        private static void ImportLoadPatterns(List<LoadDefinition> loadDefinitions)
        {
            var uniqueLoadDefs = loadDefinitions
                .GroupBy(l => l.Name)
                .Select(g => g.First())
                .ToList();

            foreach (var loadDef in uniqueLoadDefs)
            {
                try
                {
                    int n = 0;
                    string[] names = new string[0];

                    if (_etabsModel.LoadPatterns.GetNameList(ref n, ref names) == 0 &&
                        names != null && names.Contains(loadDef.Name))
                    {
                        Logger.Info($"Load pattern {loadDef.Name} already exists - skipping");
                        _processedLoadPatterns.Add(loadDef.Name);
                        continue;
                    }

                    var loadType = ConvertToEtabsLoadType(loadDef.Type);
                    int ret = _etabsModel.LoadPatterns.Add(loadDef.Name, loadType, loadDef.SelfWeightMultiplier);

                    if (ret == 0)
                    {
                        _processedLoadPatterns.Add(loadDef.Name);
                        Logger.Debug($"Added load pattern: {loadDef.Name} ({loadType})");
                    }
                    else
                    {
                        Logger.Warning($"Failed to add load pattern {loadDef.Name}. Error code: {ret}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error importing load pattern {loadDef.Name}: {ex.Message}");
                }
            }
        }

        private static eLoadPatternType ConvertToEtabsLoadType(string type)
        {
            return type?.ToUpper() switch
            {
                "DEAD" => eLoadPatternType.Dead,
                "SUPERDEAD" => eLoadPatternType.SuperDead,
                "LIVE" => eLoadPatternType.Live,
                "REDUCELIVE" => eLoadPatternType.ReduceLive,
                "QUAKE" => eLoadPatternType.Quake,
                "WIND" => eLoadPatternType.Wind,
                "SNOW" => eLoadPatternType.Snow,
                "OTHER" => eLoadPatternType.Other,
                "NOTIONAL" => eLoadPatternType.Notional,
                _ => eLoadPatternType.Other
            };
        }

        private static void ImportLoadCombinations(List<LoadCombination> combinations)
        {
            var uniqueCombos = combinations
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToList();

            foreach (var combo in uniqueCombos)
            {
                try
                {
                    int n = 0;
                    string[] names = new string[0];

                    if (_etabsModel.RespCombo.GetNameList(ref n, ref names) == 0 &&
                        names != null && names.Contains(combo.Name))
                    {
                        Logger.Info($"Load combination {combo.Name} already exists - skipping");
                        continue;
                    }

                    int ret = _etabsModel.RespCombo.Add(combo.Name, 0);
                    if (ret != 0)
                    {
                        Logger.Warning($"Failed to create load combination {combo.Name}. Error code: {ret}");
                        continue;
                    }

                    foreach (var factor in combo.Factors)
                    {
                        if (!_processedLoadPatterns.Contains(factor.LoadPattern))
                        {
                            Logger.Warning($"Load pattern {factor.LoadPattern} not found - skipping combination factor");
                            continue;
                        }

                        var cNameType = eCNameType.LoadCase;
                        ret = _etabsModel.RespCombo.SetCaseList(combo.Name, ref cNameType, factor.LoadPattern, factor.Factor);
                        if (ret != 0)
                        {
                            Logger.Warning($"Failed to add load pattern {factor.LoadPattern} to combination {combo.Name}. Error code: {ret}");
                        }
                    }

                    Logger.Debug($"Added load combination: {combo.Name} with {combo.Factors.Count} factors");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error importing load combination {combo.Name}: {ex.Message}");
                }
            }
        }
        #endregion

        #region Element Load Application
        private static void ApplySingleLoad(StructuralElement element, LoadData.LoadAssignment load)
        {
            switch (element.Type.ToUpper())
            {
                case "BEAM":
                case "BRACE":
                case "FRAME":
                    ApplyFrameLoad(element, load);
                    break;

                case "SLAB":
                case "WALL":
                case "AREA":
                    ApplyAreaLoad(element, load);
                    break;

                case "COLUMN":
                    ApplyColumnLoad(element, load);
                    break;

                case "JOINT":
                case "NODE":
                    ApplyJointLoad(element, load);
                    break;

                default:
                    Logger.Warning($"Unsupported element type for loading: {element.Type}");
                    break;
            }
        }

        private static void ApplyFrameLoad(StructuralElement element, LoadData.LoadAssignment load)
        {
            int dir = LoadHelper.NormalizeDirection(load.Dir);
            string elemName = element.Id;

            switch (load.LoadType.ToUpper())
            {
                case "UNIFORM":
                    SetLoadUniform(_etabsModel.FrameObj,
                        elemName, load.LoadPattern, load.Value, dir,
                        load.StartDistance ?? 0, load.EndDistance ?? 0,
                        load.RelativeDistance.Equals("Relative", StringComparison.OrdinalIgnoreCase) ? "Relative" : "Absolute");
                    break;

                case "POINT":
                    SetLoadPoint(_etabsModel.FrameObj,
                        elemName, load.LoadPattern, load.Value, dir,
                        load.StartDistance ?? 0,
                        load.RelativeDistance.Equals("Relative", StringComparison.OrdinalIgnoreCase) ? "Relative" : "Absolute");
                    break;

                case "TRAPEZOIDAL":
                    SetLoadStrain(_etabsModel.FrameObj,
                        elemName, load.LoadPattern, load.Value, dir,
                        load.StartDistance ?? 0, load.EndDistance ?? 0,
                        load.RelativeDistance.Equals("Relative", StringComparison.OrdinalIgnoreCase) ? "Relative" : "Absolute");
                    break;

                default:
                    Logger.Warning($"Unsupported frame load type: {load.LoadType}");
                    break;
            }
        }

        private static void ApplyAreaLoad(StructuralElement element, LoadData.LoadAssignment load)
        {
            int dir = LoadHelper.NormalizeDirection(load.Dir);
            string elemName = element.Id;

            switch (load.LoadType.ToUpper())
            {
                case "UNIFORM":
                    int ret = _etabsModel.AreaObj.SetLoadUniform(elemName, load.LoadPattern, load.Value, dir);
                    if (ret != 0)
                    {
                        Logger.Error($"Failed to apply uniform load to area {elemName}. Error code: {ret}");
                    }
                    break;

                case "POINT":
                case "HYDROSTATIC":
                    Logger.Warning($"Load type {load.LoadType} is not supported for area elements in this import.");
                    break;

                default:
                    Logger.Warning($"Unsupported area load type: {load.LoadType}");
                    break;
            }
        }

        private static void ApplyColumnLoad(StructuralElement element, LoadData.LoadAssignment load)
        {
            ApplyFrameLoad(element, load);
        }

        private static void ApplyJointLoad(StructuralElement element, LoadData.LoadAssignment load)
        {
            string jointName = element.Id;
            int dir = LoadHelper.NormalizeDirection(load.Dir);

            // Initialize force and moment components
            double[] forceComponents = new double[6]; // Fx, Fy, Fz, Mx, My, Mz

            // Apply load value to the appropriate direction
            switch (dir)
            {
                case 1: // X direction
                    forceComponents[0] = load.Value;
                    break;
                case 2: // Y direction
                    forceComponents[1] = load.Value;
                    break;
                case 3: // Z direction
                    forceComponents[2] = load.Value;
                    break;
                default:
                    Logger.Warning($"Invalid direction {dir} for joint load on {jointName}");
                    return;
            }

            int ret = _etabsModel.PointObj.SetLoadForce(
                jointName, load.LoadPattern, ref forceComponents, true, "Global");
            if (ret != 0)
            {
                Logger.Error($"Failed to apply joint load to {jointName}. Error code: {ret}");
            }
        }
        #endregion

        #region Helper Methods
        public static bool VerifyLoadPatternExists(string patternName)
        {
            if (_processedLoadPatterns.Contains(patternName))
                return true;

            int n = 0;
            string[] names = new string[0];
            int ret = _etabsModel.LoadPatterns.GetNameList(ref n, ref names);
            return ret == 0 && names != null && names.Contains(patternName);
        }

        public static List<string> GetMissingLoadPatterns(List<LoadData.LoadAssignment> loads)
        {
            int n = 0;
            string[] existingPatterns = new string[0];
            _etabsModel.LoadPatterns.GetNameList(ref n, ref existingPatterns);

            if (existingPatterns == null)
                existingPatterns = new string[0];

            return loads
                .Select(l => l.LoadPattern)
                .Distinct()
                .Where(p => !existingPatterns.Contains(p))
                .ToList();
        }
        #endregion

        #region Extension Methods
        // Frame load extension methods
        private static void SetLoadUniform(cFrameObj frameObj, string name, string loadPattern,
            double value, int dir, double startDist, double endDist, string relativeOrAbsolute)
        {
            int ret = frameObj.SetLoadDistributed(name, loadPattern, 1, 2, startDist, endDist, value, value, relativeOrAbsolute);
            if (ret != 0)
            {
                Logger.Error($"Failed to apply uniform load to frame {name}. Error code: {ret}");
            }
        }

        private static void SetLoadPoint(cFrameObj frameObj, string name, string loadPattern,
            double value, int dir, double distance, string relativeOrAbsolute)
        {
            int ret = frameObj.SetLoadPoint(name, loadPattern, 1, dir, distance, value, relativeOrAbsolute);
            if (ret != 0)
            {
                Logger.Error($"Failed to apply point load to frame {name}. Error code: {ret}");
            }
        }

        private static void SetLoadStrain(cFrameObj frameObj, string name, string loadPattern,
            double value, int dir, double startDist, double endDist, string relativeOrAbsolute)
        {
            int ret = frameObj.SetLoadDistributed(name, loadPattern, 1, 10, startDist, endDist, value, value, relativeOrAbsolute);
            if (ret != 0)
            {
                Logger.Error($"Failed to apply strain load to frame {name}. Error code: {ret}");
            }
        }
        #endregion
    }
}
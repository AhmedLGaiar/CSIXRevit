using System;
using System.Collections.Generic;
using System.Linq;
using LoadData;
using ToEtabs.Helpers;
using ToEtabs.JsonHandler;

namespace ToEtabs.Utilities
{
    public static class LoadUtilities
    {
        /// <summary>
        /// Validates load data for consistency before importing to ETABS
        /// </summary>
        public static bool ValidateLoadData(JsonExport loadData, out List<string> errors)
        {
            errors = new List<string>();

            if (loadData == null)
            {
                errors.Add("Load data is null.");
                return false;
            }

            // Validate load definitions
            if (loadData.LoadDefinitions == null || !loadData.LoadDefinitions.Any())
            {
                errors.Add("No load definitions provided.");
            }
            else
            {
                foreach (var loadDef in loadData.LoadDefinitions)
                {
                    if (string.IsNullOrWhiteSpace(loadDef.Name))
                    {
                        errors.Add("Load definition has empty or null name.");
                    }
                    if (!IsValidLoadType(loadDef.Type))
                    {
                        errors.Add($"Invalid load type: {loadDef.Type} for load definition {loadDef.Name}");
                    }
                }
            }

            // Validate load combinations
            if (loadData.LoadCombinations != null)
            {
                foreach (var combo in loadData.LoadCombinations)
                {
                    if (string.IsNullOrWhiteSpace(combo.Name))
                    {
                        errors.Add("Load combination has empty or null name.");
                    }
                    if (combo.Factors == null || !combo.Factors.Any())
                    {
                        errors.Add($"Load combination {combo.Name} has no factors.");
                    }
                    else
                    {
                        foreach (var factor in combo.Factors)
                        {
                            if (string.IsNullOrWhiteSpace(factor.LoadPattern))
                            {
                                errors.Add($"Load combination {combo.Name} has a factor with empty load pattern.");
                            }
                        }
                    }
                }
            }

            // Validate element loads
            if (loadData.Elements != null)
            {
                foreach (var element in loadData.Elements.Where(e => e.Loads?.Count > 0))
                {
                    foreach (var load in element.Loads)
                    {
                        if (string.IsNullOrWhiteSpace(load.LoadPattern))
                        {
                            errors.Add($"Element {element.Id} has a load with empty load pattern.");
                        }
                        if (!IsValidLoadType(load.LoadType))
                        {
                            errors.Add($"Invalid load type: {load.LoadType} for element {element.Id}");
                        }
                        if (load.Unit == null)
                        {
                            errors.Add($"Load on element {element.Id} has null unit.");
                        }
                    }
                }
            }

            Logger.Info($"Load data validation completed with {errors.Count} errors.");
            return !errors.Any();
        }

        /// <summary>
        /// Checks if the load type is valid
        /// </summary>
        private static bool IsValidLoadType(string loadType)
        {
            var validTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DEAD", "SUPERDEAD", "LIVE", "REDUCELIVE", "QUAKE", "WIND", "SNOW", "OTHER", "NOTIONAL",
                "UNIFORM", "POINT", "TRAPEZOIDAL", "MOMENT"
            };
            return validTypes.Contains(loadType?.ToUpper() ?? "");
        }

        /// <summary>
        /// Prepares load data by ensuring all units are converted to SI
        /// </summary>
        public static void PrepareLoadData(JsonExport loadData)
        {
            if (loadData?.Elements == null) return;

            foreach (var element in loadData.Elements.Where(e => e.Loads?.Count > 0))
            {
                foreach (var load in element.Loads)
                {
                    try
                    {
                        LoadHelper.ConvertLoadToSI(load);
                        Logger.Debug($"Converted load for element {element.Id} to SI units: {load.Value} {load.Unit}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to convert load for element {element.Id}: {ex.Message}");
                    }
                }
            }
        }
    }
}
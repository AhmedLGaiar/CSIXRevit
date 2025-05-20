using System;
using System.Collections.Generic;
using LoadData;

namespace ToEtabs.Helpers
{
    public static class LoadHelper
    {
        // Conversion factors to SI metric units (kN, m, kN/m², etc.)
        private static readonly Dictionary<string, double> _unitConversion = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            // Area Loads (pressure)
            {"ksf", 47.88026},    // kip/ft² → N/m² (1 ksf = 47.88 kN/m²)
            {"kPa", 1.0},         // kN/m² (SI)
            {"kN/m²", 1.0},       // same as kPa
            {"psi", 6.89476},     // psi → kN/m²
            {"kgf/m²", 0.00980665}, // kgf/m² → kN/m²

            // Line Loads (force per length)
            {"kip/ft", 14.5939},   // kip/ft → kN/m
            {"kN/m", 1.0},         // (SI)
            {"plf", 0.0145939},   // lb/ft → kN/m
            {"N/m", 0.001},       // N/m → kN/m

            // Point Loads (force)
            {"kip", 4.44822},     // kip → kN
            {"kN", 1.0},           // (SI)
            {"lb", 0.00444822},    // lb → kN
            {"kgf", 0.00980665},   // kgf → kN

            // Moment Loads
            {"kip-ft", 1.35582},   // kip-ft → kN·m
            {"kN-m", 1.0}          // (SI)
        };

        /// <summary>
        /// Converts load value to SI metric units (kN, kN/m, kN/m²)
        /// </summary>
        public static double ConvertToSI(double value, string unit)
        {
            if (_unitConversion.TryGetValue(unit?.Trim() ?? "", out double factor))
            {
                return value * factor;
            }
            throw new ArgumentException($"Unsupported unit: {unit}");
        }

        /// <summary>
        /// Converts a complete load object to SI units
        /// </summary>
        public static void ConvertLoadToSI(LoadAssignment load)
        {
            if (load.Unit != null && !load.Unit.Equals("SI", StringComparison.OrdinalIgnoreCase))
            {
                load.Value = ConvertToSI(load.Value, load.Unit);
                load.Unit = GetStandardSIUnit(load.LoadType);
            }
        }

        /// <summary>
        /// Gets the standard SI unit for each load type
        /// </summary>
        public static string GetStandardSIUnit(string loadType)
        {
            return loadType?.ToUpper() switch
            {
                "UNIFORM" => "kN/m²",     // Area load
                "LINE" or "TRAPEZOIDAL" => "kN/m",  // Line load
                "POINT" => "kN",          // Concentrated load
                "MOMENT" => "kN·m",      // Moment load
                _ => "kN"                 // Default
            };
        }

        /// <summary>
        /// Converts ETABS direction codes (10 = Gravity, etc.)
        /// </summary>
        public static int NormalizeDirection(int etabsDirectionCode)
        {
            return etabsDirectionCode switch
            {
                10 => 3,  // Gravity → Z-axis
                _ => etabsDirectionCode > 0 && etabsDirectionCode < 4 ? etabsDirectionCode : 3
            };
        }
    }
}
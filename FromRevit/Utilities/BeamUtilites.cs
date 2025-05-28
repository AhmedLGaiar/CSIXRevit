using Autodesk.Revit.DB;

namespace FromRevit.Utilities
{
    internal class BeamUtilities
    {
        public static double GetParameter(FamilyInstance beam, string paramName, double defaultValue)
        {
            Parameter param = beam.Symbol.LookupParameter(paramName);
            return param != null ? param.AsDouble() : defaultValue;
        }
    }
}

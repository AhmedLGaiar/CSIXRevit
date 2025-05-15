namespace ToEtabs.Helpers
{
    internal class ShearWallHelpers
    {
        internal static bool IsDimensionValid(double value, double minValue = 0.0, double tolerance = 0.001)
        {
            return value > minValue - tolerance;
        }
    }
}
namespace ToEtabs.Helpers
{
    internal class ShearWallHelpers
    {
        internal static bool IsDimensionValid(double value, double minValue = 0.0, double tolerance = 0.001)
        {
            return value > minValue - tolerance;
        }

        internal static double NormalizeAngle(double angle)
        {
            return ((angle % 360) + 360) % 360;
        }

        internal static bool IsApproximately(double value, double target, double tolerance = 1.0)
        {
            return Math.Abs(value - target) < tolerance;
        }
    }
}
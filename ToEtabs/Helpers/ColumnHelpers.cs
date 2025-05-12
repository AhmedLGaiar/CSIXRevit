using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToEtabs.Helpers
{
    internal class ColumnHelpers
    {
        internal static bool IsApproximately(double value, double target, double tolerance = 5.0)
        {
            return Math.Abs(value - target) < tolerance;
        }

        internal static double NormalizeAngle(double angle)
        {
            return ((angle % 360) + 360) % 360;
        }
    }
}

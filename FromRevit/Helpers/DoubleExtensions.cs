using System;

namespace FromRevit.Helpers
{
    public static class DoubleExtensions
    {
        private const double Epsilon = 1.0e-9; // Adjust this value as needed

        public static bool IsAlmostZero(this double value)
        {
            return Math.Abs(value) < Epsilon;
        }
    }
}

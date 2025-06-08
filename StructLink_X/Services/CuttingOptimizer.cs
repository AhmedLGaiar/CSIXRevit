using System;
using System.Collections.Generic;
using System.Linq;

namespace StructLink_X.Services
{
    public class CuttingOptimizer
    {
        public static string CalculateCuttingPlan(double totalLength)
        {
            double standardLength = 12.0; // طول القضيب القياسي
            List<double> lengths = new List<double> { totalLength }; // افتراضي: طول واحد للتبسيط
            lengths.Sort((a, b) => b.CompareTo(a)); // ترتيب تنازلي (First-Fit Decreasing)
            List<double> bars = new List<double>();
            double remainingLength = totalLength;

            while (remainingLength > 0)
            {
                if (remainingLength <= standardLength)
                {
                    bars.Add(remainingLength);
                    remainingLength = 0;
                }
                else
                {
                    bars.Add(standardLength);
                    remainingLength -= standardLength;
                }
            }

            double waste = bars.Sum() - totalLength;
            return $"Use {bars.Count} bars: {string.Join(", ", bars.Select(b => $"{b:F2}m"))}, Waste: {waste:F2}m";
        }
    }
}
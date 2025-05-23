using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReinforcementFromEtab
{

    public static class ACIBarDistributor
    {
        public static ACIBarLayout DistributeBars(
            int totalBars,
            double width,
            double height,
            int barDia=16,
            double maxSpacing = 250)
        {
            int minSpacing = 25;

            // Assume 25 mm cover + bar radius on both sides
            double usableShort = width - 2 * (25 + barDia / 2);
            double usableLong = height - 2 * (25 + barDia / 2);

            int maxR2 = Math.Max(2, (int)(usableShort / (barDia + minSpacing)));
            int maxR3 = Math.Max(2, (int)(usableLong / (barDia + minSpacing)));

            ACIBarLayout bestLayout = null;
            int bestActual = int.MaxValue;

            for (int r2 = 2; r2 <= maxR2; r2++)
            {
                for (int r3 = 2; r3 <= maxR3; r3++)
                {
                    int actualBars = 2 * r2 + 2 * r3 - 4;
                    if (actualBars >= totalBars && actualBars < bestActual)
                    {
                        bestActual = actualBars;
                        bestLayout = new ACIBarLayout
                        {
                            BarsPerR2Face = r2,
                            BarsPerR3Face = r3
                        };
                    }
                }
            }

            return bestLayout ?? throw new Exception("Could not find valid layout");
        }
    }
}

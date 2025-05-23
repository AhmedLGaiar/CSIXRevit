using ETABSv1;
using ElementsData;
using ToEtabs.Helpers;
using ToEtabs.Utilities;

namespace ToEtabs.Importers
{
    public class ImportColumn
    {
        public static void ImportColumns(List<ColumnGeometryData> columns, cSapModel _sapModel,string SelectedConcreteMaterial)
        {
            int done;
            int ColNum = 1;

            foreach (var column in columns)
            {
                double widthMeters = column.Width;
                double depthMeters = column.depth;
                done = ColumnUtilities.DefineColumnSection(_sapModel, $"C {widthMeters}*{depthMeters} H",
                    SelectedConcreteMaterial, depthMeters * 1000, widthMeters * 1000);
                if (done == 0)
                {
                    done = ColumnUtilities.DefineColumnSection(_sapModel, $"C {widthMeters}*{depthMeters} V",
                        SelectedConcreteMaterial, widthMeters * 1000, depthMeters * 1000);
                }
            }

            foreach (var column in columns)
            {
                double widthMeters = column.Width;
                double depthMeters = column.depth;

                string orientation;
                double rotation = ColumnHelpers.NormalizeAngle(column.Rotation);

                if (ColumnHelpers.IsApproximately(rotation, 0) || ColumnHelpers.IsApproximately(rotation, 180))
                {
                    orientation = "V";
                }
                else if (ColumnHelpers.IsApproximately(rotation, 90) ||
                         ColumnHelpers.IsApproximately(rotation, 270))
                {
                    orientation = "H";
                }
                else
                {
                    orientation = "H"; // Default/fallback
                }

                done = ColumnUtilities.DrawColumnByCoordinates(_sapModel,
                    column.BasePoint.X, column.BasePoint.Y, column.BasePoint.Z,
                    column.TopPoint.X, column.TopPoint.Y, column.TopPoint.Z,
                    $"C{ColNum}", $"C {widthMeters}*{depthMeters} {orientation}");

                ColNum++;
            }
        }
    }
}

using Autodesk.Revit.DB;
using ElementsData;
using FromRevit.Utilites;

namespace FromRevit.ElementsCommand
{
    public class Beams
    {
        public static List<BeamGeometryData> GetBeamGeometryData(Document doc)
        {
            IEnumerable<FamilyInstance> beamsElementCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            List<BeamGeometryData> BeamList = new List<BeamGeometryData>();
            foreach (FamilyInstance beam in beamsElementCollector)
            {
                ElementId materialId = beam.StructuralMaterialId;
                Material material = doc.GetElement(materialId) as Material;
                if (material == null) continue;

                if (!material.Name.ToLower().Contains("concrete"))
                    continue;

                LocationCurve location = beam.Location as LocationCurve;
                if (location == null) continue;

                Curve curve = location.Curve;
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                double width = BeamUtilities.GetParameter(beam, "b", 0.3);
                double depth = BeamUtilities.GetParameter(beam, "h", 0.5);

                string BeamName = beam.Name;
                string BeamId = beam.UniqueId;

                BeamList.Add(new BeamGeometryData
                {
                    ApplicationId = BeamId,
                    Name = BeamName,
                    StartPoint = PointUtilites.FromXYZInMilli(startPoint),
                    EndPoint = PointUtilites.FromXYZInMilli(endPoint),
                    Depth = UnitUtils.ConvertFromInternalUnits(depth, UnitTypeId.Meters),
                    Width = UnitUtils.ConvertFromInternalUnits(width, UnitTypeId.Meters),
                });
            }
            return BeamList;
        }
    }
}
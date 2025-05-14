using System.Collections.Generic;
using Autodesk.Revit.DB;
using ExportJsonFileFromRevit;
using FromRevit.Data;

namespace FromRevit
{
    public class BeamExtractor
    {
        private readonly Document _doc;

        public BeamExtractor(Document doc)
        {
            _doc = doc;
        }

        public IEnumerable<BeamData> ExtractConcreteBeams()
        {
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType();

            foreach (FamilyInstance beam in collector)
            {
                ElementId materialId = beam.StructuralMaterialId;
                Material material = _doc.GetElement(materialId) as Material;
                if (material == null) continue;

                // شرط: لازم تكون خرسانة
                if (!material.Name.ToLower().Contains("concrete"))
                    continue;

                LocationCurve location = beam.Location as LocationCurve;
                if (location == null) continue;

                Curve curve = location.Curve;
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                double width = GetParameter(beam, "b", 0.3);
                double depth = GetParameter(beam, "h", 0.5);

                yield return new BeamData
                {
                    ApplicationId = beam.UniqueId,
                    Name = beam.Name,
                    StartPoint = new { x = startPoint.X, y = startPoint.Y, z = startPoint.Z },
                    EndPoint = new { x = endPoint.X, y = endPoint.Y, z = endPoint.Z },
                    Material = new { name = material.Name },
                    Section = new
                    {
                        name = beam.Symbol.Name,
                        depth,
                        width
                    }
                };
            }
        }

        private double GetParameter(FamilyInstance beam, string paramName, double defaultValue)
        {
            Parameter param = beam.Symbol.LookupParameter(paramName);
            return param != null ? param.AsDouble() * 0.3048 : defaultValue;
        }
    }
}

using ElementsData;
using Autodesk.Revit.DB;
using FromRevit.Utilities;

namespace FromRevit.ElementsCommand
{
    public class Slabs
    {
        public static List<SlabData> GetSlabData(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .OfCategory(BuiltInCategory.OST_Floors);

            List<SlabData> slabList = new List<SlabData>();

            foreach (Floor floor in collector)
            {
                string typeName = doc.GetElement(floor.GetTypeId()).Name;
                Level level = doc.GetElement(floor.LevelId) as Level;
                double thickness = floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();

                Options options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
                GeometryElement geomElement = floor.get_Geometry(options);

                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is Solid solid && solid.Faces.Size > 0)
                    {
                        Face bottomFace = null;
                        double minZ = double.MaxValue;

                        foreach (Face face in solid.Faces)
                        {
                            Mesh mesh = face.Triangulate();
                            double avgZ = mesh.Vertices.Cast<XYZ>().Average(v => v.Z);

                            if (avgZ < minZ)
                            {
                                minZ = avgZ;
                                bottomFace = face;
                            }
                        }

                        EdgeArrayArray edgeLoops = bottomFace.EdgeLoops;

                        SlabData slab = new SlabData
                        {
                            Thickness = UnitUtils.ConvertFromInternalUnits(thickness, UnitTypeId.Millimeters),
                            Level = level?.Name,
                            SectionName = typeName
                        };

                        for (int i = 0; i < edgeLoops.Size; i++)
                        {
                            EdgeArray loop = edgeLoops.get_Item(i);
                            List<PointData> loopPoints = loop.Cast<Edge>()
                                .Select(e => PointUtilities.FromXYZInMilli(e.AsCurve().GetEndPoint(0),thickness))
                                .ToList();

                            if (i == 0)
                                slab.OuterBoundary = loopPoints;
                            else
                                slab.Openings.Add(loopPoints);
                        }

                        slabList.Add(slab);
                    }
                }
            }

            return slabList;
        }
    }
}
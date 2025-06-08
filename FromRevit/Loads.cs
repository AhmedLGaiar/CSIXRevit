using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using CSIXRevit.LoadData;

namespace CSIXRevit.FromRevit
{
    public class Loads
    {
        public static List<LoadAssignment> GetLoadData(Document doc)
        {
            List<LoadAssignment> loads = new List<LoadAssignment>();

            ExtractPointLoads(doc, loads);
            ExtractLineLoads(doc, loads);
            ExtractAreaLoads(doc, loads);

            return loads;
        }

        private static void ExtractPointLoads(Document doc, List<LoadAssignment> loads)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PointLoads)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                ElementId hostId = null;

                Parameter hostParam = elem.LookupParameter("Host");
                if (hostParam != null && hostParam.HasValue && hostParam.StorageType == StorageType.ElementId)
                {
                    hostId = hostParam.AsElementId();
                }

                if (hostId == null || hostId == ElementId.InvalidElementId)
                {
                    foreach (ElementId id in elem.GetDependentElements(null))
                    {
                        Element possibleHost = doc.GetElement(id);
                        if (possibleHost != null &&
                            (possibleHost.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming ||
                             possibleHost.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns))
                        {
                            hostId = id;
                            break;
                        }
                    }
                }

                if (hostId == null || hostId == ElementId.InvalidElementId) continue;

                Element host = doc.GetElement(hostId);
                if (host == null) continue;

                Parameter forceX = elem.LookupParameter("Force X");
                Parameter forceY = elem.LookupParameter("Force Y");
                Parameter forceZ = elem.LookupParameter("Force Z");

                Parameter loadCase = elem.LookupParameter("Load Case");
                string loadCaseName = loadCase?.AsValueString() ?? "DEAD";

                if (forceX != null && Math.Abs(forceX.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "PointLoad",
                        Value = forceX.AsDouble(),
                        Unit = "N",
                        Dir = 0,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (forceY != null && Math.Abs(forceY.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "PointLoad",
                        Value = forceY.AsDouble(),
                        Unit = "N",
                        Dir = 1,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (forceZ != null && Math.Abs(forceZ.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "PointLoad",
                        Value = forceZ.AsDouble(),
                        Unit = "N",
                        Dir = 2,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                Parameter momentX = elem.LookupParameter("Moment X");
                Parameter momentY = elem.LookupParameter("Moment Y");
                Parameter momentZ = elem.LookupParameter("Moment Z");

                if (momentX != null && Math.Abs(momentX.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "MomentLoad",
                        Value = momentX.AsDouble(),
                        Unit = "N-m",
                        Dir = 0,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (momentY != null && Math.Abs(momentY.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "MomentLoad",
                        Value = momentY.AsDouble(),
                        Unit = "N-m",
                        Dir = 1,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (momentZ != null && Math.Abs(momentZ.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "MomentLoad",
                        Value = momentZ.AsDouble(),
                        Unit = "N-m",
                        Dir = 2,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }
            }
        }

        private static void ExtractLineLoads(Document doc, List<LoadAssignment> loads)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_LineLoads)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                ElementId hostId = null;

                Parameter hostParam = elem.LookupParameter("Host");
                if (hostParam != null && hostParam.HasValue && hostParam.StorageType == StorageType.ElementId)
                {
                    hostId = hostParam.AsElementId();
                }

                if (hostId == null || hostId == ElementId.InvalidElementId)
                {
                    foreach (ElementId id in elem.GetDependentElements(null))
                    {
                        Element possibleHost = doc.GetElement(id);
                        if (possibleHost != null &&
                            possibleHost.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
                        {
                            hostId = id;
                            break;
                        }
                    }
                }

                if (hostId == null || hostId == ElementId.InvalidElementId) continue;

                Element host = doc.GetElement(hostId);
                if (host == null) continue;

                Parameter forceX = elem.LookupParameter("Force X");
                Parameter forceY = elem.LookupParameter("Force Y");
                Parameter forceZ = elem.LookupParameter("Force Z");

                Parameter loadCase = elem.LookupParameter("Load Case");
                string loadCaseName = loadCase?.AsValueString() ?? "DEAD";

                double startDistance = 0.0;
                double endDistance = 1.0;

                Parameter startParam = elem.LookupParameter("Start Parameter");
                Parameter endParam = elem.LookupParameter("End Parameter");

                if (startParam != null && endParam != null)
                {
                    startDistance = startParam.AsDouble();
                    endDistance = endParam.AsDouble();
                }

                if (forceX != null && Math.Abs(forceX.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "LinearLoad",
                        Value = forceX.AsDouble(),
                        Unit = "N/m",
                        Dir = 0,
                        StartDistance = startDistance,
                        EndDistance = endDistance,
                        RelativeDistance = "Relative",
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (forceY != null && Math.Abs(forceY.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "LinearLoad",
                        Value = forceY.AsDouble(),
                        Unit = "N/m",
                        Dir = 1,
                        StartDistance = startDistance,
                        EndDistance = endDistance,
                        RelativeDistance = "Relative",
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (forceZ != null && Math.Abs(forceZ.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "LinearLoad",
                        Value = forceZ.AsDouble(),
                        Unit = "N/m",
                        Dir = 2,
                        StartDistance = startDistance,
                        EndDistance = endDistance,
                        RelativeDistance = "Relative",
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                Parameter momentX = elem.LookupParameter("Moment X");
                Parameter momentY = elem.LookupParameter("Moment Y");
                Parameter momentZ = elem.LookupParameter("Moment Z");

                if (momentX != null && Math.Abs(momentX.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "LinearMomentLoad",
                        Value = momentX.AsDouble(),
                        Unit = "N-m/m",
                        Dir = 0,
                        StartDistance = startDistance,
                        EndDistance = endDistance,
                        RelativeDistance = "Relative",
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (momentY != null && Math.Abs(momentY.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "LinearMomentLoad",
                        Value = momentY.AsDouble(),
                        Unit = "N-m/m",
                        Dir = 1,
                        StartDistance = startDistance,
                        EndDistance = endDistance,
                        RelativeDistance = "Relative",
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (momentZ != null && Math.Abs(momentZ.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "LinearMomentLoad",
                        Value = momentZ.AsDouble(),
                        Unit = "N-m/m",
                        Dir = 2,
                        StartDistance = startDistance,
                        EndDistance = endDistance,
                        RelativeDistance = "Relative",
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }
            }
        }

        private static void ExtractAreaLoads(Document doc, List<LoadAssignment> loads)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_AreaLoads)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                ElementId hostId = null;

                Parameter hostParam = elem.LookupParameter("Host");
                if (hostParam != null && hostParam.HasValue && hostParam.StorageType == StorageType.ElementId)
                {
                    hostId = hostParam.AsElementId();
                }

                if (hostId == null || hostId == ElementId.InvalidElementId)
                {
                    foreach (ElementId id in elem.GetDependentElements(null))
                    {
                        Element possibleHost = doc.GetElement(id);
                        if (possibleHost != null &&
                            (possibleHost.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors ||
                             possibleHost.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming))
                        {
                            hostId = id;
                            break;
                        }
                    }
                }

                if (hostId == null || hostId == ElementId.InvalidElementId) continue;

                Element host = doc.GetElement(hostId);
                if (host == null) continue;

                Parameter forceX = elem.LookupParameter("Force X");
                Parameter forceY = elem.LookupParameter("Force Y");
                Parameter forceZ = elem.LookupParameter("Force Z");

                Parameter loadCase = elem.LookupParameter("Load Case");
                string loadCaseName = loadCase?.AsValueString() ?? "DEAD";

                if (forceX != null && Math.Abs(forceX.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "UniformLoad",
                        Value = forceX.AsDouble(),
                        Unit = "N/m²",
                        Dir = 0,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (forceY != null && Math.Abs(forceY.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "UniformLoad",
                        Value = forceY.AsDouble(),
                        Unit = "N/m²",
                        Dir = 1,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (forceZ != null && Math.Abs(forceZ.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "UniformLoad",
                        Value = forceZ.AsDouble(),
                        Unit = "N/m²",
                        Dir = 2,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                Parameter momentX = elem.LookupParameter("Moment X");
                Parameter momentY = elem.LookupParameter("Moment Y");
                Parameter momentZ = elem.LookupParameter("Moment Z");

                if (momentX != null && Math.Abs(momentX.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "UniformMomentLoad",
                        Value = momentX.AsDouble(),
                        Unit = "N-m/m²",
                        Dir = 0,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (momentY != null && Math.Abs(momentY.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "UniformMomentLoad",
                        Value = momentY.AsDouble(),
                        Unit = "N-m/m²",
                        Dir = 1,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }

                if (momentZ != null && Math.Abs(momentZ.AsDouble()) > 0.001)
                {
                    loads.Add(new LoadAssignment
                    {
                        ElementID = host.UniqueId,
                        LoadPattern = loadCaseName,
                        LoadType = "UniformMomentLoad",
                        Value = momentZ.AsDouble(),
                        Unit = "N-m/m²",
                        Dir = 2,
                        SourcePlatform = "Revit",
                        LastModified = DateTime.Now,
                        SyncState = LoadSyncState.New
                    });
                }
            }
        }
    }
}

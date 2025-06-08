using System;
using System.Collections.Generic;
using System.Linq;
using ETABSv1;
using CSIXRevit.LoadData;
using loadData;

namespace CSIXRevit.LoadData
{
    public class LoadSynchronizationManager
    {
        private LoadSyncRegistry _registry;

        public LoadSynchronizationManager(string syncFilePath)
        {
            _registry = new LoadSyncRegistry(syncFilePath);
        }

        public SyncReport SyncFromRevitToEtabs(List<LoadAssignment> revitLoads, cSapModel etabsModel)
        {
            foreach (var load in _registry.GetAllLoads().Where(l => l.SourcePlatform == "Etabs"))
            {
                load.SyncState = LoadSyncState.Deleted;
                _registry.RegisterLoad(load);
            }

            int newCount = 0;
            int modifiedCount = 0;
            int unchangedCount = 0;
            int deletedCount = 0;

            foreach (var revitLoad in revitLoads)
            {
                revitLoad.SourcePlatform = "Revit";
                revitLoad.LastModified = DateTime.Now;

                var existingLoads = _registry.GetAllLoads()
                    .Where(l => l.ElementID == revitLoad.ElementID &&
                               l.LoadType == revitLoad.LoadType &&
                               l.LoadPattern == revitLoad.LoadPattern &&
                               l.Dir == revitLoad.Dir)
                    .ToList();

                if (existingLoads.Any())
                {
                    var existingLoad = existingLoads.First();

                    if (!existingLoad.HasSameValues(revitLoad))
                    {
                        existingLoad.Value = revitLoad.Value;
                        existingLoad.StartDistance = revitLoad.StartDistance;
                        existingLoad.EndDistance = revitLoad.EndDistance;
                        existingLoad.LastModified = DateTime.Now;
                        existingLoad.SyncState = LoadSyncState.Modified;

                        UpdateEtabsLoad(etabsModel, existingLoad);
                        modifiedCount++;
                    }
                    else
                    {
                        existingLoad.SyncState = LoadSyncState.Unchanged;
                        unchangedCount++;
                    }

                    _registry.RegisterLoad(existingLoad);
                }
                else
                {
                    revitLoad.SyncState = LoadSyncState.New;
                    _registry.RegisterLoad(revitLoad);

                    CreateEtabsLoad(etabsModel, revitLoad);
                    newCount++;
                }
            }

            foreach (var loadToDelete in _registry.GetAllLoads().Where(l => l.SyncState == LoadSyncState.Deleted).ToList())
            {
                DeleteEtabsLoad(etabsModel, loadToDelete);
                _registry.RemoveLoad(loadToDelete.UniqueIdentifier);
                deletedCount++;
            }

            _registry.SaveRegistry();

            return new SyncReport
            {
                NewLoads = newCount,
                ModifiedLoads = modifiedCount,
                DeletedLoads = deletedCount,
                UnchangedLoads = unchangedCount,
                TotalLoads = _registry.GetAllLoads().Count
            };
        }

        private void UpdateEtabsLoad(cSapModel model, LoadAssignment load)
        {
            switch (load.LoadType)
            {
                case "PointLoad":
                    UpdateEtabsPointLoad(model, load);
                    break;
                case "UniformLoad":
                    UpdateEtabsUniformLoad(model, load);
                    break;
                case "LinearLoad":
                    UpdateEtabsLinearLoad(model, load);
                    break;
            }
        }

        private void CreateEtabsLoad(cSapModel model, LoadAssignment load)
        {
            switch (load.LoadType)
            {
                case "PointLoad":
                    CreateEtabsPointLoad(model, load);
                    break;
                case "UniformLoad":
                    CreateEtabsUniformLoad(model, load);
                    break;
                case "LinearLoad":
                    CreateEtabsLinearLoad(model, load);
                    break;
            }
        }

        private void DeleteEtabsLoad(cSapModel model, LoadAssignment load)
        {
            switch (load.LoadType)
            {
                case "PointLoad":
                    DeleteEtabsPointLoad(model, load);
                    break;
                case "UniformLoad":
                    DeleteEtabsUniformLoad(model, load);
                    break;
                case "LinearLoad":
                    DeleteEtabsLinearLoad(model, load);
                    break;
            }
        }

        private void UpdateEtabsPointLoad(cSapModel model, LoadAssignment load)
        {
            DeleteEtabsPointLoad(model, load);
            CreateEtabsPointLoad(model, load);
        }

        private void CreateEtabsPointLoad(cSapModel model, LoadAssignment load)
        {
            // Create a Value array for the point load as per ETABS API documentation
            double[] value = new double[6] { 0, 0, 0, 0, 0, 0 };

            // Set the appropriate force component based on direction
            value[load.Dir] = load.Value;

            // Use the correct signature from ETABS API documentation
            model.PointObj.SetLoadForce(load.ElementID, load.LoadPattern, ref value, false, "Global", 0);
        }

        private void DeleteEtabsPointLoad(cSapModel model, LoadAssignment load)
        {
            model.PointObj.DeleteLoadForce(load.ElementID, load.LoadPattern);
        }

        private void UpdateEtabsUniformLoad(cSapModel model, LoadAssignment load)
        {
            DeleteEtabsUniformLoad(model, load);
            CreateEtabsUniformLoad(model, load);
        }

        private void CreateEtabsUniformLoad(cSapModel model, LoadAssignment load)
        {
            // Create scalar values for the uniform load as per ETABS API documentation
            double value = load.Value;
            int dir = load.Dir + 1; // Convert 0-based index to 1-based direction

            // Use the correct signature from ETABS API documentation
            model.AreaObj.SetLoadUniform(load.ElementID, load.LoadPattern, value, dir, true, "Global", 0);
        }

        private void DeleteEtabsUniformLoad(cSapModel model, LoadAssignment load)
        {
            model.AreaObj.DeleteLoadUniform(load.ElementID, load.LoadPattern);
        }

        private void UpdateEtabsLinearLoad(cSapModel model, LoadAssignment load)
        {
            DeleteEtabsLinearLoad(model, load);
            CreateEtabsLinearLoad(model, load);
        }

        private void CreateEtabsLinearLoad(cSapModel model, LoadAssignment load)
        {
            // Get start and end distances
            double startDist = load.StartDistance ?? 0;
            double endDist = load.EndDistance ?? 1;

            // Create scalar values for the linear load as per ETABS API documentation
            int myType = 1; // Force per unit length
            int dir = load.Dir + 1; // Convert 0-based index to 1-based direction
            double val1 = load.Value; // Start value
            double val2 = load.Value; // End value (same for constant load)
            bool relDist = load.RelativeDistance == "Relative";

            // Use the correct signature from ETABS API documentation
            model.FrameObj.SetLoadDistributed(load.ElementID, load.LoadPattern, myType, dir,
                                             startDist, endDist, val1, val2,
                                             "Global", relDist, true, 0);
        }

        private void DeleteEtabsLinearLoad(cSapModel model, LoadAssignment load)
        {
            model.FrameObj.DeleteLoadDistributed(load.ElementID, load.LoadPattern);
        }
    }
}

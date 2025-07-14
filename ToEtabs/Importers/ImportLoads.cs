using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ETABSv1;
using Newtonsoft.Json;
using LoadData;

namespace CSIXRevit.ToEtabs
{
    public class ImportLoads
    {
        public static SyncReport ImportLoadData(List<LoadAssignment> loads, cSapModel _sapModel)
        {
            CreateLoadPatterns(_sapModel, loads);

            var syncManager = new LoadSynchronizationManager(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitEtabsExchange", "LoadSyncRegistry.json"));

            return syncManager.SyncFromRevitToEtabs(loads, _sapModel);
        }

        private static void CreateLoadPatterns(cSapModel model, List<LoadAssignment> loads)
        {
            HashSet<string> loadPatterns = new HashSet<string>();
            foreach (var load in loads)
            {
                loadPatterns.Add(load.LoadPattern);
            }

            foreach (string pattern in loadPatterns)
            {
                int patternCount = 0;
                string[] patternNames = null;
                int ret = model.LoadPatterns.GetNameList(ref patternCount, ref patternNames);

                bool patternExists = false;
                if (patternNames != null)
                {
                    foreach (string existingPattern in patternNames)
                    {
                        if (existingPattern == pattern)
                        {
                            patternExists = true;
                            break;
                        }
                    }
                }

                if (!patternExists)
                {
                    // Cast the integer to the eLoadPatternType enum
                    model.LoadPatterns.Add(pattern, (ETABSv1.eLoadPatternType)1);
                }
            }
        }
    }
}

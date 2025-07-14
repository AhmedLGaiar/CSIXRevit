using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace LoadData
{
    public class LoadSyncRegistry
    {
        private Dictionary<string, LoadAssignment> _loadRegistry;
        private string _syncFilePath;

        public LoadSyncRegistry(string syncFilePath)
        {
            _syncFilePath = syncFilePath;
            LoadRegistry();
        }

        private void LoadRegistry()
        {
            if (File.Exists(_syncFilePath))
            {
                string json = File.ReadAllText(_syncFilePath);
                _loadRegistry = JsonConvert.DeserializeObject<Dictionary<string, LoadAssignment>>(json)
                    ?? new Dictionary<string, LoadAssignment>();
            }
            else
            {
                _loadRegistry = new Dictionary<string, LoadAssignment>();
            }
        }

        public void SaveRegistry()
        {
            string json = JsonConvert.SerializeObject(_loadRegistry, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_syncFilePath));
            File.WriteAllText(_syncFilePath, json);
        }

        public void RegisterLoad(LoadAssignment load)
        {
            if (string.IsNullOrEmpty(load.UniqueIdentifier))
            {
                load.GenerateUniqueIdentifier();
            }

            _loadRegistry[load.UniqueIdentifier] = load;
        }

        public LoadAssignment GetLoad(string uniqueIdentifier)
        {
            return _loadRegistry.TryGetValue(uniqueIdentifier, out LoadAssignment load) ? load : null;
        }

        public List<LoadAssignment> GetAllLoads()
        {
            return new List<LoadAssignment>(_loadRegistry.Values);
        }

        public void RemoveLoad(string uniqueIdentifier)
        {
            _loadRegistry.Remove(uniqueIdentifier);
        }

        public bool ContainsLoad(string uniqueIdentifier)
        {
            return _loadRegistry.ContainsKey(uniqueIdentifier);
        }
    }
}

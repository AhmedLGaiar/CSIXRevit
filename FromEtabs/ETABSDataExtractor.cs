using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using LoadData;
using ETABSv1;

namespace ETABSDataExtraction
{
    public class ETABSDataExtractor
    {
        private readonly cSapModel _etabsModel;
        private readonly string _jsonOutputPath;

        public ETABSDataExtractor(cSapModel etabsModel, string outputPath)
        {
            _etabsModel = etabsModel ?? throw new ArgumentNullException(nameof(etabsModel));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path must not be null or empty.", nameof(outputPath));

            // Sanitize the file name part only
            string directory = Path.GetDirectoryName(outputPath);
            string fileName = Path.GetFileName(outputPath);
            string sanitizedFileName = SanitizeFileName(fileName);

            _jsonOutputPath = Path.Combine(directory ?? "", sanitizedFileName);
        }

        public void Execute()
        {
            try
            {
                // Step 1: Extract elements from ETABS
                var elements = ExtractElements();

                // Step 2: Extract load definitions, combinations, load assignments, and area steel
                var jsonData = ExtractData(elements);

                // Step 3: Export to JSON
                ExportToJson(jsonData);
            }
            catch (Exception ex)
            {
                throw new Exception("Error extracting ETABS data: " + ex.Message);
            }
        }

        private List<StructuralElement> ExtractElements()
        {
            var elements = new List<StructuralElement>();

            // Extract area elements (slabs, shear walls)
            int numberAreaNames = 0;
            string[] areaNames = null;
            int ret = _etabsModel.AreaObj.GetNameList(ref numberAreaNames, ref areaNames);
            if (ret != 0 || areaNames == null) throw new Exception("Failed to get area object names");

            foreach (var name in areaNames)
            {
                string propName = "";
                ret = _etabsModel.AreaObj.GetProperty(name, ref propName);
                if (ret != 0) continue;

                eSlabType slabType = 0;
                eShellType shellType = 0;
                string matProp = "";
                double thickness = 0;
                int color = 0;
                string notes = "";
                string guid = "";

                ret = _etabsModel.PropArea.GetSlab(propName, ref slabType, ref shellType, ref matProp, ref thickness, ref color, ref notes, ref guid);
                if (ret != 0) continue;

                elements.Add(new StructuralElement
                {
                    Id = name,
                    Type = slabType == eSlabType.Slab ? "Slab" : "ShearWall",
                    Loads = new List<LoadAssignment>(),
                    AreaSteel = new Dictionary<string, double>()
                });
            }

            return elements;
        }

        private JsonExport ExtractData(List<StructuralElement> elements)
        {
            var jsonData = new JsonExport
            {
                LoadDefinitions = new List<LoadDefinition>(),
                LoadCombinations = new List<LoadCombination>(),
                Elements = elements
            };

            // Extract load assignments and area steel
            foreach (var element in elements)
            {
                if (element.Type == "Slab" || element.Type == "ShearWall")
                {
                    // Get uniform loads
                    int numberLoads = 0;
                    string[] areaNames = null;
                    string[] loadPat = null;
                    string[] csys = null;
                    int[] dir = null;
                    double[] value = null;

                    int ret = _etabsModel.AreaObj.GetLoadUniform(element.Id, ref numberLoads, ref areaNames, ref loadPat, ref csys, ref dir, ref value);
                    if (ret == 0 && loadPat != null && numberLoads > 0)
                    {
                        for (int i = 0; i < numberLoads; i++)
                        {
                            element.Loads.Add(new LoadAssignment
                            {
                                LoadPattern = loadPat[i],
                                LoadType = "Uniform",
                                Value = value[i],
                                Unit = "ksf",
                                Dir = dir[i]
                            });
                        }
                    }

                    // Get area steel
                    double topDir1 = 0, topDir2 = 0, botDir1 = 0, botDir2 = 0;
                    // ret = _etabsModel.DesignConcrete.GetSummaryResultsArea(element.Id, ref topDir1, ref topDir2, ref botDir1, ref botDir2);
                    // if (ret == 0)
                    {
                        element.AreaSteel["TopDir1"] = topDir1;
                        element.AreaSteel["TopDir2"] = topDir2;
                        element.AreaSteel["BotDir1"] = botDir1;
                        element.AreaSteel["BotDir2"] = botDir2;
                    }
                }
                else if (element.Type == "Column" || element.Type == "Beam")
                {
                    // Get distributed loads
                    int numberLoads = 0;
                    string[] frameNames = null;
                    string[] loadPat = null;
                    int[] myType = null;
                    string[] csys = null;
                    int[] dir = null;
                    double[] rd1 = null;
                    double[] rd2 = null;
                    double[] dist1 = null;
                    double[] dist2 = null;
                    double[] val1 = null;
                    double[] val2 = null;

                    int ret = _etabsModel.FrameObj.GetLoadDistributed(element.Id, ref numberLoads, ref frameNames, ref loadPat, ref myType, ref csys, ref dir, ref rd1, ref rd2, ref dist1, ref dist2, ref val1, ref val2, eItemType.Objects);
                    if (ret == 0 && loadPat != null && numberLoads > 0)
                    {
                        for (int i = 0; i < numberLoads; i++)
                        {
                            element.Loads.Add(new LoadAssignment
                            {
                                LoadPattern = loadPat[i],
                                LoadType = "Distributed",
                                Value = val1[i],
                                Unit = "kip/ft",
                                Dir = dir[i],
                                StartDistance = dist1[i],
                                EndDistance = dist2[i],
                                RelativeDistance = rd1[i] == rd2[i] ? rd1[i].ToString() : $"{rd1[i]}-{rd2[i]}"
                            });
                        }
                    }
                }
            }

            // Extract load patterns
            int numberPatterns = 0;
            string[] patternNames = null;
            int ret1 = _etabsModel.LoadPatterns.GetNameList(ref numberPatterns, ref patternNames);
            if (ret1 == 0 && patternNames != null && numberPatterns > 0)
            {
                foreach (var name in patternNames)
                {
                    ETABSv1.eLoadPatternType loadType = 0;
                    int ret2 = _etabsModel.LoadPatterns.GetLoadType(name, ref loadType);
                    if (ret2 == 0)
                    {
                        double selfWt = 0;
                        if (_etabsModel.LoadPatterns.GetSelfWTMultiplier(name, ref selfWt) == 0)
                        {
                            jsonData.LoadDefinitions.Add(new LoadDefinition
                            {
                                Name = name,
                                Type = Enum.IsDefined(typeof(eLoadPatternType), loadType)
                                    ? ((eLoadPatternType)loadType).ToString()
                                    : "Unknown",
                                SelfWeightMultiplier = selfWt
                            });
                        }
                        else
                        {
                            Console.WriteLine($"Failed to get self-weight multiplier for load pattern '{name}': ret = {_etabsModel.LoadPatterns.GetSelfWTMultiplier(name, ref selfWt)}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to get load type for load pattern '{name}': ret = {ret2}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Failed to get load pattern names: ret = {ret1}, patternNames = {(patternNames == null ? "null" : "non-null")}");
            }

            // Extract load combinations
            int numberCombos = 0;
            string[] comboNames = null;
            int ret3 = _etabsModel.RespCombo.GetNameList(ref numberCombos, ref comboNames);
            if (ret3 == 0 && comboNames != null)
            {
                foreach (var name in comboNames)
                {
                    int numberItems = 0;
                    string[] caseNames = null;
                    double[] sf = null;
                    eCNameType[] comboType = null;

                    int ret4 = _etabsModel.RespCombo.GetCaseList(name, ref numberItems, ref comboType, ref caseNames, ref sf);
                    if (ret4 == 0 && caseNames != null && numberItems > 0)
                    {
                        var combo = new LoadCombination
                        {
                            Name = name,
                            Factors = new List<LoadCombinationFactor>()
                        };
                        for (int i = 0; i < numberItems; i++)
                        {
                            combo.Factors.Add(new LoadCombinationFactor
                            {
                                LoadPattern = caseNames[i],
                                Factor = sf[i]
                            });
                        }
                        jsonData.LoadCombinations.Add(combo);
                    }
                }
            }

            return jsonData;
        }

        private void ExportToJson(JsonExport jsonData)
        {
            try
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(_jsonOutputPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write JSON to file
                File.WriteAllText(_jsonOutputPath, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
                Console.WriteLine($"JSON file saved to: {_jsonOutputPath}");
            }
            catch (IOException ex)
            {
                throw new Exception($"Failed to write JSON file to {_jsonOutputPath}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new Exception($"Permission denied when writing to {_jsonOutputPath}: {ex.Message}");
            }
        }

        // Helper method to sanitize file names (remove invalid characters)

        private string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
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

            string directory = Path.GetDirectoryName(outputPath);
            string fileName = Path.GetFileName(outputPath);
            string sanitizedFileName = SanitizeFileName(fileName);

            _jsonOutputPath = Path.Combine(directory ?? "", sanitizedFileName);
        }

        public void Execute()
        {
            try
            {
                var elements = ExtractElements();
                var jsonData = ExtractData(elements);
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

            // Add frame elements (beams, columns)
            int numberFrameNames = 0;
            string[] frameNames = null;
            string sAuto = ""; // Add a variable to hold the required 'SAuto' parameter

            ret = _etabsModel.FrameObj.GetNameList(ref numberFrameNames, ref frameNames);
            if (ret != 0 || frameNames == null) throw new Exception("Failed to get frame object names");

            foreach (var name in frameNames)
            {
                string propName = "";
                ret = _etabsModel.FrameObj.GetSection(name, ref propName , ref sAuto);
                if (ret != 0) continue;

                elements.Add(new StructuralElement
                {
                    Id = name,
                    Type = "Beam", // Adjust based on actual type (e.g., Column)
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

            // Extract load assignments
            foreach (var element in elements)
            {
                if (element.Type == "Slab" || element.Type == "ShearWall")
                {
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
                                ElementID = element.Id,
                                LoadPattern = loadPat[i],
                                LoadType = "Uniform",
                                Value = value[i],
                                Unit = "ksf",
                                Dir = dir[i],
                                StartDistance = null,
                                EndDistance = null,
                                RelativeDistance = null
                            });
                        }
                    }
                }
                else if (element.Type == "Beam" || element.Type == "Column")
                {
                    // Distributed loads
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
                                ElementID = element.Id,
                                LoadPattern = loadPat[i],
                                LoadType = myType[i] == 1 ? "Uniform" : "Trapezoidal",
                                Value = val1[i],
                                Unit = "kip/ft",
                                Dir = dir[i],
                                StartDistance = dist1[i],
                                EndDistance = dist2[i],
                                RelativeDistance = rd1[i] == 1.0 && rd2[i] == 1.0 ? "True" : "False"
                            });
                        }
                    }


                    // Point loads (optional, if needed)
                    int numberPointLoads = 0;
                    string[] pointFrameNames = null;
                    string[] pointLoadPat = null;
                    int[] pointType = null;  // Added missing parameter
                    string[] pointCsys = null;
                    int[] pointDir = null;
                    double[] pointRelDist = null;
                    double[] pointDist = null;
                    double[] pointVal = null;
                    string groupName = "";   // Added missing parameter

                    ret = _etabsModel.FrameObj.GetLoadPoint(
                        element.Id,
                        ref numberPointLoads,
                        ref pointFrameNames,
                        ref pointLoadPat,
                        ref pointType,       // Added missing parameter
                        ref pointCsys,
                        ref pointDir,
                        ref pointRelDist,
                        ref pointDist,
                        ref pointVal,
                         eItemType.Objects);         // Changed to pass by value

                    if (ret == 0 && pointLoadPat != null && numberPointLoads > 0)
                    {
                        for (int i = 0; i < numberPointLoads; i++)
                        {
                            element.Loads.Add(new LoadAssignment
                            {
                                ElementID = element.Id,
                                LoadPattern = pointLoadPat[i],
                                LoadType = "Point",
                                Value = pointVal[i],
                                Unit = "kip",
                                Dir = pointDir[i],
                                StartDistance = pointDist[i],
                                EndDistance = pointDist[i], // Point loads apply at a single point
                                RelativeDistance = pointRelDist[i] == 1.0 ? "True" : "False"
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
                    eLoadPatternType loadType = 0;
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
                    }
                }
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
                string directory = Path.GetDirectoryName(_jsonOutputPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_jsonOutputPath, JsonConvert.SerializeObject(jsonData, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include
                }));
                Console.WriteLine($"JSON file saved to: {_jsonOutputPath}");
            }
            catch (IOException ex)
            {
                throw new Exception($"Failed to write JSON file to {_jsonOutputPath}: {ex.Message}");
            }
        }

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
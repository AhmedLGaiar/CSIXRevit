//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Xml;
////using ETABSv17;
//using FromRevit.Data;
//using Newtonsoft.Json;

//namespace ETABSPlugin
//{
//    public class WriteStructuralWalls
//    {


//        // Entry point for the ETABS plugin
//        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
//        {
//            try
//            {
//                // List to store structural wall data
//                List<StructuralWallData> wallList = new List<StructuralWallData>();

//                // Get the number of stories
//                int numberOfStories;
//                SapModel.Story.GetNameList(out numberOfStories, out string[] storyNames);

//                // Iterate through all stories
//                for (int i = 0; i < numberOfStories; i++)
//                {
//                    // Get walls for each story
//                    int wallCount;
//                    string wallFilter = "Wall"; // Adjust if ETABS uses a different filter
//                    SapModel.AreaObj.GetNameList(storyNames[i], out wallCount, out string[] wallNames);

//                    // Process each wall
//                    for (int j = 0; j < wallCount; j++)
//                    {
//                        string wallName = wallNames[j];

//                        // Get wall properties
//                        string sectionName;
//                        SapModel.AreaObj.GetSection(wallName, out sectionName);

//                        // Get material properties
//                        string materialProp;
//                        SapModel.AreaObj.GetMaterial(wallName, out materialProp);

//                        // Get wall points
//                        int pointCount;
//                        SapModel.AreaObj.GetPoints(wallName, out pointCount, out string[] pointNames);

//                        // Ensure we have at least 2 points to define the wall
//                        if (pointCount < 2) continue;

//                        // Get coordinates of first two points to define wall
//                        double startX, startY, startZ, endX, endY, endZ;
//                        SapModel.PointObj.GetCoordCartesian(pointNames[0],  ref startX, ref startY, ref startZ);
//                        SapModel.PointObj.GetCoordCartesian(pointNames[1], ref endX, ref endY, ref endZ);

//                        // Calculate length
//                        double length = Math.Sqrt(
//                            Math.Pow(endX - startX, 2) +
//                            Math.Pow(endY - startY, 2) +
//                            Math.Pow(endZ - startZ, 2)
//                        );

//                        // Calculate orientation angle
//                        double orientation = Math.Atan2(endY - startY, endX - startX) * (180 / Math.PI);

//                        // Estimate thickness (might need adjustment based on ETABS API)
//                        double thickness = 0;
//                        if (!string.IsNullOrEmpty(sectionName))
//                        {
//                            // You might need to parse section name or use specific ETABS API calls
//                            // This is a placeholder and may need customization
//                            thickness = 0.2; // Default to 200mm
//                        }

//                        // Determine if load-bearing (might need custom logic)
//                        bool isLoadBearing = false;
//                        try
//                        {
//                            // Placeholder for load-bearing check
//                            // You may need to implement specific ETABS API calls
//                            int designOptionsType;
//                            SapModel.AreaObj.GetDesignOptions(wallName, out designOptionsType);
//                            isLoadBearing = (designOptionsType > 0);
//                        }
//                        catch
//                        {
//                            // Default to false if unable to determine
//                            isLoadBearing = false;
//                        }

//                        // Create structural wall data object
//                        StructuralWallData wallData = new StructuralWallData
//                        {
//                            Name = wallName,
//                            Story = storyNames[i],
//                            Length = length,
//                            Thickness = thickness,
//                            Section = sectionName,
//                            Material = materialProp,
//                            StartX = startX,
//                            StartY = startY,
//                            StartZ = startZ,
//                            EndX = endX,
//                            EndY = endY,
//                            EndZ = endZ,
//                            IsLoadBearing = isLoadBearing,
//                            Orientation = orientation
//                        };

//                        wallList.Add(wallData);
//                    }
//                }

//                // Serialize to JSON
//                string jsonOutput = JsonConvert.SerializeObject(new { StructuralWalls = wallList }, Formatting.Indented);

//                // Save to desktop
//                string filePath = Path.Combine(
//                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                    "ETABS_StructuralWalls.json"
//                );
//                File.WriteAllText(filePath, jsonOutput);

//                // Indicate successful completion
//                ISapPlugin.Finish(0);
//            }
//            catch (Exception ex)
//            {
//                // Log error 
//                File.WriteAllText(
//                    Path.Combine(
//                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                        "ETABS_StructuralWallExport_Error.txt"
//                    ),
//                    $"Error in structural wall export: {ex.Message}\n{ex.StackTrace}"
//                );

//                // Indicate an error in the plugin execution
//                ISapPlugin.Finish(1);
//            }
//        }
//    }
//}
using HelixToolkit.Wpf;
using StructLink_X.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Linq;

namespace StructLink_X.Services
{
    public class VisualizationService
    {
      
        public static void DrawColumn3D(HelixViewport3D viewport, ColumnRCData column, string selectedCode)
        {
            if (column == null) return;

            viewport.Children.Clear();
            var modelGroup = new ModelVisual3D();

            // التحقق من صحة البيانات وتطبيق القيم الافتراضية
            ValidateColumnData(column);

            // حساب الأبعاد بالمتر
            double width = Math.Max(column.Width / 1000.0, 0.1); // تحويل من مم إلى متر
            double depth = Math.Max(column.Depth / 1000.0, 0.1);
            double height = Math.Max(column.Height, 0.5);

            // رسم جسم العمود الخرساني
            var concreteBuilder = new MeshBuilder();
            concreteBuilder.AddBox(new Point3D(0, 0, height / 2), width, depth, height);

            bool isCompliant = CheckColumnCompliance(column, selectedCode);
            var concreteMaterial = CreateColumnMaterial(isCompliant);

            var columnGeometry = new GeometryModel3D(concreteBuilder.ToMesh(), concreteMaterial);
            var columnVisual = new ModelVisual3D { Content = columnGeometry };
            modelGroup.Children.Add(columnVisual);

            // رسم حديد التسليح الطولي
            DrawColumnLongitudinalRebar3D(modelGroup, column, width, depth, height);

            // رسم الكانات (الأساور)
            DrawColumnTies3D(modelGroup, column, width, depth, height);

            // إضافة الإضاءة المحسنة
            AddLighting(viewport);

            viewport.Children.Add(modelGroup);

            // تعديل زاوية المشاهدة لعرض أفضل
            viewport.Camera.Position = new Point3D(width * 2, depth * 2, height * 0.8);
            viewport.Camera.LookDirection = new Vector3D(-width, -depth, -height * 0.3);
            viewport.ZoomExtents();
        }

        // رسم الكمرة ثلاثي الأبعاد محسّن
        public static void DrawBeam3D(HelixViewport3D viewport, BeamRCData beam, string selectedCode)
        {
            if (beam == null) return;

            viewport.Children.Clear();
            var modelGroup = new ModelVisual3D();

            ValidateBeamData(beam);

            // حساب الأبعاد بالمتر
            double width = Math.Max(beam.Width / 1000.0, 0.1);
            double depth = Math.Max(beam.Depth / 1000.0, 0.1);
            double length = Math.Max(beam.Length, 0.5);

            // رسم جسم الكمرة الخرسانية
            var concreteBuilder = new MeshBuilder();
            concreteBuilder.AddBox(new Point3D(length / 2, 0, 0), length, width, depth);

            bool isCompliant = CheckBeamCompliance(beam, selectedCode);
            var concreteMaterial = CreateBeamMaterial(isCompliant);

            var beamGeometry = new GeometryModel3D(concreteBuilder.ToMesh(), concreteMaterial);
            var beamVisual = new ModelVisual3D { Content = beamGeometry };
            modelGroup.Children.Add(beamVisual);

            // رسم حديد التسليح الطولي
            DrawBeamLongitudinalRebar3D(modelGroup, beam, width, depth, length);

            // رسم الكانات
            DrawBeamTies3D(modelGroup, beam, width, depth, length);

            AddLighting(viewport);
            viewport.Children.Add(modelGroup);

            // تعديل زاوية المشاهدة للكمرة
            viewport.Camera.Position = new Point3D(length * 0.8, width * 2, depth * 1.5);
            viewport.Camera.LookDirection = new Vector3D(-length * 0.3, -width, -depth * 0.8);
            viewport.ZoomExtents();
        }

        // رسم مقطع العمود ثنائي الأبعاد محسّن
        public static WriteableBitmap DrawColumn2D(ColumnRCData column, string selectedCode = "")
        {
            if (column == null) return CreateEmptyBitmap();

            ValidateColumnData(column);

            int canvasWidth = 600, canvasHeight = 600;
            WriteableBitmap bitmap = new WriteableBitmap(canvasWidth, canvasHeight, 96, 96, PixelFormats.Pbgra32, null);
            bitmap.Lock();

            byte[] pixels = new byte[canvasWidth * canvasHeight * 4];
            FillBackground(pixels, Colors.White);

            // حساب المقياس والموقع - عرض المقطع العرضي
            double columnWidth = column.Width / 1000.0; // بالمتر
            double columnDepth = column.Depth / 1000.0;

            double maxDimension = Math.Max(columnWidth, columnDepth);
            double scale = Math.Min((canvasWidth - 120) / maxDimension, (canvasHeight - 120) / maxDimension);

            int rectWidth = (int)(columnWidth * scale);
            int rectHeight = (int)(columnDepth * scale);
            int startX = (canvasWidth - rectWidth) / 2;
            int startY = (canvasHeight - rectHeight) / 2;

            // رسم إطار العمود
            bool isCompliant = CheckColumnCompliance(column, selectedCode);
            Color fillColor = isCompliant ? Color.FromRgb(235, 235, 235) : Color.FromRgb(255, 220, 220);
            Color borderColor = Colors.Black;

            // رسم الخلفية
            FillRectangle(pixels, canvasWidth, canvasHeight, startX, startY, rectWidth, rectHeight, fillColor);

            // رسم الإطار الخارجي
            DrawRectangleBorder(pixels, canvasWidth, canvasHeight, startX, startY, rectWidth, rectHeight, 3, borderColor);

            // رسم حديد التسليح الطولي
            DrawColumn2DRebar(pixels, canvasWidth, canvasHeight, column, startX, startY, rectWidth, rectHeight, scale);

            // رسم الكانات
            DrawColumn2DTies(pixels, canvasWidth, canvasHeight, column, startX, startY, rectWidth, rectHeight, scale);

            // إضافة الأبعاد والتسميات
            DrawColumnDimensions2D(pixels, canvasWidth, canvasHeight, startX, startY, rectWidth, rectHeight, column, scale);

            bitmap.WritePixels(new Int32Rect(0, 0, canvasWidth, canvasHeight), pixels, canvasWidth * 4, 0);
            bitmap.Unlock();
            return bitmap;
        }

        // رسم مقطع الكمرة ثنائي الأبعاد محسّن
        public static WriteableBitmap DrawBeam2D(BeamRCData beam, string selectedCode = "")
        {
            if (beam == null) return CreateEmptyBitmap();

            ValidateBeamData(beam);

            int canvasWidth = 700, canvasHeight = 500;
            WriteableBitmap bitmap = new WriteableBitmap(canvasWidth, canvasHeight, 96, 96, PixelFormats.Pbgra32, null);
            bitmap.Lock();

            byte[] pixels = new byte[canvasWidth * canvasHeight * 4];
            FillBackground(pixels, Colors.White);

            // حساب المقياس والموقع - عرض المقطع العرضي
            double beamWidth = beam.Width / 1000.0;
            double beamDepth = beam.Depth / 1000.0;

            double maxDimension = Math.Max(beamWidth, beamDepth);
            double scale = Math.Min((canvasWidth - 140) / maxDimension, (canvasHeight - 120) / maxDimension);

            int rectWidth = (int)(beamWidth * scale);
            int rectHeight = (int)(beamDepth * scale);
            int startX = (canvasWidth - rectWidth) / 2;
            int startY = (canvasHeight - rectHeight) / 2;

            // رسم إطار الكمرة
            bool isCompliant = CheckBeamCompliance(beam, selectedCode);
            Color fillColor = isCompliant ? Color.FromRgb(230, 230, 230) : Color.FromRgb(255, 220, 220);
            Color borderColor = Colors.Black;

            FillRectangle(pixels, canvasWidth, canvasHeight, startX, startY, rectWidth, rectHeight, fillColor);
            DrawRectangleBorder(pixels, canvasWidth, canvasHeight, startX, startY, rectWidth, rectHeight, 3, borderColor);

            // رسم حديد التسليح
            DrawBeam2DRebar(pixels, canvasWidth, canvasHeight, beam, startX, startY, rectWidth, rectHeight, scale);

            // رسم الكانات
            DrawBeam2DTies(pixels, canvasWidth, canvasHeight, beam, startX, startY, rectWidth, rectHeight, scale);

            // إضافة الأبعاد والتسميات
            DrawBeamDimensions2D(pixels, canvasWidth, canvasHeight, startX, startY, rectWidth, rectHeight, beam, scale);

            bitmap.WritePixels(new Int32Rect(0, 0, canvasWidth, canvasHeight), pixels, canvasWidth * 4, 0);
            bitmap.Unlock();
            return bitmap;
        }

        #region Validation Methods

        private static void ValidateColumnData(ColumnRCData column)
        {
            // تطبيق القيم الافتراضية للبيانات غير الصحيحة
            if (column.Width <= 0) column.Width = 300; // مم
            if (column.Depth <= 0) column.Depth = 300;
            if (column.Height <= 0) column.Height = 3.0; // متر
            if (column.ConcreteCover <= 0) column.ConcreteCover = 25;
            if (column.MainBarDiameter <= 0) column.MainBarDiameter = 16;
            if (column.TieBarDiameter <= 0) column.TieBarDiameter = 8;
            if (column.TieSpacing <= 0) column.TieSpacing = 200;
            if (column.NumBarsDir2 <= 0) column.NumBarsDir2 = 2;
            if (column.NumBarsDir3 <= 0) column.NumBarsDir3 = 2;
            if (column.NumTiesDir2 <= 0) column.NumTiesDir2 = 1;
            if (column.NumTiesDir3 <= 0) column.NumTiesDir3 = 1;
        }

        private static void ValidateBeamData(BeamRCData beam)
        {
            if (beam.Width <= 0) beam.Width = 250;
            if (beam.Depth <= 0) beam.Depth = 500;
            if (beam.Length <= 0) beam.Length = 6.0;
            if (beam.ConcreteCover <= 0) beam.ConcreteCover = 25;
            if (beam.MainBarDiameter <= 0) beam.MainBarDiameter = 16;
            if (beam.TieBarDiameter <= 0) beam.TieBarDiameter = 8;
            if (beam.TieSpacing <= 0) beam.TieSpacing = 200;
            if (beam.TopBars <= 0) beam.TopBars = 2;
            if (beam.BottomBars <= 0) beam.BottomBars = 3;
        }

        #endregion

        #region 3D Drawing Methods

        private static void DrawColumnLongitudinalRebar3D(ModelVisual3D modelGroup, ColumnRCData column, double width, double depth, double height)
        {
            double cover = column.ConcreteCover / 1000.0;
            double rebarRadius = column.MainBarDiameter / 2000.0;

            var rebarPositions = CalculateColumnRebarPositions3D(width, depth, cover, column.NumBarsDir3, column.NumBarsDir2);
            var rebarMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(60, 60, 60)));

            foreach (var position in rebarPositions)
            {
                var rebarBuilder = new MeshBuilder();
                rebarBuilder.AddCylinder(
                    new Point3D(position.X, position.Y, 0),
                    new Point3D(position.X, position.Y, height),
                    rebarRadius, 12);

                var rebarGeometry = new GeometryModel3D(rebarBuilder.ToMesh(), rebarMaterial);
                var rebarVisual = new ModelVisual3D { Content = rebarGeometry };
                modelGroup.Children.Add(rebarVisual);
            }
        }

        private static void DrawColumnTies3D(ModelVisual3D modelGroup, ColumnRCData column, double width, double depth, double height)
        {
            double cover = column.ConcreteCover / 1000.0;
            double tieRadius = column.TieBarDiameter / 2000.0;
            double tieSpacing = column.TieSpacing / 1000.0;

            int tieCount = Math.Max(1, (int)Math.Ceiling(height / tieSpacing));
            var tieMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 120, 200)));

            for (int i = 0; i < tieCount; i++)
            {
                double z = tieSpacing * 0.5 + i * tieSpacing;
                if (z < height - tieSpacing * 0.25)
                {
                    DrawSingleColumnTie3D(modelGroup, width, depth, cover, z, tieRadius,
                                         column.NumTiesDir2, column.NumTiesDir3, tieMaterial);
                }
            }
        }

        private static void DrawBeamLongitudinalRebar3D(ModelVisual3D modelGroup, BeamRCData beam, double width, double depth, double length)
        {
            double cover = beam.ConcreteCover / 1000.0;
            double rebarRadius = beam.MainBarDiameter / 2000.0;
            var rebarMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(60, 60, 60)));

            // حديد التسليح السفلي
            var bottomPositions = CalculateBeamRebarPositions3D(width, beam.BottomBars, cover);
            foreach (var yPos in bottomPositions)
            {
                var rebarBuilder = new MeshBuilder();
                rebarBuilder.AddCylinder(
                    new Point3D(0, yPos, -depth / 2 + cover + rebarRadius),
                    new Point3D(length, yPos, -depth / 2 + cover + rebarRadius),
                    rebarRadius, 12);

                var rebarGeometry = new GeometryModel3D(rebarBuilder.ToMesh(), rebarMaterial);
                var rebarVisual = new ModelVisual3D { Content = rebarGeometry };
                modelGroup.Children.Add(rebarVisual);
            }

            // حديد التسليح العلوي
            var topPositions = CalculateBeamRebarPositions3D(width, beam.TopBars, cover);
            foreach (var yPos in topPositions)
            {
                var rebarBuilder = new MeshBuilder();
                rebarBuilder.AddCylinder(
                    new Point3D(0, yPos, depth / 2 - cover - rebarRadius),
                    new Point3D(length, yPos, depth / 2 - cover - rebarRadius),
                    rebarRadius, 12);

                var rebarGeometry = new GeometryModel3D(rebarBuilder.ToMesh(), rebarMaterial);
                var rebarVisual = new ModelVisual3D { Content = rebarGeometry };
                modelGroup.Children.Add(rebarVisual);
            }
        }

        private static void DrawBeamTies3D(ModelVisual3D modelGroup, BeamRCData beam, double width, double depth, double length)
        {
            double cover = beam.ConcreteCover / 1000.0;
            double tieRadius = beam.TieBarDiameter / 2000.0;
            double tieSpacing = beam.TieSpacing / 1000.0;

            int tieCount = Math.Max(1, (int)Math.Ceiling(length / tieSpacing));
            var tieMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 120, 200)));

            for (int i = 0; i < tieCount; i++)
            {
                double x = tieSpacing * 0.5 + i * tieSpacing;
                if (x < length - tieSpacing * 0.25)
                {
                    DrawSingleBeamTie3D(modelGroup, x, width, depth, cover, tieRadius, tieMaterial);
                }
            }
        }

        private static void DrawSingleColumnTie3D(ModelVisual3D modelGroup, double width, double depth, double cover,
                                                 double z, double radius, int numTiesDir2, int numTiesDir3, Material material)
        {
            var tieBuilder = new MeshBuilder();

            // الكانة الخارجية الرئيسية
            double innerWidth = width - 2 * cover;
            double innerDepth = depth - 2 * cover;

            // رسم الكانة المستطيلة
            var points = new List<Point3D>
            {
                new Point3D(-innerWidth/2, -innerDepth/2, z),
                new Point3D(innerWidth/2, -innerDepth/2, z),
                new Point3D(innerWidth/2, innerDepth/2, z),
                new Point3D(-innerWidth/2, innerDepth/2, z),
                new Point3D(-innerWidth/2, -innerDepth/2, z)
            };

            for (int i = 0; i < points.Count - 1; i++)
            {
                tieBuilder.AddCylinder(points[i], points[i + 1], radius, 8);
            }

            var tieGeometry = new GeometryModel3D(tieBuilder.ToMesh(), material);
            var tieVisual = new ModelVisual3D { Content = tieGeometry };
            modelGroup.Children.Add(tieVisual);
        }

        private static void DrawSingleBeamTie3D(ModelVisual3D modelGroup, double x, double width, double depth,
                                               double cover, double radius, Material material)
        {
            var tieBuilder = new MeshBuilder();

            double innerWidth = width - 2 * cover;
            double innerDepth = depth - 2 * cover;

            // رسم الكانة المستطيلة للكمرة
            var points = new List<Point3D>
            {
                new Point3D(x, -innerWidth/2, -innerDepth/2),
                new Point3D(x, innerWidth/2, -innerDepth/2),
                new Point3D(x, innerWidth/2, innerDepth/2),
                new Point3D(x, -innerWidth/2, innerDepth/2),
                new Point3D(x, -innerWidth/2, -innerDepth/2)
            };

            for (int i = 0; i < points.Count - 1; i++)
            {
                tieBuilder.AddCylinder(points[i], points[i + 1], radius, 8);
            }

            var tieGeometry = new GeometryModel3D(tieBuilder.ToMesh(), material);
            var tieVisual = new ModelVisual3D { Content = tieGeometry };
            modelGroup.Children.Add(tieVisual);
        }

        #endregion

        #region 2D Drawing Methods

        private static void DrawColumn2DRebar(byte[] pixels, int canvasWidth, int canvasHeight, ColumnRCData column,
                                            int startX, int startY, int rectWidth, int rectHeight, double scale)
        {
            double cover = column.ConcreteCover / 1000.0 * scale;
            int rebarRadius = Math.Max((int)(column.MainBarDiameter / 2000.0 * scale), 4);

            var rebarPositions = CalculateColumnRebarPositions2D(rectWidth, rectHeight, cover,
                                                               column.NumBarsDir3, column.NumBarsDir2);

            Color rebarColor = Color.FromRgb(40, 40, 40);
            foreach (var pos in rebarPositions)
            {
                int centerX = startX + (int)pos.X;
                int centerY = startY + (int)pos.Y;
                DrawFilledCircle(pixels, canvasWidth, canvasHeight, centerX, centerY, rebarRadius, rebarColor);
                DrawCircleBorder(pixels, canvasWidth, canvasHeight, centerX, centerY, rebarRadius, 1, Colors.Black);
            }
        }

        private static void DrawColumn2DTies(byte[] pixels, int canvasWidth, int canvasHeight, ColumnRCData column,
                                           int startX, int startY, int rectWidth, int rectHeight, double scale)
        {
            double cover = column.ConcreteCover / 1000.0 * scale;
            int tieThickness = Math.Max((int)(column.TieBarDiameter / 1000.0 * scale), 2);
            Color tieColor = Color.FromRgb(0, 100, 180);

            // رسم الكانة الخارجية الرئيسية
            DrawRectangleBorder(pixels, canvasWidth, canvasHeight,
                              (int)(startX + cover), (int)(startY + cover),
                              (int)(rectWidth - 2 * cover), (int)(rectHeight - 2 * cover),
                              tieThickness, tieColor);

            // رسم الكانات الإضافية حسب العدد المطلوب
            if (column.NumTiesDir2 > 1 || column.NumTiesDir3 > 1)
            {
                DrawAdditionalColumnTies2D(pixels, canvasWidth, canvasHeight, startX, startY,
                                         rectWidth, rectHeight, cover, tieThickness, tieColor,
                                         column.NumTiesDir2, column.NumTiesDir3);
            }
        }

        private static void DrawBeam2DRebar(byte[] pixels, int canvasWidth, int canvasHeight, BeamRCData beam,
                                          int startX, int startY, int rectWidth, int rectHeight, double scale)
        {
            double cover = beam.ConcreteCover / 1000.0 * scale;
            int rebarRadius = Math.Max((int)(beam.MainBarDiameter / 2000.0 * scale), 4);
            Color rebarColor = Color.FromRgb(40, 40, 40);

            // حديد التسليح السفلي
            var bottomPositions = CalculateBeamRebarPositions2D(rectWidth, beam.BottomBars, (int)cover);
            foreach (int xPos in bottomPositions)
            {
                int centerX = startX + xPos;
                int centerY = startY + rectHeight - (int)cover;
                DrawFilledCircle(pixels, canvasWidth, canvasHeight, centerX, centerY, rebarRadius, rebarColor);
                DrawCircleBorder(pixels, canvasWidth, canvasHeight, centerX, centerY, rebarRadius, 1, Colors.Black);
            }

            // حديد التسليح العلوي
            var topPositions = CalculateBeamRebarPositions2D(rectWidth, beam.TopBars, (int)cover);
            foreach (int xPos in topPositions)
            {
                int centerX = startX + xPos;
                int centerY = startY + (int)cover;
                DrawFilledCircle(pixels, canvasWidth, canvasHeight, centerX, centerY, rebarRadius, rebarColor);
                DrawCircleBorder(pixels, canvasWidth, canvasHeight, centerX, centerY, rebarRadius, 1, Colors.Black);
            }
        }

        private static void DrawBeam2DTies(byte[] pixels, int canvasWidth, int canvasHeight, BeamRCData beam,
                                         int startX, int startY, int rectWidth, int rectHeight, double scale)
        {
            double cover = beam.ConcreteCover / 1000.0 * scale;
            int tieThickness = Math.Max((int)(beam.TieBarDiameter / 1000.0 * scale), 2);
            Color tieColor = Color.FromRgb(0, 100, 180);

            // رسم الكانة الخارجية
            DrawRectangleBorder(pixels, canvasWidth, canvasHeight,
                              (int)(startX + cover), (int)(startY + cover),
                              (int)(rectWidth - 2 * cover), (int)(rectHeight - 2 * cover),
                              tieThickness, tieColor);
        }

        #endregion

        #region Helper Methods

        private static List<Point3D> CalculateColumnRebarPositions3D(double width, double depth, double cover,
                                                                    int numBarsDir3, int numBarsDir2)
        {
            var positions = new List<Point3D>();

            // توزيع الحديد على المحيط الخارجي
            if (numBarsDir3 >= 2)
            {
                double spacingX = numBarsDir3 > 2 ? (width - 2 * cover) / (numBarsDir3 - 1) : 0;
                for (int i = 0; i < numBarsDir3; i++)
                {
                    double x = -width / 2 + cover + i * spacingX;
                    positions.Add(new Point3D(x, -depth / 2 + cover, 0)); // الحافة السفلية
                    if (numBarsDir2 > 2) // إضافة على الحافة العلوية فقط إذا كان هناك أكثر من قضيبين في الاتجاه الآخر
                        positions.Add(new Point3D(x, depth / 2 - cover, 0));
                }
            }

            if (numBarsDir2 >= 2)
            {
                double spacingY = numBarsDir2 > 2 ? (depth - 2 * cover) / (numBarsDir2 - 1) : depth - 2 * cover;
                for (int i = 1; i < numBarsDir2 - 1; i++) // تجنب التكرار في الأركان
                {
                    double y = -depth / 2 + cover + i * spacingY;
                    positions.Add(new Point3D(-width / 2 + cover, y, 0)); // الحافة اليسرى
                    positions.Add(new Point3D(width / 2 - cover, y, 0));  // الحافة اليمنى
                }
            }

            return positions;
        }

        private static List<Point> CalculateColumnRebarPositions2D(int width, int height, double cover,
                                                                  int numBarsDir3, int numBarsDir2)
        {
            var positions = new List<Point>();

            if (numBarsDir3 >= 2)
            {
                double spacingX = numBarsDir3 > 2 ? (width - 2 * cover) / (numBarsDir3 - 1) : 0;
                for (int i = 0; i < numBarsDir3; i++)
                {
                    double x = cover + i * spacingX;
                    positions.Add(new Point((int)x, (int)cover));
                    if (numBarsDir2 > 2)
                        positions.Add(new Point((int)x, height - (int)cover));
                }
            }

            if (numBarsDir2 >= 2)
            {
                double spacingY = numBarsDir2 > 2 ? (height - 2 * cover) / (numBarsDir2 - 1) : height - 2 * cover;
                for (int i = 1; i < numBarsDir2 - 1; i++)
                {
                    double y = cover + i * spacingY;
                    positions.Add(new Point((int)cover, (int)y));
                    positions.Add(new Point(width - (int)cover, (int)y));
                }
            }

            return positions;
        }

        private static List<double> CalculateBeamRebarPositions3D(double width, int barCount, double cover)
        {
            var positions = new List<double>();

            if (barCount == 1)
            {
                positions.Add(0);
            }
            else if (barCount >= 2)
            {
                double availableWidth = width - 2 * cover;
                double spacing = barCount > 1 ? availableWidth / (barCount - 1) : 0;

                for (int i = 0; i < barCount; i++)
                {
                    double yPos = -width / 2 + cover + i * spacing;
                    positions.Add(yPos);
                }
            }

            return positions;
        }

        private static List<int> CalculateBeamRebarPositions2D(int width, int barCount, int cover)
        {
            var positions = new List<int>();

            if (barCount == 1)
            {
                positions.Add(width / 2);
            }
            else if (barCount >= 2)
            {
                int availableWidth = width - 2 * cover;
                double spacing = barCount > 1 ? (double)availableWidth / (barCount - 1) : 0;

                for (int i = 0; i < barCount; i++)
                {
                    int xPos = cover + (int)(i * spacing);
                    positions.Add(xPos);
                }
            }

            return positions;
        }

        private static void DrawAdditionalColumnTies2D(byte[] pixels, int canvasWidth, int canvasHeight,
                                                      int startX, int startY, int rectWidth, int rectHeight,
                                                      double cover, int thickness, Color color,
                                                      int numTiesDir2, int numTiesDir3)
        {
            // رسم الكانات الداخلية الإضافية للأعمدة الكبيرة
            if (numTiesDir2 > 1)
            {
                double spacingY = (rectHeight - 2 * cover) / (numTiesDir2 + 1);
                for (int i = 1; i <= numTiesDir2 - 1; i++)
                {
                    int y = (int)(startY + cover + i * spacingY);
                    DrawLine(pixels, canvasWidth, canvasHeight,
                            (int)(startX + cover), y,
                            (int)(startX + rectWidth - cover), y,
                            thickness, color);
                }
            }

            if (numTiesDir3 > 1)
            {
                double spacingX = (rectWidth - 2 * cover) / (numTiesDir3 + 1);
                for (int i = 1; i <= numTiesDir3 - 1; i++)
                {
                    int x = (int)(startX + cover + i * spacingX);
                    DrawLine(pixels, canvasWidth, canvasHeight,
                            x, (int)(startY + cover),
                            x, (int)(startY + rectHeight - cover),
                            thickness, color);
                }
            }
        }

        private static void DrawColumnDimensions2D(byte[] pixels, int canvasWidth, int canvasHeight,
                                                  int startX, int startY, int rectWidth, int rectHeight,
                                                  ColumnRCData column, double scale)
        {
            Color textColor = Colors.Black;
            int fontSize = 12;

            // رسم أبعاد العرض (أسفل المقطع)
            string widthText = $"{column.Width} mm";
            DrawText(pixels, canvasWidth, canvasHeight, widthText,
                    startX + rectWidth / 2, startY + rectHeight + 30,
                    fontSize, textColor, true);

            // رسم أبعاد العمق (يمين المقطع)
            string depthText = $"{column.Depth} mm";
            DrawText(pixels, canvasWidth, canvasHeight, depthText,
                    startX + rectWidth + 40, startY + rectHeight / 2,
                    fontSize, textColor, false);

            // رسم معلومات التسليح
            string rebarInfo = $"φ{column.MainBarDiameter} - {column.NumBarsDir2}×{column.NumBarsDir3}";
            DrawText(pixels, canvasWidth, canvasHeight, rebarInfo,
                    startX, startY - 20, fontSize, textColor, false);

            // رسم معلومات الكانات
            string tieInfo = $"φ{column.TieBarDiameter}@{column.TieSpacing}mm";
            DrawText(pixels, canvasWidth, canvasHeight, tieInfo,
                    startX, startY - 40, fontSize, textColor, false);
        }

        private static void DrawBeamDimensions2D(byte[] pixels, int canvasWidth, int canvasHeight,
                                               int startX, int startY, int rectWidth, int rectHeight,
                                               BeamRCData beam, double scale)
        {
            Color textColor = Colors.Black;
            int fontSize = 12;

            // رسم أبعاد العرض
            string widthText = $"{beam.Width} mm";
            DrawText(pixels, canvasWidth, canvasHeight, widthText,
                    startX + rectWidth / 2, startY + rectHeight + 30,
                    fontSize, textColor, true);

            // رسم أبعاد العمق
            string depthText = $"{beam.Depth} mm";
            DrawText(pixels, canvasWidth, canvasHeight, depthText,
                    startX + rectWidth + 40, startY + rectHeight / 2,
                    fontSize, textColor, false);

            // رسم معلومات التسليح العلوي والسفلي
            string topRebarInfo = $"Top: {beam.TopBars}φ{beam.MainBarDiameter}";
            DrawText(pixels, canvasWidth, canvasHeight, topRebarInfo,
                    startX, startY - 20, fontSize, textColor, false);

            string bottomRebarInfo = $"Bottom: {beam.BottomBars}φ{beam.MainBarDiameter}";
            DrawText(pixels, canvasWidth, canvasHeight, bottomRebarInfo,
                    startX, startY - 40, fontSize, textColor, false);

            // رسم معلومات الكانات
            string tieInfo = $"φ{beam.TieBarDiameter}@{beam.TieSpacing}mm";
            DrawText(pixels, canvasWidth, canvasHeight, tieInfo,
                    startX, startY - 60, fontSize, textColor, false);
        }

        #endregion

        #region Material and Lighting Methods

        private static Material CreateColumnMaterial(bool isCompliant)
        {
            Color color = isCompliant ? Color.FromRgb(220, 220, 220) : Color.FromRgb(255, 200, 200);
            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), 20));
            return material;
        }

        private static Material CreateBeamMaterial(bool isCompliant)
        {
            Color color = isCompliant ? Color.FromRgb(210, 210, 210) : Color.FromRgb(255, 200, 200);
            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), 20));
            return material;
        }

        private static void AddLighting(HelixViewport3D viewport)
        {
            // إضافة إضاءة محيطة
            var ambientLight = new AmbientLight(Colors.Gray);
            var ambientVisual = new ModelVisual3D { Content = ambientLight };
            viewport.Children.Add(ambientVisual);

            // إضافة إضاءة اتجاهية رئيسية
            var directionalLight1 = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1));
            var directionalVisual1 = new ModelVisual3D { Content = directionalLight1 };
            viewport.Children.Add(directionalVisual1);

            // إضافة إضاءة اتجاهية ثانوية
            var directionalLight2 = new DirectionalLight(Colors.LightGray, new Vector3D(1, 1, 1));
            var directionalVisual2 = new ModelVisual3D { Content = directionalLight2 };
            viewport.Children.Add(directionalVisual2);
        }

        #endregion

        #region Compliance Check Methods

        private static bool CheckColumnCompliance(ColumnRCData column, string selectedCode)
        {
            // فحص بسيط للامتثال للكود المحدد
            if (string.IsNullOrEmpty(selectedCode)) return true;

            // فحص الحد الأدنى للأبعاد
            if (column.Width < 200 || column.Depth < 200) return false;

            // فحص الحد الأدنى للغطاء الخرساني
            if (column.ConcreteCover < 20) return false;

            // فحص الحد الأدنى لقطر الحديد الطولي
            if (column.MainBarDiameter < 12) return false;

            // فحص تباعد الكانات
            if (column.TieSpacing > Math.Min(column.Width, column.Depth) * 0.75) return false;

            return true;
        }

        private static bool CheckBeamCompliance(BeamRCData beam, string selectedCode)
        {
            if (string.IsNullOrEmpty(selectedCode)) return true;

            // فحص الحد الأدنى للأبعاد
            if (beam.Width < 200 || beam.Depth < 300) return false;

            // فحص نسبة العمق إلى العرض
            if (beam.Depth / beam.Width < 1.2) return false;

            // فحص الحد الأدنى للغطاء الخرساني
            if (beam.ConcreteCover < 20) return false;

            // فحص الحد الأدنى لعدد الأسياخ
            if (beam.TopBars < 2 || beam.BottomBars < 2) return false;

            return true;
        }

        #endregion

        #region Drawing Utility Methods

        private static WriteableBitmap CreateEmptyBitmap()
        {
            int width = 400, height = 400;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            bitmap.Lock();

            byte[] pixels = new byte[width * height * 4];
            FillBackground(pixels, Colors.LightGray);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
            bitmap.Unlock();
            return bitmap;
        }

        private static void FillBackground(byte[] pixels, Color color)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = color.B;     // Blue
                pixels[i + 1] = color.G; // Green
                pixels[i + 2] = color.R; // Red
                pixels[i + 3] = color.A; // Alpha
            }
        }

        private static void FillRectangle(byte[] pixels, int canvasWidth, int canvasHeight,
                                        int x, int y, int width, int height, Color color)
        {
            for (int row = y; row < y + height && row < canvasHeight; row++)
            {
                for (int col = x; col < x + width && col < canvasWidth; col++)
                {
                    if (row >= 0 && col >= 0)
                    {
                        int index = (row * canvasWidth + col) * 4;
                        if (index + 3 < pixels.Length)
                        {
                            pixels[index] = color.B;
                            pixels[index + 1] = color.G;
                            pixels[index + 2] = color.R;
                            pixels[index + 3] = color.A;
                        }
                    }
                }
            }
        }

        private static void DrawRectangleBorder(byte[] pixels, int canvasWidth, int canvasHeight,
                                              int x, int y, int width, int height, int thickness, Color color)
        {
            // رسم الحافة العلوية
            FillRectangle(pixels, canvasWidth, canvasHeight, x, y, width, thickness, color);
            // رسم الحافة السفلية
            FillRectangle(pixels, canvasWidth, canvasHeight, x, y + height - thickness, width, thickness, color);
            // رسم الحافة اليسرى
            FillRectangle(pixels, canvasWidth, canvasHeight, x, y, thickness, height, color);
            // رسم الحافة اليمنى
            FillRectangle(pixels, canvasWidth, canvasHeight, x + width - thickness, y, thickness, height, color);
        }

        private static void DrawFilledCircle(byte[] pixels, int canvasWidth, int canvasHeight,
                                           int centerX, int centerY, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int pixelX = centerX + x;
                        int pixelY = centerY + y;

                        if (pixelX >= 0 && pixelX < canvasWidth && pixelY >= 0 && pixelY < canvasHeight)
                        {
                            int index = (pixelY * canvasWidth + pixelX) * 4;
                            if (index + 3 < pixels.Length)
                            {
                                pixels[index] = color.B;
                                pixels[index + 1] = color.G;
                                pixels[index + 2] = color.R;
                                pixels[index + 3] = color.A;
                            }
                        }
                    }
                }
            }
        }

        private static void DrawCircleBorder(byte[] pixels, int canvasWidth, int canvasHeight,
                                           int centerX, int centerY, int radius, int thickness, Color color)
        {
            for (int y = -radius - thickness; y <= radius + thickness; y++)
            {
                for (int x = -radius - thickness; x <= radius + thickness; x++)
                {
                    double distance = Math.Sqrt(x * x + y * y);
                    if (distance >= radius - thickness && distance <= radius + thickness)
                    {
                        int pixelX = centerX + x;
                        int pixelY = centerY + y;

                        if (pixelX >= 0 && pixelX < canvasWidth && pixelY >= 0 && pixelY < canvasHeight)
                        {
                            int index = (pixelY * canvasWidth + pixelX) * 4;
                            if (index + 3 < pixels.Length)
                            {
                                pixels[index] = color.B;
                                pixels[index + 1] = color.G;
                                pixels[index + 2] = color.R;
                                pixels[index + 3] = color.A;
                            }
                        }
                    }
                }
            }
        }

        private static void DrawLine(byte[] pixels, int canvasWidth, int canvasHeight,
                                   int x1, int y1, int x2, int y2, int thickness, Color color)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;

            while (true)
            {
                // رسم نقطة بسمك محدد
                for (int i = -thickness / 2; i <= thickness / 2; i++)
                {
                    for (int j = -thickness / 2; j <= thickness / 2; j++)
                    {
                        int pixelX = x + i;
                        int pixelY = y + j;

                        if (pixelX >= 0 && pixelX < canvasWidth && pixelY >= 0 && pixelY < canvasHeight)
                        {
                            int index = (pixelY * canvasWidth + pixelX) * 4;
                            if (index + 3 < pixels.Length)
                            {
                                pixels[index] = color.B;
                                pixels[index + 1] = color.G;
                                pixels[index + 2] = color.R;
                                pixels[index + 3] = color.A;
                            }
                        }
                    }
                }

                if (x == x2 && y == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void DrawText(byte[] pixels, int canvasWidth, int canvasHeight, string text,
                                   int x, int y, int fontSize, Color color, bool centered)
        {
            // تنفيذ بسيط لرسم النص - يمكن تحسينه باستخدام مكتبة نصوص متقدمة
            // هذا مجرد placeholder للتوضيح
            int textWidth = text.Length * fontSize / 2;
            if (centered)
            {
                x -= textWidth / 2;
            }

            // يمكن إضافة تنفيذ فعلي لرسم النص هنا
            // أو استخدام DrawingContext في WPF
        }

        #endregion
    }
}
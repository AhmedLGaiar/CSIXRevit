using iTextSharp.text;
using iTextSharp.text.pdf;
using StructLink_X.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StructLink_X.Services
{
    public class PdfExporter
    {
        // Define consistent styling
        private static readonly Font TitleFont = new Font(Font.FontFamily.HELVETICA, 16, Font.BOLD, BaseColor.BLACK);
        private static readonly Font SectionFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK);
        private static readonly Font HeaderFont = new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD, BaseColor.BLACK);
        private static readonly Font DateFont = new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, BaseColor.DARK_GRAY);
        private static readonly Font TableHeaderFont = new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD, BaseColor.WHITE);
        private static readonly Font TableDataFont = new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font FooterFont = new Font(Font.FontFamily.HELVETICA, 9, Font.ITALIC, BaseColor.DARK_GRAY);

        private static readonly BaseColor HeaderColor = new BaseColor(0, 122, 204); // Professional blue
        private static readonly BaseColor AlternateRowColor = new BaseColor(248, 248, 248); // Light gray
        private static readonly BaseColor SummaryHeaderColor = new BaseColor(76, 175, 80); // Green accent

        public static void ExportToPDF(IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams, string filePath, string selectedCode)
        {
            Document doc = new Document(PageSize.A4.Rotate(), 20f, 20f, 25f, 25f);
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            // Add professional header
            AddHeader(doc, selectedCode);

            // Add project summary first
            AddProjectSummary(doc, columns, beams);

            // Add columns section
            if (columns != null && columns.Any())
            {
                AddColumnsSection(doc, columns);
            }

            // Add beams section
            if (beams != null && beams.Any())
            {
                AddBeamsSection(doc, beams);
            }

            // Add footer
            AddFooter(doc);

            doc.Close();
        }

        private static void AddHeader(Document doc, string selectedCode)
        {
            // Professional header with logo and project info
            PdfPTable headerTable = new PdfPTable(3);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 1f, 3f, 1.5f });

            // Logo/Company cell
            PdfPCell logoCell = new PdfPCell(new Phrase("SLX", new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, BaseColor.WHITE)));
            logoCell.BackgroundColor = HeaderColor;
            logoCell.Border = Rectangle.NO_BORDER;
            logoCell.HorizontalAlignment = Element.ALIGN_CENTER;
            logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            logoCell.Padding = 10;
            headerTable.AddCell(logoCell);

            // Title cell
            PdfPCell titleCell = new PdfPCell();
            titleCell.AddElement(new Paragraph("StructLink_X - Enhanced Structural Analysis Report", TitleFont));
            titleCell.Border = Rectangle.NO_BORDER;
            titleCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            titleCell.Padding = 10;
            headerTable.AddCell(titleCell);

            // Date and project info cell
            PdfPCell infoCell = new PdfPCell();
            infoCell.AddElement(new Paragraph($"Generated: {DateTime.Now:dd MMM yyyy}", new Font(Font.FontFamily.HELVETICA, 9, Font.BOLD, BaseColor.BLACK)));
            infoCell.AddElement(new Paragraph($"Time: {DateTime.Now:HH:mm}", DateFont));
            infoCell.AddElement(new Paragraph($"Page: 1", DateFont));
            infoCell.Border = Rectangle.NO_BORDER;
            infoCell.HorizontalAlignment = Element.ALIGN_RIGHT;
            infoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            infoCell.Padding = 10;
            headerTable.AddCell(infoCell);

            doc.Add(headerTable);
            doc.Add(new Paragraph("\n"));
        }

        private static void AddColumnsSection(Document doc, IEnumerable<ColumnRCData> columns)
        {
            // Section header with count
            int columnCount = columns.Count();
            doc.Add(new Paragraph($"COLUMNS ({columnCount} Types)", SectionFont));
            doc.Add(new Paragraph("\n"));

            // Enhanced columns table with better layout
            PdfPTable columnTable = new PdfPTable(11);
            columnTable.WidthPercentage = 100;
            columnTable.SetWidths(new float[] { 0.6f, 1.2f, 1f, 0.7f, 1.1f, 1.1f, 1.1f, 0.8f, 1f, 0.8f, 1f });

            string[] columnHeaders = {
                "ID", "Section", "Size (mm)", "Qty", "Main Bars", "Dir-2 Bars",
                "Dir-3 Bars", "Tie Ø", "Tie Spacing", "Cover", "Height (m)"
            };

            // Add headers
            foreach (string header in columnHeaders)
            {
                PdfPCell cell = CreateHeaderCell(header);
                columnTable.AddCell(cell);
            }

            // Add data rows
            int idCounter = 1;
            foreach (var column in columns)
            {
                ValidateColumnData(column, idCounter);

                int tieBarDiameter = column.TieBarDiameter > 0 ? column.TieBarDiameter : 8;
                int totalMainBars = column.NumBarsDir3 + column.NumBarsDir2;
                string sectionName = GetSectionName(column.SectionName, column.UniqueName, "COL", idCounter);

                var cellData = new string[]
                {
                    idCounter.ToString(),
                    sectionName,
                    $"{column.Width}×{column.Depth}",
                    column.SectionCount.ToString(),
                    $"{totalMainBars}T{column.MainBarDiameter}",
                    $"{column.NumBarsDir2}T{column.MainBarDiameter}",
                    $"{column.NumBarsDir3}T{column.MainBarDiameter}",
                    $"T{tieBarDiameter}",
                    $"{column.TieSpacing:F0}",
                    $"{column.ConcreteCover:F0}",
                    $"{column.Height:F1}"
                };

                AddTableRow(columnTable, cellData, idCounter % 2 == 0);
                idCounter++;
            }

            doc.Add(columnTable);
            doc.Add(new Paragraph("\n"));
        }

        private static void AddBeamsSection(Document doc, IEnumerable<BeamRCData> beams)
        {
            // Group beams by their properties to remove duplicates and count quantities
            var groupedBeams = GroupBeamsByProperties(beams);

            int uniqueBeamTypes = groupedBeams.Count();
            int totalBeamCount = groupedBeams.Sum(g => g.Count());

            doc.Add(new Paragraph($"BEAMS ({uniqueBeamTypes} Unique Types, {totalBeamCount} Total)", SectionFont));
            doc.Add(new Paragraph("\n"));

            // Enhanced beams table with quantity column
            PdfPTable beamTable = new PdfPTable(10);
            beamTable.WidthPercentage = 100;
            beamTable.SetWidths(new float[] { 0.6f, 1.2f, 1f, 0.7f, 1.1f, 1.1f, 0.8f, 1f, 0.8f, 1f });

            string[] beamHeaders = {
                "ID", "Section", "Size (mm)", "Qty", "Top Bars", "Bottom Bars",
                "Tie Ø", "Tie Spacing", "Legs", "Cover"
            };

            // Add headers
            foreach (string header in beamHeaders)
            {
                PdfPCell cell = CreateHeaderCell(header);
                beamTable.AddCell(cell);
            }

            // Add data rows
            int idCounter = 1;
            foreach (var group in groupedBeams.OrderBy(g => g.Key.Width).ThenBy(g => g.Key.Depth))
            {
                var beam = group.First(); // Representative beam from the group
                ValidateBeamData(beam, idCounter);

                int tieBarDiameter = beam.TieBarDiameter > 0 ? beam.TieBarDiameter : 8;
                string sectionName = GetGroupedSectionName(group.ToList(), idCounter);

                var cellData = new string[]
                {
                    idCounter.ToString(),
                    sectionName,
                    $"{beam.Width}×{beam.Depth}",
                    group.Count().ToString(),
                    $"{beam.TopBars}T{beam.MainBarDiameter}",
                    $"{beam.BottomBars}T{beam.MainBarDiameter}",
                    $"T{tieBarDiameter}",
                    $"{beam.TieSpacing:F0}",
                    beam.NumOfLegs.ToString(),
                    $"{beam.ConcreteCover:F0}"
                };

                AddTableRow(beamTable, cellData, idCounter % 2 == 0);
                idCounter++;
            }

            doc.Add(beamTable);
            doc.Add(new Paragraph("\n"));
        }

        private static void AddProjectSummary(Document doc, IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams)
        {
            doc.Add(new Paragraph("PROJECT SUMMARY", SectionFont));
            doc.Add(new Paragraph("\n"));

            PdfPTable summaryTable = new PdfPTable(3); // Reduced to 3 columns
            summaryTable.WidthPercentage = 90;
            summaryTable.SetWidths(new float[] { 2f, 1f, 1f }); // Adjusted widths for 3 columns

            string[] summaryHeaders = { "Element Type", "Unique Types", "Total Elements" }; // Removed Avg Dimensions and Total Volume

            foreach (string header in summaryHeaders)
            {
                PdfPCell cell = CreateHeaderCell(header);
                cell.BackgroundColor = SummaryHeaderColor;
                summaryTable.AddCell(cell);
            }

            // Calculate summary data
            int columnTypes = columns?.Count() ?? 0;
            int totalColumnElements = columns?.Sum(c => c.SectionCount) ?? 0;

            // Group beams to get accurate counts
            var groupedBeams = beams != null ? GroupBeamsByProperties(beams) : new List<IGrouping<BeamKey, BeamRCData>>();
            int beamTypes = groupedBeams.Count();
            int totalBeamElements = groupedBeams.Sum(g => g.Count());

            var summaryData = new string[][]
            {
                new string[] { "Columns", columnTypes.ToString(), totalColumnElements.ToString() },
                new string[] { "Beams", beamTypes.ToString(), totalBeamElements.ToString() },
                new string[] { "TOTAL", (columnTypes + beamTypes).ToString(), (totalColumnElements + totalBeamElements).ToString() }
            };

            for (int i = 0; i < summaryData.Length; i++)
            {
                bool isTotal = i == summaryData.Length - 1;
                AddTableRow(summaryTable, summaryData[i], false, isTotal);
            }

            doc.Add(summaryTable);
            doc.Add(new Paragraph("\n"));
        }

        private static void AddFooter(Document doc)
        {
            // Add a separator line
            doc.Add(new Paragraph("_".PadRight(150, '_'), FooterFont));
            doc.Add(new Paragraph("\n"));

            // Footer information
            PdfPTable footerTable = new PdfPTable(2);
            footerTable.WidthPercentage = 100;
            footerTable.SetWidths(new float[] { 1f, 1f });

            PdfPCell leftFooter = new PdfPCell(new Phrase("Report generated by StructLink_X v2.0", FooterFont));
            leftFooter.Border = Rectangle.NO_BORDER;
            leftFooter.HorizontalAlignment = Element.ALIGN_LEFT;
            footerTable.AddCell(leftFooter);

            PdfPCell rightFooter = new PdfPCell(new Phrase("Thank you for using StructLink_X", FooterFont));
            rightFooter.Border = Rectangle.NO_BORDER;
            rightFooter.HorizontalAlignment = Element.ALIGN_RIGHT;
            footerTable.AddCell(rightFooter);

            doc.Add(footerTable);
        }

        // Helper methods
        private static PdfPCell CreateHeaderCell(string text)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, TableHeaderFont));
            cell.BackgroundColor = HeaderColor;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 6;
            return cell;
        }

        private static void AddTableRow(PdfPTable table, string[] data, bool alternateRow, bool isTotalRow = false)
        {
            Font font = isTotalRow ? new Font(Font.FontFamily.HELVETICA, 7, Font.BOLD, BaseColor.BLACK) : TableDataFont;
            BaseColor bgColor = isTotalRow ? new BaseColor(220, 220, 220) :
                               (alternateRow ? AlternateRowColor : BaseColor.WHITE);

            foreach (string item in data)
            {
                PdfPCell cell = new PdfPCell(new Phrase(item, font));
                cell.BackgroundColor = bgColor;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                cell.Padding = 4;
                table.AddCell(cell);
            }
        }

        private static string GetSectionName(string sectionName, string uniqueName, string prefix, int id)
        {
            return sectionName ?? uniqueName ?? $"{prefix}-{id:D3}";
        }

        private static void ValidateColumnData(ColumnRCData column, int id)
        {
            if (column.MainBarDiameter <= 0)
                throw new ArgumentException($"MainBarDiameter must be positive for column {id}.");
            if (column.TieSpacing <= 0)
                throw new ArgumentException($"TieSpacing must be positive for column {id}.");
            if (column.Width <= 0 || column.Depth <= 0)
                throw new ArgumentException($"Column dimensions must be positive for column {id}.");
        }

        private static void ValidateBeamData(BeamRCData beam, int id)
        {
            if (beam.MainBarDiameter <= 0)
                throw new ArgumentException($"MainBarDiameter must be positive for beam {id}.");
            if (beam.TieSpacing <= 0)
                throw new ArgumentException($"TieSpacing must be positive for beam {id}.");
            if (beam.NumOfLegs <= 0)
                throw new ArgumentException($"NumOfLegs must be positive for beam {id}.");
            if (beam.Width <= 0 || beam.Depth <= 0)
                throw new ArgumentException($"Beam dimensions must be positive for beam {id}.");
        }

        // Beam grouping helper methods
        private static IEnumerable<IGrouping<BeamKey, BeamRCData>> GroupBeamsByProperties(IEnumerable<BeamRCData> beams)
        {
            return beams.GroupBy(beam => new BeamKey
            {
                Width = beam.Width,
                Depth = beam.Depth,
                MainBarDiameter = beam.MainBarDiameter,
                TopBars = beam.TopBars,
                BottomBars = beam.BottomBars,
                TieBarDiameter = beam.TieBarDiameter > 0 ? beam.TieBarDiameter : 8,
                TieSpacing = beam.TieSpacing,
                NumOfLegs = beam.NumOfLegs,
                ConcreteCover = beam.ConcreteCover,
                Length = Math.Round(beam.Length, 1) // Round to nearest 0.1m for grouping
            });
        }

        private static string GetGroupedSectionName(List<BeamRCData> beamGroup, int id)
        {
            // Try to get a meaningful name from the group
            var namesWithValues = beamGroup
                .Where(b => !string.IsNullOrEmpty(b.SectionName) || !string.IsNullOrEmpty(b.UniqueName))
                .ToList();

            if (namesWithValues.Any())
            {
                var firstValidName = namesWithValues.First();
                string baseName = firstValidName.SectionName ?? firstValidName.UniqueName;

                // If multiple beams have different names, show the count
                if (beamGroup.Count > 1)
                {
                    var uniqueNames = beamGroup
                        .Select(b => b.SectionName ?? b.UniqueName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .ToList();

                    if (uniqueNames.Count > 1)
                    {
                        return $"{baseName} (+{beamGroup.Count - 1} similar)";
                    }
                }

                return baseName;
            }

            return $"BEAM-{id:D3}";
        }

        // BeamKey class for grouping beams with similar properties
        private class BeamKey : IEquatable<BeamKey>
        {
            public double Width { get; set; }
            public double Depth { get; set; }
            public int MainBarDiameter { get; set; }
            public int TopBars { get; set; }
            public int BottomBars { get; set; }
            public int TieBarDiameter { get; set; }
            public double TieSpacing { get; set; }
            public int NumOfLegs { get; set; }
            public double ConcreteCover { get; set; }
            public double Length { get; set; }

            public bool Equals(BeamKey other)
            {
                if (other == null) return false;
                return Width == other.Width &&
                       Depth == other.Depth &&
                       MainBarDiameter == other.MainBarDiameter &&
                       TopBars == other.TopBars &&
                       BottomBars == other.BottomBars &&
                       TieBarDiameter == other.TieBarDiameter &&
                       Math.Abs(TieSpacing - other.TieSpacing) < 0.01 &&
                       NumOfLegs == other.NumOfLegs &&
                       Math.Abs(ConcreteCover - other.ConcreteCover) < 0.01 &&
                       Math.Abs(Length - other.Length) < 0.1; // 0.1m tolerance for length
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as BeamKey);
            }

            public override int GetHashCode()
            {
                return (Width, Depth, MainBarDiameter, TopBars, BottomBars,
                        TieBarDiameter, TieSpacing, NumOfLegs, ConcreteCover, Length).GetHashCode();
            }
        }
    }
}
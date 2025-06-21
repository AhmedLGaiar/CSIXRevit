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
        public static void ExportToPDF(IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams, string filePath, string selectedCode)
        {
            Document doc = new Document(PageSize.A4.Rotate(), 25f, 25f, 30f, 30f); // Landscape for more columns
            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            // Add header with logo placeholder and formatted date
            PdfPTable headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 1f, 3f });

            PdfPCell logoCell = new PdfPCell(new Phrase(" SLX ", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
            logoCell.Border = Rectangle.NO_BORDER;
            logoCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            headerTable.AddCell(logoCell);

            Font headerFont = new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD, BaseColor.BLACK);
            Font dateFont = new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, BaseColor.DARK_GRAY);
            PdfPCell headerCell = new PdfPCell();
            headerCell.AddElement(new Paragraph($"StructLink_X - Enhanced Rebar Report (SLX)", headerFont));
            headerCell.AddElement(new Paragraph($"Generated on: {DateTime.Now:dd MMMM yyyy, HH:mm}", dateFont));
            headerCell.Border = Rectangle.NO_BORDER;
            headerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            headerTable.AddCell(headerCell);

            doc.Add(headerTable);
            doc.Add(new Paragraph("\n"));

            // Styling for tables
            Font tableHeaderFont = new Font(Font.FontFamily.HELVETICA, 8, Font.BOLD, BaseColor.WHITE);
            Font tableDataFont = new Font(Font.FontFamily.HELVETICA, 7, Font.NORMAL, BaseColor.BLACK);
            BaseColor headerColor = new BaseColor(0, 122, 204); // Dark blue header
            BaseColor alternateRowColor = new BaseColor(240, 240, 240); // Light gray for alternate rows

            // Enhanced Columns Table
            if (columns != null && columns.Any())
            {
                doc.Add(new Paragraph("Columns", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
                doc.Add(new Paragraph("\n"));

                // Enhanced table with 10 columns
                PdfPTable columnTable = new PdfPTable(10);
                columnTable.WidthPercentage = 100;
                columnTable.SetWidths(new float[] { 0.8f, 1.3f, 1.2f, 0.8f, 1.3f, 1.2f, 1.2f, 1f, 1.2f, 1.2f });
                columnTable.DefaultCell.Padding = 3;

                string[] columnHeaders = {
                    "ID",
                    "Section",
                    "Size (mm)",
                    "Qty",
                    "Main Bars",
                    "Dir-2 Bars",
                    "Dir-3 Bars",
                    "Tie Dia.",
                    "Tie Spacing",
                    "Cover"
                };

                foreach (string header in columnHeaders)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                    cell.BackgroundColor = headerColor;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    columnTable.AddCell(cell);
                }

                int rowCount = 0;
                int idCounter = 1;

                foreach (var column in columns)
                {
                    // Validate inputs
                    if (column.MainBarDiameter <= 0) throw new ArgumentException("MainBarDiameter must be positive for column.");
                    if (column.TieSpacing <= 0) throw new ArgumentException("TieSpacing must be positive for column.");

                    int tieBarDiameter = column.TieBarDiameter > 0 ? column.TieBarDiameter : 8;
                    int totalMainBars = column.NumBarsDir3 + column.NumBarsDir2;

                    PdfPCell[] cells = new PdfPCell[]
                    {
                        new PdfPCell(new Phrase(idCounter.ToString(), tableDataFont)),
                        new PdfPCell(new Phrase(column.SectionName ?? column.UniqueName ?? $"COL-{idCounter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{column.Width}×{column.Depth}", tableDataFont)),
                        new PdfPCell(new Phrase(column.SectionCount.ToString(), tableDataFont)),
                        new PdfPCell(new Phrase($"{totalMainBars}T{column.MainBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{column.NumBarsDir2}T{column.MainBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{column.NumBarsDir3}T{column.MainBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"T{tieBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{column.TieSpacing:F0}", tableDataFont)),
                        new PdfPCell(new Phrase($"{column.ConcreteCover:F0}", tableDataFont))
                    };

                    foreach (var cell in cells)
                    {
                        cell.BackgroundColor = rowCount % 2 == 0 ? BaseColor.WHITE : alternateRowColor;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.Padding = 4;
                        columnTable.AddCell(cell);
                    }
                    rowCount++;
                    idCounter++;
                }

                doc.Add(columnTable);
                doc.Add(new Paragraph("\n"));
            }

            // Enhanced Beams Table
            if (beams != null && beams.Any())
            {
                doc.Add(new Paragraph("Beams", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
                doc.Add(new Paragraph("\n"));

                // Enhanced table with 9 columns
                PdfPTable beamTable = new PdfPTable(9);
                beamTable.WidthPercentage = 100;
                beamTable.SetWidths(new float[] { 0.8f, 1.3f, 1.2f, 1.3f, 1.3f, 1f, 1.2f, 1f, 1.2f });
                beamTable.DefaultCell.Padding = 3;

                string[] beamHeaders = {
                    "ID",
                    "Section",
                    "Size (mm)",
                    "Top Bars",
                    "Bottom Bars",
                    "Tie Dia.",
                    "Tie Spacing",
                    "Legs",
                    "Cover"
                };

                foreach (string header in beamHeaders)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                    cell.BackgroundColor = headerColor;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 6;
                    beamTable.AddCell(cell);
                }

                int rowCount = 0;
                int idCounter = 1;

                foreach (var beam in beams)
                {
                    // Validate inputs
                    if (beam.MainBarDiameter <= 0) throw new ArgumentException("MainBarDiameter must be positive for beam.");
                    if (beam.TieSpacing <= 0) throw new ArgumentException("TieSpacing must be positive for beam.");
                    if (beam.NumOfLegs <= 0) throw new ArgumentException("NumOfLegs must be positive for beam.");

                    int tieBarDiameter = beam.TieBarDiameter > 0 ? beam.TieBarDiameter : 8;

                    PdfPCell[] cells = new PdfPCell[]
                    {
                        new PdfPCell(new Phrase(idCounter.ToString(), tableDataFont)),
                        new PdfPCell(new Phrase(beam.SectionName ?? beam.UniqueName ?? $"BEAM-{idCounter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.Width}×{beam.Depth}", tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.TopBars}T{beam.MainBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.BottomBars}T{beam.MainBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"T{tieBarDiameter}", tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.TieSpacing:F0}", tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.NumOfLegs}", tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.ConcreteCover:F0}", tableDataFont))
                    };

                    foreach (var cell in cells)
                    {
                        cell.BackgroundColor = rowCount % 2 == 0 ? BaseColor.WHITE : alternateRowColor;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.Padding = 4;
                        beamTable.AddCell(cell);
                    }
                    rowCount++;
                    idCounter++;
                }

                doc.Add(beamTable);
                doc.Add(new Paragraph("\n"));
            }

            // Enhanced Summary Table
            doc.Add(new Paragraph("Project Summary", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
            doc.Add(new Paragraph("\n"));

            // Summary Statistics Table
            PdfPTable summaryTable = new PdfPTable(4);
            summaryTable.WidthPercentage = 80;
            summaryTable.SetWidths(new float[] { 2f, 1f, 2f, 1f });
            summaryTable.DefaultCell.Padding = 5;

            string[] summaryHeaders = { "Element Type", "Count", "Total Sections", "Avg Dimensions" };
            foreach (string header in summaryHeaders)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                cell.BackgroundColor = headerColor;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                cell.Padding = 8;
                summaryTable.AddCell(cell);
            }

            int columnCount = columns?.Count() ?? 0;
            int totalColumnSections = columns?.Sum(c => c.SectionCount) ?? 0;
            int beamCount = beams?.Count() ?? 0;

            // Calculate average dimensions
            string avgColumnDim = columns?.Any() == true ?
                $"{columns.Average(c => c.Width):F0}×{columns.Average(c => c.Depth):F0}" : "N/A";
            string avgBeamDim = beams?.Any() == true ?
                $"{beams.Average(b => b.Width):F0}×{beams.Average(b => b.Depth):F0}" : "N/A";

            PdfPCell[] summaryCells = new PdfPCell[]
            {
                new PdfPCell(new Phrase("Columns", tableDataFont)),
                new PdfPCell(new Phrase(columnCount.ToString(), tableDataFont)),
                new PdfPCell(new Phrase(totalColumnSections.ToString(), tableDataFont)),
                new PdfPCell(new Phrase(avgColumnDim, tableDataFont)),
                new PdfPCell(new Phrase("Beams", tableDataFont)),
                new PdfPCell(new Phrase(beamCount.ToString(), tableDataFont)),
                new PdfPCell(new Phrase(beamCount.ToString(), tableDataFont)),
                new PdfPCell(new Phrase(avgBeamDim, tableDataFont))
            };

            foreach (var cell in summaryCells)
            {
                cell.BackgroundColor = BaseColor.WHITE;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                cell.Padding = 6;
                summaryTable.AddCell(cell);
            }

            doc.Add(summaryTable);
            doc.Add(new Paragraph("\n"));

            // Rebar Summary Table
            if ((columns?.Any() == true) || (beams?.Any() == true))
            {
                doc.Add(new Paragraph("Rebar Summary", new Font(Font.FontFamily.HELVETICA, 11, Font.BOLD, BaseColor.BLACK)));
                doc.Add(new Paragraph("\n"));

                PdfPTable rebarTable = new PdfPTable(3);
                rebarTable.WidthPercentage = 60;
                rebarTable.SetWidths(new float[] { 2f, 1f, 1f });

                string[] rebarHeaders = { "Rebar Size", "Total Length (m)", "Total Weight (kg)" };
                foreach (string header in rebarHeaders)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                    cell.BackgroundColor = headerColor;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 8;
                    rebarTable.AddCell(cell);
                }

                // Calculate rebar totals by diameter
                var rebarSummary = new Dictionary<int, (double length, double weight)>();

                if (columns?.Any() == true)
                {
                    foreach (var column in columns)
                    {
                        // Main bars
                        double mainLength = RebarCalculator.CalculateColumnRebarLength(column);
                        double mainWeight = RebarCalculator.CalculateRebarWeight(mainLength * column.SectionCount, column.MainBarDiameter);

                        if (rebarSummary.ContainsKey(column.MainBarDiameter))
                        {
                            rebarSummary[column.MainBarDiameter] = (
                                rebarSummary[column.MainBarDiameter].length + mainLength * column.SectionCount,
                                rebarSummary[column.MainBarDiameter].weight + mainWeight
                            );
                        }
                        else
                        {
                            rebarSummary[column.MainBarDiameter] = (mainLength * column.SectionCount, mainWeight);
                        }

                        // Tie bars
                        int tieDiameter = column.TieBarDiameter > 0 ? column.TieBarDiameter : 8;
                        double tieLength = RebarCalculator.CalculateColumnTieLength(column);
                        double tieWeight = RebarCalculator.CalculateRebarWeight(tieLength * column.SectionCount, tieDiameter);

                        if (rebarSummary.ContainsKey(tieDiameter))
                        {
                            rebarSummary[tieDiameter] = (
                                rebarSummary[tieDiameter].length + tieLength * column.SectionCount,
                                rebarSummary[tieDiameter].weight + tieWeight
                            );
                        }
                        else
                        {
                            rebarSummary[tieDiameter] = (tieLength * column.SectionCount, tieWeight);
                        }
                    }
                }

                if (beams?.Any() == true)
                {
                    foreach (var beam in beams)
                    {
                        // Main bars
                        double mainLength = RebarCalculator.CalculateBeamRebarLength(beam);
                        double mainWeight = RebarCalculator.CalculateRebarWeight(mainLength, beam.MainBarDiameter);

                        if (rebarSummary.ContainsKey(beam.MainBarDiameter))
                        {
                            rebarSummary[beam.MainBarDiameter] = (
                                rebarSummary[beam.MainBarDiameter].length + mainLength,
                                rebarSummary[beam.MainBarDiameter].weight + mainWeight
                            );
                        }
                        else
                        {
                            rebarSummary[beam.MainBarDiameter] = (mainLength, mainWeight);
                        }

                        // Tie bars
                        int tieDiameter = beam.TieBarDiameter > 0 ? beam.TieBarDiameter : 8;
                        double tieCount = Math.Ceiling(beam.Length / (beam.TieSpacing / 1000));
                        double singleTieLength = RebarCalculator.CalculateBeamTieLength(beam);
                        double totalTieLength = singleTieLength * tieCount * beam.NumOfLegs;
                        double tieWeight = RebarCalculator.CalculateRebarWeight(totalTieLength, tieDiameter);

                        if (rebarSummary.ContainsKey(tieDiameter))
                        {
                            rebarSummary[tieDiameter] = (
                                rebarSummary[tieDiameter].length + totalTieLength,
                                rebarSummary[tieDiameter].weight + tieWeight
                            );
                        }
                        else
                        {
                            rebarSummary[tieDiameter] = (totalTieLength, tieWeight);
                        }
                    }
                }

                // Add rebar summary rows
                foreach (var kvp in rebarSummary.OrderBy(x => x.Key))
                {
                    PdfPCell[] rebarCells = new PdfPCell[]
                    {
                        new PdfPCell(new Phrase($"T{kvp.Key}", tableDataFont)),
                        new PdfPCell(new Phrase($"{kvp.Value.length:F2}", tableDataFont)),
                        new PdfPCell(new Phrase($"{kvp.Value.weight:F2}", tableDataFont))
                    };

                    foreach (var cell in rebarCells)
                    {
                        cell.BackgroundColor = BaseColor.WHITE;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.Padding = 6;
                        rebarTable.AddCell(cell);
                    }
                }

                doc.Add(rebarTable);
                doc.Add(new Paragraph("\n"));
            }

            // Footer
            Font footerFont = new Font(Font.FontFamily.HELVETICA, 9, Font.ITALIC, BaseColor.DARK_GRAY);
            doc.Add(new Paragraph("Report generated by StructLink_X v2.0", footerFont));
            doc.Add(new Paragraph("Thank you for using StructLink_X", footerFont));

            doc.Close();
        }
    }
}
using iTextSharp.text;
using iTextSharp.text.pdf;
using StructLink_X._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StructLink_X._0.Services
{
    public class PdfExporter
    {
        public static void ExportToPDF(IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams, string filePath, string selectedCode)
        {
            Document doc = new Document(PageSize.A4, 25f, 25f, 30f, 30f);
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
            headerCell.AddElement(new Paragraph($"StructLink_X - Rebar Report (SLX)", headerFont));
            headerCell.AddElement(new Paragraph($"Generated on: {DateTime.Now:dd MMMM yyyy, HH:mm}", dateFont));
            headerCell.Border = Rectangle.NO_BORDER;
            headerCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            headerTable.AddCell(headerCell);

            doc.Add(headerTable);
            doc.Add(new Paragraph("\n"));

            // Styling for tables
            Font tableHeaderFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.WHITE);
            Font tableDataFont = new Font(Font.FontFamily.HELVETICA, 9, Font.NORMAL, BaseColor.BLACK);
            BaseColor headerColor = new BaseColor(0, 122, 204); // Dark blue header
            BaseColor alternateRowColor = new BaseColor(240, 240, 240); // Light gray for alternate rows

            // Columns Table
            if (columns != null && columns.Any())
            {
                doc.Add(new Paragraph("Columns", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
                doc.Add(new Paragraph("\n"));

                PdfPTable columnTable = new PdfPTable(6); // 6 columns: ID, Element Type, Dimensions, Count, Main, Ties/m~
                columnTable.WidthPercentage = 100;
                columnTable.SetWidths(new float[] { 1f, 1.5f, 1.5f, 1f, 1.5f, 1.5f });
                columnTable.DefaultCell.Padding = 5;

                string[] columnHeaders = { "ID", "Element Type", "Dimensions (mm)", "Count", "Main", "Ties/m~" };
                foreach (string header in columnHeaders)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                    cell.BackgroundColor = headerColor;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 8;
                    columnTable.AddCell(cell);
                }

                int rowCount = 0;
                int idCounter = 1;

                foreach (var column in columns)
                {
                    // Validate inputs
                    if (column.MainBarDiameter <= 0) throw new ArgumentException("MainBarDiameter must be positive for column.");
                    if (column.TieSpacing <= 0) throw new ArgumentException("TieSpacing must be positive for column.");

                    //double tieCount = Math.Ceiling(column.Height / (column.TieSpacing / 1000));
                    int tieBarDiameter = column.TieBarDiameter > 0 ? column.TieBarDiameter : 8;
                    double rebarLength = RebarCalculator.CalculateColumnRebarLength(column);
                    double tieLength = RebarCalculator.CalculateColumnTieLength(column);
                    double rebarWeight = RebarCalculator.CalculateRebarWeight(rebarLength * column.SectionCount, column.MainBarDiameter);
                    double tieWeight = RebarCalculator.CalculateRebarWeight(tieLength * column.SectionCount, tieBarDiameter);
                    double totalWeight = rebarWeight + tieWeight;

                    // Calculate ties per meter
                    double tiesPerMeter = 1000 / column.TieSpacing;

                    PdfPCell[] cells = new PdfPCell[]
                    {
                        new PdfPCell(new Phrase(idCounter.ToString(), tableDataFont)),
                        new PdfPCell(new Phrase(column.ElementType, tableDataFont)),
                        new PdfPCell(new Phrase($"{column.Width}x{column.Depth}", tableDataFont)),
                        new PdfPCell(new Phrase(column.SectionCount.ToString(), tableDataFont)),
                        new PdfPCell(new Phrase($"{(column.NumBarsDir3 + column.NumBarsDir2)}T{column.MainBarDiameter}", tableDataFont)), // Main rebar
                        new PdfPCell(new Phrase($"{tiesPerMeter:F0}T{tieBarDiameter}", tableDataFont)), // Ties/m~
                    };

                    foreach (var cell in cells)
                    {
                        cell.BackgroundColor = rowCount % 2 == 0 ? BaseColor.WHITE : alternateRowColor;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.Padding = 6;
                        columnTable.AddCell(cell);
                    }
                    rowCount++;
                    idCounter++;
                }

                doc.Add(columnTable);
                doc.Add(new Paragraph("\n"));
            }

            // Beams Table
            if (beams != null && beams.Any())
            {
                doc.Add(new Paragraph("Beams", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
                doc.Add(new Paragraph("\n"));

                PdfPTable beamTable = new PdfPTable(6); // 6 columns: ID, Element Type, Dimensions, Main, Ties/m~, Num of Legs
                beamTable.WidthPercentage = 100;
                beamTable.SetWidths(new float[] { 1f, 1.5f, 1.5f, 1.5f, 1.5f, 1f });
                beamTable.DefaultCell.Padding = 5;

                string[] beamHeaders = { "ID", "Element Type", "Dimensions (mm)", "Main", "Ties/m~", "Num of Legs" };
                foreach (string header in beamHeaders)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                    cell.BackgroundColor = headerColor;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                    cell.Padding = 8;
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

                    double tieCount = Math.Ceiling(beam.Length / (beam.TieSpacing / 1000));
                    int tieBarDiameter = beam.TieBarDiameter > 0 ? beam.TieBarDiameter : 8;
                    double rebarLength = RebarCalculator.CalculateBeamRebarLength(beam);
                    double singleTieLength = RebarCalculator.CalculateBeamTieLength(beam); // Length of one tie
                    double tieLength = singleTieLength * tieCount * beam.NumOfLegs; // Total tie length with NumOfLegs
                    double rebarWeight = RebarCalculator.CalculateRebarWeight(rebarLength, beam.MainBarDiameter);
                    double tieWeight = RebarCalculator.CalculateRebarWeight(tieLength, tieBarDiameter);
                    double totalWeight = rebarWeight + tieWeight;

                    // Calculate ties per meter
                    double tiesPerMeter = 1000 / beam.TieSpacing;

                    PdfPCell[] cells = new PdfPCell[]
                    {
                        new PdfPCell(new Phrase(idCounter.ToString(), tableDataFont)),
                        new PdfPCell(new Phrase(beam.ElementType, tableDataFont)),
                        new PdfPCell(new Phrase($"{beam.Width}x{beam.Depth}", tableDataFont)),
                        new PdfPCell(new Phrase($"{(beam.BottomBars + beam.TopBars)}T{beam.MainBarDiameter}", tableDataFont)), // Main rebar
                        new PdfPCell(new Phrase($"{tiesPerMeter:F0}T{tieBarDiameter}", tableDataFont)), // Ties/m~
                        new PdfPCell(new Phrase($"{beam.NumOfLegs}", tableDataFont)), // Num of Legs
                    };

                    foreach (var cell in cells)
                    {
                        cell.BackgroundColor = rowCount % 2 == 0 ? BaseColor.WHITE : alternateRowColor;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.Padding = 6;
                        beamTable.AddCell(cell);
                    }
                    rowCount++;
                    idCounter++;
                }

                doc.Add(beamTable);
                doc.Add(new Paragraph("\n"));
            }

            // Summary Count Table
            doc.Add(new Paragraph("Summary", new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK)));
            doc.Add(new Paragraph("\n"));

            PdfPTable summaryTable = new PdfPTable(2);
            summaryTable.WidthPercentage = 50;
            summaryTable.SetWidths(new float[] { 2f, 1f });
            summaryTable.DefaultCell.Padding = 5;

            string[] summaryHeaders = { "Element", "Count" };
            foreach (string header in summaryHeaders)
            {
                PdfPCell cell = new PdfPCell(new Phrase(header, tableHeaderFont));
                cell.BackgroundColor = headerColor;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                cell.Padding = 8;
                summaryTable.AddCell(cell);
            }

            int columnCount = columns?.Sum(c => c.SectionCount) ?? 0;
            int beamCount = beams?.Count() ?? 0;

            PdfPCell[] summaryCells = new PdfPCell[]
            {
                new PdfPCell(new Phrase("Columns", tableDataFont)),
                new PdfPCell(new Phrase(columnCount.ToString(), tableDataFont)),
                new PdfPCell(new Phrase("Beams", tableDataFont)),
                new PdfPCell(new Phrase(beamCount.ToString(), tableDataFont))
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
            Font thankYouFont = new Font(Font.FontFamily.HELVETICA, 10, Font.ITALIC, BaseColor.DARK_GRAY);
            doc.Add(new Paragraph("Thank you for using StructLink_X", thankYouFont));

            doc.Close();
        }
    }
}
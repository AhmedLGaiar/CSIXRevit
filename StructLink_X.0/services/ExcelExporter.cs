using OfficeOpenXml;
using StructLink_X._0.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace StructLink_X._0.Services
{
    public class ExcelExporter
    {
        private List<double> calculatedValues; // لتخزين القيم المحسوبة (مثل الأوزان أو الأطوال)

        public ExcelExporter()
        {
            calculatedValues = new List<double>();
            // Set EPPlus license context explicitly for EPPlus 5+
            EnsureLicenseContext();
        }

        /// <summary>
        /// Ensures the EPPlus license context is set before any operations.
        /// </summary>
        private void EnsureLicenseContext()
        {
            if (ExcelPackage.LicenseContext == LicenseContext.NonCommercial)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            }
        }

        /// <summary>
        /// Adds a calculated value to the list and handles potential issues.
        /// </summary>
        /// <param name="value">The calculated value to add</param>
        /// <returns>Formatted string with the added value and list status</returns>
        public string AddCalculatedValue(double value)
        {
            try
            {
                // التحقق من القيم غير الصالحة (مثل القيم اللانهائية أو NaN)
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return $"Error: Value {value} is invalid (NaN or Infinity).";
                }

                calculatedValues.Add(value);
                return $"Value {value:F2} added successfully. Total values: {calculatedValues.Count}. Values: [{string.Join(", ", calculatedValues)}]";
            }
            catch (Exception ex)
            {
                return $"Error occurred: {ex.Message}. Value not added.";
            }
        }

        public void ExportToExcel(IEnumerable<ColumnRCData> columns, IEnumerable<BeamRCData> beams, string filePath)
        {
            // Ensure LicenseContext is set before using ExcelPackage
            EnsureLicenseContext();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets.Add("Rebar Report");

                    // وضع العناوين
                    ws.Cells[1, 1].Value = "Element";
                    ws.Cells[1, 2].Value = "Dimensions";
                    ws.Cells[1, 3].Value = "Concrete Volume (m³)";
                    ws.Cells[1, 4].Value = "Main Reinforcement";
                    ws.Cells[1, 5].Value = "Ties (m)";
                    ws.Cells[1, 6].Value = "Weight (kg)";
                    ws.Cells[1, 7].Value = "Code Compliance";
                    ws.Cells[1, 8].Value = "Cutting Plan";

                    int row = 2;

                    // تصدير بيانات الأعمدة
                    foreach (var column in columns)
                    {
                        ws.Cells[row, 1].Value = column.SectionName ?? "N/A";
                        //ws.Cells[row, 2].Value = $"{column.Width}x{column.Depth}x{column.Height * 1000}";
                        double concreteVolume = RebarCalculator.CalculateColumnConcreteVolume(column);
                        ws.Cells[row, 3].Value = concreteVolume.ToString("F2");
                        ws.Cells[row, 4].Value = $"{column.NumBarsDir3 + column.NumBarsDir2}T{column.MainBarDiameter}";
                        double tieLength = RebarCalculator.CalculateColumnTieLength(column);
                        ws.Cells[row, 5].Value = tieLength.ToString("F2");
                        // Fix for CS1501: Adjust the method call to match the signature of CalculateColumnRebarLength
                        double rebarLength = RebarCalculator.CalculateColumnRebarLength(column); // Removed the second argument
                        double totalLength = rebarLength + tieLength;
                        double weight = RebarCalculator.CalculateRebarWeight(totalLength, column.MainBarDiameter);
                        ws.Cells[row, 6].Value = weight.ToString("F2");
                        Console.WriteLine(AddCalculatedValue(weight));
                        ws.Cells[row, 8].Value = CuttingOptimizer.CalculateCuttingPlan(rebarLength);
                        row++;
                    }

                    // تصدير بيانات الكمرات
                    foreach (var beam in beams)
                    {
                        ws.Cells[row, 1].Value = beam.UniqueName ?? "N/A";
                        ws.Cells[row, 2].Value = $"{beam.Width}x{beam.Depth}x{beam.Length * 1000}";
                        double concreteVolume = RebarCalculator.CalculateBeamConcreteVolume(beam);
                        ws.Cells[row, 3].Value = concreteVolume.ToString("F2");
                        ws.Cells[row, 4].Value = $"{beam.BottomBars + beam.TopBars}T{beam.MainBarDiameter}";
                        double tieLength = RebarCalculator.CalculateBeamTieLength(beam);
                        ws.Cells[row, 5].Value = tieLength.ToString("F2");
                        // Fix for CS1501: Adjust the method call to match the signature of CalculateBeamRebarLength
                        double rebarLength = RebarCalculator.CalculateBeamRebarLength(beam); // Removed the second argument
                        double totalLength = rebarLength + tieLength;
                        double weight = RebarCalculator.CalculateRebarWeight(totalLength, beam.MainBarDiameter);
                        ws.Cells[row, 6].Value = weight.ToString("F2");
                        Console.WriteLine(AddCalculatedValue(weight));
                        ws.Cells[row, 8].Value = CuttingOptimizer.CalculateCuttingPlan(rebarLength);
                        row++;
                    }

                   
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    package.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export to Excel: {ex.Message}");
            }
        }
    }
}
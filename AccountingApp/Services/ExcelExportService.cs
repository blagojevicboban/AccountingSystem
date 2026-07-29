using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AccountingApp.Services;

public static class ExcelExportService
{
    /// <summary>
    /// Izvozi sadržaj iz zadatog WPF DataGrid-a u Excel (.xlsx) fajl i automatski ga otvara.
    /// Formatira naslove, brojeve, ivice i auto-fituje kolone.
    /// </summary>
    public static void ExportDataGridToExcel(DataGrid dataGrid, string title, string defaultFileName)
    {
        if (dataGrid == null || dataGrid.ItemsSource == null)
        {
            System.Windows.MessageBox.Show("Nema podataka za izvoz u Excel.", "Obaveštenje", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var items = dataGrid.ItemsSource.Cast<object>().ToList();
        if (items.Count == 0)
        {
            System.Windows.MessageBox.Show("Tabela ne sadrži podatke za izvoz.", "Obaveštenje", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var columns = dataGrid.Columns
            .OfType<DataGridTextColumn>()
            .Where(c => c.Visibility == System.Windows.Visibility.Visible)
            .ToList();

        if (columns.Count == 0)
        {
            System.Windows.MessageBox.Show("Nema vidljivih kolona za izvoz.", "Obaveštenje", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Izveštaj");

        // 1. Naslov izveštaja
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        worksheet.Cell(2, 1).Value = $"Datum izvoza: {DateTime.Now:dd.MM.yyyy. HH:mm}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 9;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

        int startRow = 4;

        // 2. Zaglavlje tabele
        for (int c = 0; c < columns.Count; c++)
        {
            var cell = worksheet.Cell(startRow, c + 1);
            cell.Value = columns[c].Header?.ToString() ?? $"Kolona {c + 1}";
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        // 3. Popunjavanje redova
        int currentRow = startRow + 1;
        foreach (var item in items)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var col = columns[c];
                var cell = worksheet.Cell(currentRow, c + 1);

                object? val = GetCellValue(item, col);
                if (val == null)
                {
                    cell.Value = "";
                }
                else if (val is decimal decVal)
                {
                    cell.Value = decVal;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (val is double dblVal)
                {
                    cell.Value = dblVal;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (val is int intVal)
                {
                    cell.Value = intVal;
                    cell.Style.NumberFormat.Format = "#,##0";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (val is DateTime dtVal)
                {
                    cell.Value = dtVal;
                    cell.Style.NumberFormat.Format = "dd.MM.yyyy";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (val is bool boolVal)
                {
                    cell.Value = boolVal ? "Da" : "Ne";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else
                {
                    string strVal = val.ToString() ?? "";
                    if (decimal.TryParse(strVal, out decimal parsedDec) && (strVal.Contains(",") || strVal.Contains(".")))
                    {
                        cell.Value = parsedDec;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else
                    {
                        cell.Value = strVal;
                    }
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
            }
            currentRow++;
        }

        // 4. Zbirni red (Total) ako postoje numeričke kolone
        var totalRow = worksheet.Cell(currentRow, 1);
        for (int c = 0; c < columns.Count; c++)
        {
            var headerName = columns[c].Header?.ToString() ?? "";
            bool isNumericHeader = headerName.Contains("iznos", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("duguje", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("potražuje", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("potrazuje", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("saldo", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("količina", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("kolicina", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("ulaz", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("izlaz", StringComparison.OrdinalIgnoreCase) ||
                                  headerName.Contains("vrednost", StringComparison.OrdinalIgnoreCase);

            var cell = worksheet.Cell(currentRow, c + 1);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            cell.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Double;

            if (c == 0)
            {
                cell.Value = "TOTAL:";
            }
            else if (isNumericHeader)
            {
                string colLetter = XLHelper.GetColumnLetterFromNumber(c + 1);
                cell.FormulaA1 = $"SUM({colLetter}{startRow + 1}:{colLetter}{currentRow - 1})";
                cell.Style.NumberFormat.Format = "#,##0.00";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
        }

        worksheet.Columns().AdjustToContents();

        // 5. Snimanje i otvaranje fajla
        SaveAndOpenFile(workbook, defaultFileName);
    }

    private static object? GetCellValue(object item, DataGridTextColumn col)
    {
        if (col.Binding is Binding binding && !string.IsNullOrEmpty(binding.Path?.Path))
        {
            string propName = binding.Path.Path;
            return GetPropertyValue(item, propName);
        }
        return null;
    }

    private static object? GetPropertyValue(object obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath)) return null;

        object? currentObj = obj;
        foreach (var part in propertyPath.Split('.'))
        {
            if (currentObj == null) return null;
            var prop = currentObj.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) return null;
            currentObj = prop.GetValue(currentObj);
        }
        return currentObj;
    }

    private static void SaveAndOpenFile(XLWorkbook workbook, string defaultFileName)
    {
        try
        {
            string safeName = string.Join("_", defaultFileName.Split(Path.GetInvalidFileNameChars()));
            if (!safeName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) safeName += ".xlsx";

            string tempPath = Path.Combine(Path.GetTempPath(), $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            workbook.SaveAs(tempPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Greška pri kreiranju/otvaranju Excel fajla:\n{ex.Message}", "Greška", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}

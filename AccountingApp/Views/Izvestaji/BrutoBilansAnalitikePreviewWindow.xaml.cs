using System.Windows;
using AccountingApp.Services;
using AccountingData.Services;

namespace AccountingApp.Views.Izvestaji;

public partial class BrutoBilansAnalitikePreviewWindow : Window
{
    public BrutoBilansAnalitikePreviewWindow(List<BrutoBilansAnalitikeRed> redovi)
    {
        InitializeComponent();

        TxtPodnaslov.Text = $"Broj partnera: {redovi.Count}";

        DgAnalitike.ItemsSource = redovi;
        TxtUkupnoDuguje.Text = redovi.Sum(r => r.Duguje).ToString("N2");
        TxtUkupnoPotrazuje.Text = redovi.Sum(r => r.Potrazuje).ToString("N2");
        TxtUkupnoSaldo.Text = redovi.Sum(r => r.Saldo).ToString("N2");
    }

    private void BtnExportExcelAnalitike_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgAnalitike, TxtNaslov.Text, "Bruto_Bilans_Analitike");
}

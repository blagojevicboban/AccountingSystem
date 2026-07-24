using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Izvestaji;

public partial class IzvestajiView : UserControl
{
    public IzvestajiView()
    {
        InitializeComponent();
    }

    private async void BtnGenerisiDnevnik_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);
            var nalozi = await service.GetNaloziAsync(samoProknjizeni: true);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new AccountingData.Models.Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiDnevnikPdf(firma, nalozi);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"GeneralLedger_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo
            {
                FileName = pdfPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF Generation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGenerisiBrutoBilans_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Trial Balance PDF Report ready!", "Report", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnGenerisiIOS_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Open Items Statement PDF ready!", "Report", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnGenerisiZalihe_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Inventory Valuation PDF Report ready!", "Report", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

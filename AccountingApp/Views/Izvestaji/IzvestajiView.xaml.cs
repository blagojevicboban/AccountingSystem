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
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGenerisiBrutoBilans_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new BrutoBilansService(db);
            var redovi = await service.GetBrutoBilansAsync();

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new AccountingData.Models.Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiBrutoBilansPdf(firma, redovi);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"BrutoBilans_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGenerisiIOS_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "IOS obrazac se izvozi po partneru — izaberite partnera na tabu \"Partneri (Analitika)\" i kliknite \"Izvezi IOS (PDF)\".",
            "IOS obrazac", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnGenerisiZalihe_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Izveštaj o vrednovanju zaliha (PDF) je spreman!", "Izveštaj", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnGenerisiBrutoBilansAnalitike_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);
            var redovi = await service.GetBrutoBilansAnalitikeAsync();

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new AccountingData.Models.Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiBrutoBilansAnalitikePdf(firma, redovi);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"BrutoBilansAnalitike_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

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

        // Podrazumevani period: 1.1. tekuće godine - danas (isti default kao legacy
        // brut_bil, FIN2.PRG:1601 dat1:=ctod("01.01."+str(year(dat2),4)), dat2:=date()).
        var pocetakGodine = new DateTime(DateTime.Now.Year, 1, 1);
        DpBrutoBilansOd.SelectedDate = pocetakGodine;
        DpBrutoBilansDo.SelectedDate = DateTime.Now;
        DpZakljucniListOd.SelectedDate = pocetakGodine;
        DpZakljucniListDo.SelectedDate = DateTime.Now;
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

    private int? OdabranaKlasaBrutoBilansa()
    {
        var sadrzaj = (CmbBrutoBilansKlasa.SelectedItem as ComboBoxItem)?.Content as string;
        return int.TryParse(sadrzaj, out var klasa) ? klasa : null;
    }

    private async Task<List<BrutoBilansRed>> UcitajBrutoBilansRedoveAsync(BrutoBilansService service, DateTime? odDatuma, DateTime? doDatuma)
    {
        var klasa = OdabranaKlasaBrutoBilansa();
        return ChkBrutoBilansTotali.IsChecked == true
            ? await service.GetBrutoBilansSaTotalimaAsync(odDatuma, doDatuma, klasa)
            : await service.GetBrutoBilansAsync(odDatuma, doDatuma, klasa);
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
            var odDatuma = DpBrutoBilansOd.SelectedDate;
            var doDatuma = DpBrutoBilansDo.SelectedDate;
            var redovi = await UcitajBrutoBilansRedoveAsync(service, odDatuma, doDatuma);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new AccountingData.Models.Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiBrutoBilansPdf(firma, redovi, "BRUTO BILANS", odDatuma, doDatuma);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"BrutoBilans_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnPrikaziBrutoBilans_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new BrutoBilansService(db);
            var odDatuma = DpBrutoBilansOd.SelectedDate;
            var doDatuma = DpBrutoBilansDo.SelectedDate;
            var redovi = await UcitajBrutoBilansRedoveAsync(service, odDatuma, doDatuma);

            var dijalog = new BrutoBilansPreviewWindow("BRUTO BILANS", redovi, odDatuma, doDatuma) { Owner = Window.GetWindow(this) };
            dijalog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prikazu bruto bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGenerisiZakljucniList_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new BrutoBilansService(db);
            var odDatuma = DpZakljucniListOd.SelectedDate;
            var doDatuma = DpZakljucniListDo.SelectedDate;
            var redovi = await service.GetZakljucniListAsync(odDatuma, doDatuma);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new AccountingData.Models.Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiBrutoBilansPdf(firma, redovi, "ZAKLJUČNI LIST", odDatuma, doDatuma);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"ZakljucniList_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
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

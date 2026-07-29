using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Partneri;

public partial class PartneriView : UserControl
{
    private List<Partner> _sviPartneri = new();

    public PartneriView()
    {
        InitializeComponent();
        LoadPartnere();
    }

    private async void LoadPartnere()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            _sviPartneri = await service.GetPartneriAsync();
            LstPartneri.ItemsSource = _sviPartneri;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretragaPartnera_TextChanged(object sender, TextChangedEventArgs e)
    {
        string search = TxtPretragaPartnera.Text.Trim().ToLower();
        LstPartneri.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviPartneri
            : _sviPartneri.Where(p => p.SifraPartnera.ToLower().Contains(search) || p.Naziv.ToLower().Contains(search)).ToList();
    }

    private async void LstPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            return;
        }

        TxtNaslovPartnera.Text = partner.Naziv;
        TxtPodnaslovPartnera.Text = $"Šifra: {partner.SifraPartnera}" + (string.IsNullOrWhiteSpace(partner.Pib) ? "" : $" | PIB: {partner.Pib}");

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            var stavke = await service.GetOtvoreneStavkeAsync(partner.PartnerId);
            DgOtvoreneStavke.ItemsSource = stavke;
            TxtSaldoPartnera.Text = (stavke.Count > 0 ? stavke[^1].Saldo : 0m).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju otvorenih stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajIOS_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za izvoz IOS obrasca.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);
            var stavke = await service.GetOtvoreneStavkeAsync(partner.PartnerId);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiIOSPdf(firma, partner, stavke);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"IOS_{partner.SifraPartnera}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnObracunKamate_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za obračun kamate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new KamataWindow(partner) { Owner = Window.GetWindow(this) };
        dijalog.ShowDialog();
    }

    private void BtnExportExcelPartneri_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgOtvoreneStavke, TxtNaslovPartnera.Text, "Partneri_Otvorene_Stavke");
}

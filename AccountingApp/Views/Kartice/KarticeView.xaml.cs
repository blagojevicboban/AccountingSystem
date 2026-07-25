using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Kartice;

public partial class KarticeView : UserControl
{
    private List<Konto> _svaKonta = new();

    public KarticeView()
    {
        InitializeComponent();
        LoadKonta();
    }

    private async void LoadKonta()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);

            bool samoSaPrometom = ChkSamoSaPrometom?.IsChecked ?? true;
            _svaKonta = await service.GetKontaAsync(samoSaPrometom);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        LoadKonta();
    }

    private void ApplyFilter()
    {
        if (LstKonta == null) return;

        string search = TxtPretragaKonta?.Text.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrEmpty(search)
            ? _svaKonta
            : _svaKonta.Where(k => k.BrojKonta.ToLower().Contains(search) || k.NazivKonta.ToLower().Contains(search)).ToList();

        LstKonta.ItemsSource = filtered;
        if (filtered.Any())
        {
            LstKonta.SelectedIndex = 0;
        }
        else
        {
            DgKartica.ItemsSource = null;
            TxtNaslovKonta.Text = "Nema konta za prikaz";
            TxtPodnaslovKonta.Text = "";
            TxtSaldoKonta.Text = "0,00";
        }
    }

    private void TxtPretragaKonta_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private async void LstKonta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstKonta.SelectedItem is not Konto konto)
        {
            return;
        }

        TxtNaslovKonta.Text = $"{konto.BrojKonta} — {konto.NazivKonta}";
        TxtPodnaslovKonta.Text = konto.IsSintetika ? "Sintetički konto" : "Analitički konto";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);

            var kartica = await service.GetKarticaKontaAsync(konto.BrojKonta);
            DgKartica.ItemsSource = kartica;
            TxtSaldoKonta.Text = (kartica.Count > 0 ? kartica[^1].Saldo : 0m).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampaj_Click(object sender, RoutedEventArgs e)
    {
        if (LstKonta.SelectedItem is not Konto konto)
        {
            MessageBox.Show("Izaberite konto za štampu kartice.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);
            var kartica = await service.GetKarticaKontaAsync(konto.BrojKonta);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiKarticuPdf(firma, konto, kartica);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"KarticaKonta_{konto.BrojKonta}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

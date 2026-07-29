using System.ComponentModel;
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

public class KontoIzbor : INotifyPropertyChanged
{
    public Konto Konto { get; }
    public KontoIzbor(Konto konto) => Konto = konto;

    public string BrojKonta => Konto.BrojKonta;
    public string NazivKonta => Konto.NazivKonta;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class KarticeView : UserControl
{
    private List<Konto> _svaKonta = new();

    public KarticeView()
    {
        InitializeComponent();
        DpKarticaOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpKarticaDo.SelectedDate = DateTime.Today;
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

        var izbori = filtered.Select(k => new KontoIzbor(k)).ToList();
        foreach (var izbor in izbori) izbor.PropertyChanged += KontoIzbor_PropertyChanged;
        LstKonta.ItemsSource = izbori;
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
            BtnStampaj.IsEnabled = false;
        }

        UpdateBtnStampajIzabraneState();
    }

    private void KontoIzbor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KontoIzbor.IsSelected))
        {
            UpdateBtnStampajIzabraneState();
        }
    }

    private void UpdateBtnStampajIzabraneState()
    {
        var izbori = LstKonta.ItemsSource as List<KontoIzbor>;
        BtnStampajIzabrane.IsEnabled = izbori?.Any(k => k.IsSelected) ?? false;
    }

    private void TxtPretragaKonta_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private async void LstKonta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstKonta.SelectedItem is not KontoIzbor izbor)
        {
            return;
        }

        var konto = izbor.Konto;
        TxtNaslovKonta.Text = $"{konto.BrojKonta} — {konto.NazivKonta}";
        TxtPodnaslovKonta.Text = konto.IsSintetika ? "Sintetički konto" : "Analitički konto";

        await UcitajKarticu();
    }

    private async void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        await UcitajKarticu();
    }

    private async Task UcitajKarticu()
    {
        if (LstKonta.SelectedItem is not KontoIzbor izbor)
        {
            return;
        }

        var konto = izbor.Konto;
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);

            var kartica = await service.GetKarticaKontaAsync(konto.BrojKonta, DpKarticaOd.SelectedDate, DpKarticaDo.SelectedDate);
            DgKartica.ItemsSource = kartica;
            TxtSaldoKonta.Text = (kartica.Count > 0 ? kartica[^1].Saldo : 0m).ToString("N2");
            BtnStampaj.IsEnabled = kartica.Count > 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampaj_Click(object sender, RoutedEventArgs e)
    {
        if (LstKonta.SelectedItem is not KontoIzbor izbor)
        {
            MessageBox.Show("Izaberite konto za štampu kartice.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var konto = izbor.Konto;
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);
            var odDatuma = DpKarticaOd.SelectedDate;
            var doDatuma = DpKarticaDo.SelectedDate;
            var kartica = await service.GetKarticaKontaAsync(konto.BrojKonta, odDatuma, doDatuma);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiKarticuPdf(firma, konto, kartica, odDatuma, doDatuma);

            string sigurnaSifra = string.Join("_", konto.BrojKonta.Split(Path.GetInvalidFileNameChars()));
            string pdfPath = Path.Combine(Path.GetTempPath(), $"KarticaKonta_{sigurnaSifra}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajIzabrane_Click(object sender, RoutedEventArgs e)
    {
        var izabrani = (LstKonta.ItemsSource as List<KontoIzbor> ?? new()).Where(k => k.IsSelected).ToList();
        if (!izabrani.Any())
        {
            MessageBox.Show("Čekirajte bar jedan konto za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);
            var odDatuma = DpKarticaOd.SelectedDate;
            var doDatuma = DpKarticaDo.SelectedDate;
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            var kartice = new List<(Konto Konto, List<KarticaRed> Stavke)>();
            foreach (var izbor in izabrani)
            {
                var stavke = await service.GetKarticaKontaAsync(izbor.Konto.BrojKonta, odDatuma, doDatuma);
                kartice.Add((izbor.Konto, stavke));
            }

            byte[] pdfBytes = PdfReportService.GenerisiViseKarticaPdf(firma, kartice, odDatuma, doDatuma);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"KarticeKonta_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelKartica_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgKartica, TxtNaslovKonta.Text, "Kartica_Konta");
}

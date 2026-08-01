using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AccountingApp.Services;
using AccountingApp.Views.Pomoc;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Partneri;

public partial class KamataWindow : Window
{
    private readonly Partner _partner;
    private List<KamataStavka> _poslednjiObracun = new();

    public KamataWindow(Partner partner)
    {
        InitializeComponent();
        _partner = partner;
        TxtNaslovPartnera.Text = $"💰 Obračun kamate — {partner.Naziv}";
        DpDatumObracuna.SelectedDate = DateTime.Now;
        DpNovaStopaOd.SelectedDate = DateTime.Now;

        LoadStope();
    }

    private async void LoadStope()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new KamataService(db);

            DgStope.ItemsSource = await service.GetStopeAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kamatnih stopa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnDodajStopu_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtNovaStopaProcenat.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var stopa)
            && !decimal.TryParse(TxtNovaStopaProcenat.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out stopa))
        {
            MessageBox.Show("Unesite ispravnu vrednost stope (npr. 8,5).", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DpNovaStopaOd.SelectedDate is not DateTime datumOd)
        {
            MessageBox.Show("Izaberite datum od kada stopa važi.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new KamataService(db);

            await service.DodajStopuAsync(datumOd, stopa, "Ručno uneta stopa");
            TxtNovaStopaProcenat.Text = string.Empty;
            LoadStope();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri dodavanju stope: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnObracunaj_Click(object sender, RoutedEventArgs e)
    {
        if (DpDatumObracuna.SelectedDate is not DateTime datumObracuna)
        {
            MessageBox.Show("Izaberite datum obračuna.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new KamataService(db);

            _poslednjiObracun = await service.ObracunajKamatuAsync(_partner.PartnerId, datumObracuna);
            DgKamata.ItemsSource = _poslednjiObracun;
            TxtUkupnaKamata.Text = _poslednjiObracun.Sum(k => k.ObracunataKamata).ToString("N2");

            if (_poslednjiObracun.Count == 0)
            {
                MessageBox.Show("Nema dugovnih otvorenih stavki sa kašnjenjem na zadati datum obračuna.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri obračunu kamate: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnStampajKamatu_Click(object sender, RoutedEventArgs e)
    {
        if (_poslednjiObracun.Count == 0)
        {
            MessageBox.Show("Prvo izvršite obračun kamate (dugme \"Obračunaj\").", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiKamataPdf(firma, _partner, _poslednjiObracun, DpDatumObracuna.SelectedDate ?? DateTime.Now);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"Kamata_{_partner.SifraPartnera}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "💰 Pomoć — Obračun kamate",
            "Obračun zatezne kamate na neplaćene otvorene stavke partnera.",
            new (string, string)[]
            {
                ("➕ Dodaj stopu", "Dodaje novu kamatnu stopu koja važi od unetog datuma."),
                ("🧮 Obračunaj", "Izračunava kamatu za sve dugovane stavke partnera do datuma obračuna."),
                ("🖨️", "Izvozi obračun u PDF."),
            },
            "Kamata se obračunava po danima kašnjenja svake pojedinačne stavke, primenjujući stopu koja je važila na dan kašnjenja (ako je bilo više izmena stope u periodu)."
        ) { Owner = this }.ShowDialog();
    }
}

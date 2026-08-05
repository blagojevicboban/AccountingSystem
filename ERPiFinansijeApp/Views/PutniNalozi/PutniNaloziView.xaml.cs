using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.PutniNalozi;

public partial class PutniNaloziView : UserControl
{
    private List<PutniNalog> _sviNalozi = new();

    public PutniNaloziView()
    {
        InitializeComponent();
        Loaded += PutniNaloziView_Loaded;
    }

    private void PutniNaloziView_Loaded(object sender, RoutedEventArgs e)
    {
        LoadPutniNalozi();
    }

    private async void LoadPutniNalozi()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new PutniNalogService(db);
            _sviNalozi = await service.GetPutniNaloziAsync();
            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju putnih naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filtriraj()
    {
        if (DgPutniNalozi == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";

        var filtered = _sviNalozi.Where(p =>
            string.IsNullOrEmpty(search) ||
            p.BrojNaloga.ToLower().Contains(search) ||
            p.ZaposleniIme.ToLower().Contains(search) ||
            p.Relacija.ToLower().Contains(search)
        ).ToList();

        DgPutniNalozi.ItemsSource = filtered;
        if (filtered.Any()) DgPutniNalozi.SelectedIndex = 0;
    }

    private void DgPutniNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            DgStavkeTroskova.ItemsSource = pn.StavkeTroskova;
        }
        else
        {
            DgStavkeTroskova.ItemsSource = null;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private async void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite($"Data Source={AppConfig.DbPath}")
            .Options;
        using var db = new AccountingDbContext(options);
        var win = new PutniNalogEditWindow(new PutniNalog(), db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadPutniNalozi();
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            if (pn.IsKnjizeno)
            {
                MessageBox.Show("Proknjiženi putni nalozi se ne mogu menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var full = await new PutniNalogService(db).GetPutniNalogByIdAsync(pn.PutniNalogId);
            if (full != null)
            {
                var win = new PutniNalogEditWindow(full, db) { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true) LoadPutniNalozi();
            }
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            if (pn.IsKnjizeno)
            {
                MessageBox.Show("Proknjiženi putni nalozi se ne mogu brisati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Da li ste sigurni da želite obrisati putni nalog br. {pn.BrojNaloga}?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;
                using var db = new AccountingDbContext(options);
                await new PutniNalogService(db).ObrisiPutniNalogAsync(pn.PutniNalogId);
                LoadPutniNalozi();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            if (pn.IsKnjizeno)
            {
                MessageBox.Show("Putni nalog je već proknjižen.", "Obaveštenje", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string kontoStr = pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? "5340" : "5330";
            var res = MessageBox.Show($"Da li želite da proknjižite putni nalog br. {pn.BrojNaloga} na Konto {kontoStr} u Glavnoj knjizi?", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;
                using var db = new AccountingDbContext(options);
                var (success, msg, nalogId) = await new PutniNalogService(db).KnjiziPutniNalogAsync(pn.PutniNalogId);
                if (success)
                {
                    MessageBox.Show(msg, "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPutniNalozi();
                }
                else
                {
                    MessageBox.Show(msg, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnIzvozZarade_Click(object sender, RoutedEventArgs e)
    {
        var win = new IzvozZaZaradeWindow { Owner = Window.GetWindow(this) };
        win.ShowDialog();
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgPutniNalozi, "Evidencija putnih naloga i dnevnica", "PutniNalozi");

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (DgPutniNalozi?.SelectedItem is PutniNalog pn)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;
                using var db = new AccountingDbContext(options);
                var full = await new PutniNalogService(db).GetPutniNalogByIdAsync(pn.PutniNalogId) ?? pn;
                var firma = await db.Firme.FirstOrDefaultAsync() ?? AppSession.TrenutnaFirma ?? new Firma { Naziv = "NAZIV FIRME" };

                var pdfBytes = PdfReportService.GenerisiPutniNalogPdf(firma, full);
                string siguranBroj = (full.BrojNaloga ?? "PN").Replace('/', '_').Replace('\\', '_');
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"PutniNalog_{siguranBroj}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                await System.IO.File.WriteAllBytesAsync(tempPath, pdfBytes);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri generisanju obrasca Putnog Naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("Molimo izaberite putni nalog za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

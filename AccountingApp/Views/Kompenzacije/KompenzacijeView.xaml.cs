using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Kompenzacije;

public partial class KompenzacijeView : UserControl
{
    private List<Kompenzacija> _sveKompenzacije = new();
    private List<ObostranoDugovanjeCandidate> _kandidati = new();

    public KompenzacijeView()
    {
        InitializeComponent();
        Loaded += KompenzacijeView_Loaded;
    }

    private void KompenzacijeView_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajSveData();
    }

    private void UcitajSveData()
    {
        LoadKompenzacije();
        LoadKandidate();
    }

    private async void LoadKompenzacije()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KompenzacijaService(db);
            _sveKompenzacije = await service.GetKompenzacijeAsync();
            Filtriraj();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kompenzacija: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadKandidate()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KompenzacijaService(db);
            _kandidati = await service.GetObostranaDugovanjaAsync();
            DgKandidati.ItemsSource = _kandidati;
        }
        catch { }
    }

    private void Filtriraj()
    {
        if (DgKompenzacije == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";

        var filtered = _sveKompenzacije.Where(k =>
            string.IsNullOrEmpty(search) ||
            k.BrojDokumenta.ToLower().Contains(search) ||
            k.NazivPartnera.ToLower().Contains(search)
        ).ToList();

        DgKompenzacije.ItemsSource = filtered;
        if (filtered.Any()) DgKompenzacije.SelectedIndex = 0;
    }

    private void DgKompenzacije_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            DgKompenzacijaStavke.ItemsSource = k.Stavke;
        }
        else
        {
            DgKompenzacijaStavke.ItemsSource = null;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private async void BtnNovaKompenzacija_Click(object sender, RoutedEventArgs e)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite($"Data Source={AppConfig.DbPath}")
            .Options;
        using var db = new AccountingDbContext(options);
        var win = new KompenzacijaEditWindow(new Kompenzacija(), db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) UcitajSveData();
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            if (k.IsKnjizeno)
            {
                MessageBox.Show("Proknjižene kompenzacije se ne mogu menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var full = await new KompenzacijaService(db).GetKompenzacijaByIdAsync(k.KompenzacijaId);
            if (full != null)
            {
                var win = new KompenzacijaEditWindow(full, db) { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true) UcitajSveData();
            }
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            if (k.IsKnjizeno)
            {
                MessageBox.Show("Proknjižene kompenzacije se ne mogu brisati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Da li ste sigurni da želite obrisati kompenzaciju br. {k.BrojDokumenta}?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;
                using var db = new AccountingDbContext(options);
                await new KompenzacijaService(db).ObrisiKompenzacijuAsync(k.KompenzacijaId);
                UcitajSveData();
            }
        }
    }

    private async void BtnKnjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            if (k.IsKnjizeno)
            {
                MessageBox.Show("Kompenzacija je već proknjižena.", "Obaveštenje", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show($"Da li želite da proknjižite kompenzaciju br. {k.BrojDokumenta} u Glavnu knjigu i zatvorite fakture u IOS-u?", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;
                using var db = new AccountingDbContext(options);
                var (success, msg, nalogId) = await new KompenzacijaService(db).KnjiziIZatvoriKompenzacijuAsync(k.KompenzacijaId);
                if (success)
                {
                    MessageBox.Show(msg, "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    UcitajSveData();
                }
                else
                {
                    MessageBox.Show(msg, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgKompenzacije, "Evidencija kompenzacija i poravnanja", "Kompenzacije_Poravnanja");

    private void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (DgKompenzacije?.SelectedItem is Kompenzacija k)
        {
            MessageBox.Show($"Generisanje PDF Izjave o kompenzaciji br. {k.BrojDokumenta} je pripremljeno za štampu.", "Štampa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnOsveziSkeniranje_Click(object sender, RoutedEventArgs e)
    {
        LoadKandidate();
    }

    private async void BtnPrebijKandidata_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ObostranoDugovanjeCandidate kand)
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var novKompenzacija = new Kompenzacija
            {
                PartnerId = kand.PartnerId,
                NazivPartnera = kand.NazivPartnera,
                Datum = DateTime.Today,
                Napomena = $"Automatski predlog prebijanja (Potraživanje: {kand.PotrazivanjeKupac:N2}, Obaveza: {kand.ObavezaDobavljac:N2})"
            };

            var win = new KompenzacijaEditWindow(novKompenzacija, db) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                UcitajSveData();
            }
        }
    }
}

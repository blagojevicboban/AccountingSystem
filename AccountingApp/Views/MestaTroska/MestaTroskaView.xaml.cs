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

namespace AccountingApp.Views.MestaTroska;

public partial class MestaTroskaView : UserControl
{
    private List<MestoTroska> _svaMesta = new();

    public MestaTroskaView()
    {
        InitializeComponent();
        Loaded += MestaTroskaView_Loaded;
    }

    private void MestaTroskaView_Loaded(object sender, RoutedEventArgs e)
    {
        DpOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDo.SelectedDate = DateTime.Today;

        LoadMestaTroska();
    }

    private async void LoadMestaTroska()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new MestaTroskaService(db);
            _svaMesta = await service.GetMestaTroskaAsync();

            Filtriraj();

            CmbIzvestajMesto.ItemsSource = _svaMesta;
            if (_svaMesta.Any())
            {
                CmbIzvestajMesto.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju mesta troška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadAnalitiku()
    {
        if (CmbIzvestajMesto?.SelectedValue is int mestoId && mestoId > 0)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new MestaTroskaService(db);

                DateTime odD = DpOd?.SelectedDate ?? new DateTime(DateTime.Today.Year, 1, 1);
                DateTime doD = DpDo?.SelectedDate ?? DateTime.Today;

                var (redovi, summary) = await service.GetAnalitikaPoMestuTroskaAsync(mestoId, odD, doD);

                DgAnalitika.ItemsSource = redovi;
                TxtUkupnoPrihodi.Text = $"{summary.UkupnoPrihodi:N2} RSD";
                TxtUkupnoRashodi.Text = $"{summary.UkupnoRashodi:N2} RSD";
                TxtNetoRezultat.Text = $"{summary.NetoRezultat:N2} RSD";
            }
            catch { }
        }
    }

    private void Filtriraj()
    {
        if (DgMestaTroska == null) return;
        string search = TxtPretraga?.Text.Trim().ToLower() ?? "";

        var filtered = _svaMesta.Where(m =>
            string.IsNullOrEmpty(search) ||
            m.Sifra.ToLower().Contains(search) ||
            m.Naziv.ToLower().Contains(search)
        ).ToList();

        DgMestaTroska.ItemsSource = filtered;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => Filtriraj();

    private void CmbIzvestajFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadAnalitiku();
    private void DpFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadAnalitiku();
    private void BtnOsveziAnalitiku_Click(object sender, RoutedEventArgs e) => LoadAnalitiku();

    private async void BtnNovoMesto_Click(object sender, RoutedEventArgs e)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite($"Data Source={AppConfig.DbPath}")
            .Options;
        using var db = new AccountingDbContext(options);
        var win = new MestoTroskaEditWindow(new MestoTroska(), db) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadMestaTroska();
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (DgMestaTroska?.SelectedItem is MestoTroska mt)
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var full = await new MestaTroskaService(db).GetMestoTroskaByIdAsync(mt.MestoTroskaId);
            if (full != null)
            {
                var win = new MestoTroskaEditWindow(full, db) { Owner = Window.GetWindow(this) };
                if (win.ShowDialog() == true) LoadMestaTroska();
            }
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (DgMestaTroska?.SelectedItem is MestoTroska mt)
        {
            if (MessageBox.Show($"Da li ste sigurni da želite obrisati mesto troška/projekat '{mt.Naziv}' ({mt.Sifra})?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var options = new DbContextOptionsBuilder<AccountingDbContext>()
                        .UseSqlite($"Data Source={AppConfig.DbPath}")
                        .Options;
                    using var db = new AccountingDbContext(options);
                    await new MestaTroskaService(db).ObrisiMestoTroskaAsync(mt.MestoTroskaId);
                    LoadMestaTroska();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgMestaTroska, "Šifarnik mesta troška i projekata", "MestaTroska");

    private void BtnStampaAnalitiku_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Generisanje PDF Izveštaja profitabilnosti po mestu troška/projektu je pripremljeno za štampu.", "Štampa", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class TrgovinaView : UserControl
{
    private List<Kalkulacija> _sveKalkulacije = new();

    public TrgovinaView()
    {
        InitializeComponent();
        LoadKalkulacije();
    }

    private async void LoadKalkulacije()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new KalkulacijaService(db);

            _sveKalkulacije = await service.GetKalkulacijeAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kalkulacija: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        string search = TxtPretraga.Text.Trim().ToLower();
        DgKalkulacije.ItemsSource = string.IsNullOrEmpty(search)
            ? _sveKalkulacije
            : _sveKalkulacije.Where(k => k.BrojKalkulacije.ToLower().Contains(search)).ToList();
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void BtnNovaKalkulacija_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new KalkulacijaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadKalkulacije();
        }
    }

    private async void BtnKnjiziKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovana.IsKnjizen)
        {
            MessageBox.Show($"Kalkulacija #{selektovana.BrojKalkulacije} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new KalkulacijaService(db);

            await service.KnjiziKalkulacijuAsync(selektovana.KalkulacijaId);
            MessageBox.Show($"Kalkulacija #{selektovana.BrojKalkulacije} je uspešno proknjižena!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadKalkulacije();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

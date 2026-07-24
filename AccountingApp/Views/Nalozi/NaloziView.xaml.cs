using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Nalozi;

public partial class NaloziView : UserControl
{
    private List<Nalog> _allNalozi = new();

    public NaloziView()
    {
        InitializeComponent();
        LoadNalozi();
    }

    private async void LoadNalozi()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            _allNalozi = await service.GetNaloziAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading entries: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        string search = TxtPretraga.Text.Trim().ToLower();
        bool samoKnjizeni = ChkSamoProknjizeni.IsChecked ?? false;

        var filtered = _allNalozi.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToLower().Contains(search) || (n.Opis != null && n.Opis.ToLower().Contains(search))) &&
            (!samoKnjizeni || n.IsKnjizen)
        ).ToList();

        DgNalozi.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgNalozi.SelectedIndex = 0;
        }
        else
        {
            DgStavke.ItemsSource = null;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void DgNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selectedNalog)
        {
            TxtDetailHeader.Text = $"📋 Line Items for Entry #{selectedNalog.BrojNaloga} ({selectedNalog.Opis})";
            DgStavke.ItemsSource = selectedNalog.Stavke;
        }
    }

    private void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("New journal entry editor ready!", "New Entry", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selectedNalog)
        {
            if (selectedNalog.IsKnjizen)
            {
                MessageBox.Show($"Entry #{selectedNalog.BrojNaloga} is already posted!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new NaloziService(db);

                await service.KnjiziNalogAsync(selectedNalog.NalogId);
                MessageBox.Show($"Entry #{selectedNalog.BrojNaloga} successfully posted!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadNalozi();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Posting error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

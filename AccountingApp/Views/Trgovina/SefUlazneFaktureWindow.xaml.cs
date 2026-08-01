using System.Windows;
using AccountingData;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class SefUlazneFaktureWindow : Window
{
    public SefUlazneFaktureWindow()
    {
        InitializeComponent();
        DpOdDatuma.SelectedDate = DateTime.Today.AddDays(-30);
        Loaded += SefUlazneFaktureWindow_Loaded;
    }

    private void SefUlazneFaktureWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajUlazneFakture();
    }

    private async void UcitajUlazneFakture()
    {
        try
        {
            TxtStatus.Text = "Preuzimanje faktura sa SEF-a...";
            DateTime odDatuma = DpOdDatuma.SelectedDate ?? DateTime.Today.AddDays(-30);

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new SefService(db);

            var res = await service.PreuzmiUlazneFaktureAsync(odDatuma);
            if (res.Success && res.Data != null)
            {
                DgUlazneFakture.ItemsSource = res.Data;
                TxtStatus.Text = $"Preuzeto {res.Data.Count} ulaznih e-faktura od datuma {odDatuma:dd.MM.yyyy}.";
            }
            else
            {
                TxtStatus.Text = $"Greška: {res.Message}";
                MessageBox.Show(res.Message, "SEF Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Greška pri preuzimanju.";
            MessageBox.Show($"Greška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajUlazneFakture();
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

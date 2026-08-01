using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Partneri;

public partial class KursnaListaWindow : Window
{
    private List<KursnaListaStavka> _tekuciKursevi = new();

    public KursnaListaWindow()
    {
        InitializeComponent();
        DpDatumKurseve.SelectedDate = DateTime.Today;
        Loaded += KursnaListaWindow_Loaded;
    }

    private void KursnaListaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajKurseve();
    }

    private async void UcitajKurseve(bool forsirajNbs = false)
    {
        try
        {
            TxtStatus.Text = "Učitavanje kursne liste NBS...";
            DateTime datum = DpDatumKurseve.SelectedDate ?? DateTime.Today;

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KursnaListaService(db);

            _tekuciKursevi = forsirajNbs 
                ? await service.OsveziSaNbsAsync(datum)
                : await service.GetKursnaListaZaDatumAsync(datum);

            DgKursevi.ItemsSource = _tekuciKursevi;
            TxtStatus.Text = $"Učitano {_tekuciKursevi.Count} valuta sa kursne liste NBS za {datum:dd.MM.yyyy}.";

            PreracunajDevize();
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Greška pri učitavanju.";
            MessageBox.Show($"Greška pri učitavanju kursne liste: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajKurseve(forsirajNbs: true);
    }

    private void Kalkulator_InputChanged(object sender, TextChangedEventArgs e)
    {
        PreracunajDevize();
    }

    private void CmbValuta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PreracunajDevize();
    }

    private void PreracunajDevize()
    {
        if (TxtIznosDevize == null || TxtRezultatRsd == null || CmbValuta == null) return;

        string valutaText = (CmbValuta.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "EUR";
        string valutaKod = valutaText.Split(' ')[0].Trim();

        if (!decimal.TryParse(TxtIznosDevize.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal iznos))
        {
            TxtRezultatRsd.Text = "0.00 RSD";
            return;
        }

        var stavka = _tekuciKursevi.FirstOrDefault(k => k.ValutaOznaka.Equals(valutaKod, StringComparison.OrdinalIgnoreCase));
        if (stavka != null && stavka.Jedinica > 0)
        {
            decimal rsd = Math.Round((iznos * stavka.SrednjiKurs) / stavka.Jedinica, 2);
            TxtRezultatRsd.Text = $"{rsd:N2} RSD";
        }
        else
        {
            TxtRezultatRsd.Text = "0.00 RSD";
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

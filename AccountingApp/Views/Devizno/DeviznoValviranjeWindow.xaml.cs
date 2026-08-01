using System.Windows;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Devizno;

public partial class DeviznoValviranjeWindow : Window
{
    private List<DeviznoKnjigovodstvoResult> _rezultati = new();

    public DeviznoValviranjeWindow()
    {
        InitializeComponent();
        DpNaDan.SelectedDate = new DateTime(DateTime.Today.Year, 12, 31);
        Loaded += DeviznoValviranjeWindow_Loaded;
    }

    private void DeviznoValviranjeWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Izracunaj();
    }

    private async void Izracunaj()
    {
        try
        {
            DateTime naDan = DpNaDan.SelectedDate ?? DateTime.Today;
            decimal.TryParse(TxtKursEur.Text.Trim(), out decimal kursEur);
            decimal.TryParse(TxtKursUsd.Text.Trim(), out decimal kursUsd);

            if (kursEur <= 0) kursEur = 117.20m;
            if (kursUsd <= 0) kursUsd = 108.50m;

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new DeviznoKnjigovodstvoService(db);

            _rezultati = await service.ObracunajValviranjeAsync(naDan, kursEur, kursUsd);
            DgValviranje.ItemsSource = _rezultati;

            decimal ukupneRazlike = _rezultati.Sum(r => r.KursnaRazlikaRsd);
            TxtStatus.Text = $"Proračunato {_rezultati.Count} deviznih konta. Ukupna netovana kursna razlika: {ukupneRazlike:N2} RSD.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri proračunu kursnih razlika: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnIzracunaj_Click(object sender, RoutedEventArgs e)
    {
        Izracunaj();
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (!_rezultati.Any(r => r.KursnaRazlikaRsd != 0))
        {
            MessageBox.Show("Nema kursnih razlika za knjiženje.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            DateTime naDan = DpNaDan.SelectedDate ?? DateTime.Today;
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new DeviznoKnjigovodstvoService(db);

            var (success, message, nalog) = await service.ProknjiziValviranjeAsync(naDan, _rezultati);

            if (success)
            {
                MessageBox.Show($"✅ {message}", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show($"❌ {message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

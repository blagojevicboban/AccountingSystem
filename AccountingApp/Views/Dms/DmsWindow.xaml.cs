using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace AccountingApp.Views.Dms;

public partial class DmsWindow : Window
{
    private readonly int? _nalogId;
    private readonly int? _racunId;
    private readonly int? _kalkulacijaId;
    private List<DokumentPrilog> _prilozi = new();

    public DmsWindow(int? nalogId = null, int? racunId = null, int? kalkulacijaId = null)
    {
        InitializeComponent();
        _nalogId = nalogId;
        _racunId = racunId;
        _kalkulacijaId = kalkulacijaId;

        if (_nalogId.HasValue) TxtNaslovDms.Text = $"📎 DMS Prilozi uz Nalog Knjiženja #{_nalogId.Value}";
        else if (_racunId.HasValue) TxtNaslovDms.Text = $"📎 DMS Prilozi uz Fakturu #{_racunId.Value}";
        else if (_kalkulacijaId.HasValue) TxtNaslovDms.Text = $"📎 DMS Prilozi uz Kalkulaciju #{_kalkulacijaId.Value}";

        Loaded += DmsWindow_Loaded;
    }

    private void DmsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajPriloge();
    }

    private async void UcitajPriloge()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new DmsService(db);

            if (_nalogId.HasValue)
                _prilozi = await service.GetPriloziZaNalogAsync(_nalogId.Value);

            DgPrilozi.ItemsSource = _prilozi;
            TxtBrojDokumenata.Text = $"Broj dokumenata: {_prilozi.Count}";
            TxtStatus.Text = $"Učitano {_prilozi.Count} priloženih dokumenta.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju priloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnDodajPrilog_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Izaberite dokument za prilaganje",
            Filter = "Svi podržani dokumenti (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png|PDF Dokumenti (*.pdf)|*.pdf|Slike (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new DmsService(db);

                var (success, message, prilog) = await service.DodajPrilogAsync(_nalogId, _racunId, _kalkulacijaId, openDialog.FileName, "Ulazni Račun");

                if (success)
                {
                    MessageBox.Show($"✅ {message}", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    UcitajPriloge();
                }
                else
                {
                    MessageBox.Show($"❌ {message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri prilaganju fajla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnOtvoriFajl_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is DokumentPrilog prilog)
        {
            try
            {
                if (File.Exists(prilog.PutanjaFajla))
                {
                    Process.Start(new ProcessStartInfo(prilog.PutanjaFajla) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Fajl ne postoji na navedenoj putanji na disku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri otvaranju fajla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnObrisiFajl_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is DokumentPrilog prilog)
        {
            var res = MessageBox.Show($"Da li ste sigurni da želite da obrišete prilog '{prilog.NazivFajla}'?", "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new DmsService(db);

                var (success, message) = await service.ObrisiPrilogAsync(prilog.DokumentPrilogId);
                if (success)
                {
                    UcitajPriloge();
                }
                else
                {
                    MessageBox.Show($"❌ {message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

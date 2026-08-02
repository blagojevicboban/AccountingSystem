using System.Windows;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Trgovina;

public partial class FiskalniRacunWindow : Window
{
    private readonly int _racunId;
    private RacunOtpremnica? _racun;

    public FiskalniRacunWindow(int racunId)
    {
        InitializeComponent();
        _racunId = racunId;
        Loaded += FiskalniRacunWindow_Loaded;
    }

    private async void FiskalniRacunWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            _racun = await db.RacuniOtpremnice
                .Include(r => r.Stavke)
                .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == _racunId);

            if (_racun != null)
            {
                TxtUkupnoZaUplatu.Text = $"{_racun.UkupnoZaUplatu:N2} RSD";

                if (_racun.FiskalniStatus == FiskalniStatus.Fiskalizovan)
                {
                    TxtFiskalniJournal.Text = $"=== RAČUN JE VEĆ FISKALIZOVAN ===\n" +
                                               $"Broj fiskalnog računa: {_racun.FiskalniBroj}\n" +
                                               $"Datum fiskalizacije: {_racun.FiskalniDatum:dd.MM.yyyy HH:mm:ss}\n" +
                                               $"QR Kod URL: {_racun.FiskalniQrKod}\n";
                    TxtVerificationUrl.Text = _racun.FiskalniQrKod ?? "";
                    BtnFiskalizuj.IsEnabled = false;
                }
                else if (_racun.FiskalniStatus == FiskalniStatus.Simulacija)
                {
                    TxtFiskalniJournal.Text = "=== SIMULIRAN RAČUN — NIJE FISKALIZOVAN ===\n" +
                                              $"Broj: {_racun.FiskalniBroj}\n" +
                                              $"Datum: {_racun.FiskalniDatum:dd.MM.yyyy HH:mm:ss}\n\n" +
                                              "Ovaj račun nije evidentiran u Poreskoj upravi.\n" +
                                              "Pokrenite fiskalizaciju ponovo kada PFR bude dostupan.";
                    TxtStatus.Text = "⚠️ SIMULACIJA — račun NIJE fiskalizovan!";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnFiskalizuj_Click(object sender, RoutedEventArgs e)
    {
        string nacinPlacanja = "Cash";
        if (RbKartica.IsChecked == true) nacinPlacanja = "Card";
        if (RbVirman.IsChecked == true) nacinPlacanja = "WireTransfer";

        try
        {
            BtnFiskalizuj.IsEnabled = false;
            TxtStatus.Text = "Slanje zahteva PFR servisu...";

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new EsirFiskalizacijaService(db);

            var (success, simulacija, message, log) = await service.FiskalizujRacunAsync(_racunId, nacinPlacanja);

            if (success && log != null)
            {
                TxtFiskalniJournal.Text = log.RawJsonResponse;
                TxtVerificationUrl.Text = log.VerificationUrl;

                if (simulacija)
                {
                    TxtStatus.Text = "⚠️ SIMULACIJA — račun NIJE fiskalizovan!";
                    MessageBox.Show($"⚠️ {message}\n\nBroj: {log.InvoiceNumber}",
                        "Simulirani račun — bez fiskalizacije", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    TxtStatus.Text = "Račun je uspešno fiskalizovan!";
                    MessageBox.Show($"✅ {message}\n\nFiskalni broj: {log.InvoiceNumber}", "Fiskalizacija uspešna", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                TxtStatus.Text = "Greška pri fiskalizaciji.";
                MessageBox.Show($"❌ {message}", "Greška PFR", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnFiskalizuj.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Greška pri radu sa PFR-om.";
            MessageBox.Show($"Greška pri fiskalizaciji: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnFiskalizuj.IsEnabled = true;
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

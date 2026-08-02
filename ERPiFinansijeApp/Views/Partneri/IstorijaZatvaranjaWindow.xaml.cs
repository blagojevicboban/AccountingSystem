using System.Windows;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Partneri;

public partial class IstorijaZatvaranjaWindow : Window
{
    private readonly Partner _partner;

    public bool NestoOtkazano { get; private set; }

    public IstorijaZatvaranjaWindow(Partner partner)
    {
        InitializeComponent();
        _partner = partner;
        TxtNaslov.Text = $"🕘 Istorija zatvaranja — {partner.Naziv}";
        LoadIstoriju();
    }

    private async void LoadIstoriju()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new ZatvaranjeStavkiService(db);

            DgIstorija.ItemsSource = await service.GetIstorijaZatvaranjaAsync(_partner.PartnerId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju istorije zatvaranja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOtkaziZatvaranje_Click(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Samo Administrator može otkazati zatvaranje stavki.", "Nedozvoljeno", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DgIstorija.SelectedItem is not ZatvaranjeStavke zatvaranje)
        {
            MessageBox.Show("Izaberite zatvaranje koje želite da otkažete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Otkazati zatvaranje u iznosu {zatvaranje.Iznos:N2} RSD od {zatvaranje.DatumZatvaranja:dd.MM.yyyy}?",
            "Potvrda", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new ZatvaranjeStavkiService(db);

            await service.OtkaziZatvaranjeAsync(zatvaranje.ZatvaranjeStavkeId,
                AppSession.TrenutniKorisnik?.KorisnikId, AppSession.TrenutniKorisnik?.KorisnickoIme);

            NestoOtkazano = true;
            LoadIstoriju();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otkazivanju zatvaranja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

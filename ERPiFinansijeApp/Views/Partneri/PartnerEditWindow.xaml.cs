using System.Windows;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Partneri;

public partial class PartnerEditWindow : Window
{
    private readonly Partner _partner;
    private readonly bool _jeSintetickiPartner;

    /// <summary>Sačuvani/promovisani partner — pozivalac njime osvežava listu i ponovo bira red.</summary>
    public Partner? Sacuvan { get; private set; }

    public PartnerEditWindow(Partner partner)
    {
        InitializeComponent();
        _partner = partner;
        _jeSintetickiPartner = partner.PartnerId <= 0;

        TxtNaslov.Text = _jeSintetickiPartner ? "✏️ Izmena podataka partnera (nepovezan konto)" : "✏️ Izmena podataka partnera";
        TxtNapomenaPromocija.Visibility = _jeSintetickiPartner ? Visibility.Visible : Visibility.Collapsed;

        TxtNaziv.Text = partner.Naziv;
        TxtAdresa.Text = partner.Adresa;
        TxtPttIMesto.Text = partner.PttIMesto;
        TxtPib.Text = partner.Pib;
        TxtMaticniBroj.Text = partner.MaticniBroj;
        TxtTelefon.Text = partner.Telefon;
        TxtZiroRacun.Text = partner.ZiroRacun;

        UcitajKonta();
    }

    private async void UcitajKonta()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            KontoPicker.Poveži(CmbKonto, await db.Konta.ToListAsync(), prefiks: "");
            KontoPicker.PostaviKonto(CmbKonto, _partner.KontoPartnera ?? _partner.SifraPartnera);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string naziv = TxtNaziv.Text.Trim();
        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Naziv je obavezan.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string? konto = KontoPicker.IzabraniKonto(CmbKonto);
        if (string.IsNullOrWhiteSpace(konto))
        {
            MessageBox.Show("Izaberite konto partnera.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var podaci = new Partner
        {
            Naziv = naziv,
            Adresa = string.IsNullOrWhiteSpace(TxtAdresa.Text) ? null : TxtAdresa.Text.Trim(),
            PttIMesto = string.IsNullOrWhiteSpace(TxtPttIMesto.Text) ? null : TxtPttIMesto.Text.Trim(),
            Pib = string.IsNullOrWhiteSpace(TxtPib.Text) ? null : TxtPib.Text.Trim(),
            MaticniBroj = string.IsNullOrWhiteSpace(TxtMaticniBroj.Text) ? null : TxtMaticniBroj.Text.Trim(),
            Telefon = string.IsNullOrWhiteSpace(TxtTelefon.Text) ? null : TxtTelefon.Text.Trim(),
            ZiroRacun = string.IsNullOrWhiteSpace(TxtZiroRacun.Text) ? null : TxtZiroRacun.Text.Trim(),
            KontoPartnera = konto
        };

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new PartnerPromocijaService(db);

            string? brojKontaZaPromociju = _jeSintetickiPartner ? (_partner.KontoPartnera ?? _partner.SifraPartnera) : null;
            Sacuvan = await service.SacuvajPartneraAsync(_partner.PartnerId, brojKontaZaPromociju, podaci);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

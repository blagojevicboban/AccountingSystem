using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Devizno;

public partial class UvoznaKalkulacijaWindow : Window
{
    private readonly UvoznaKalkulacija _kalkulacija = new();
    private readonly ObservableCollection<UvoznaStavka> _stavke = new();

    public UvoznaKalkulacijaWindow()
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        Loaded += UvoznaKalkulacijaWindow_Loaded;
    }

    private async void UvoznaKalkulacijaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var partneri = await db.Partneri.ToListAsync();
            var magacini = await db.Magacini.ToListAsync();

            CmbInoPartner.ItemsSource = partneri;
            if (partneri.Any()) CmbInoPartner.SelectedIndex = 0;

            CmbMagacin.ItemsSource = magacini;
            if (magacini.Any()) CmbMagacin.SelectedIndex = 0;

            // Dodajemo 2 demo stavke
            _stavke.Add(new UvoznaStavka { ArtikalId = 1, Kolicina = 100, InoCenaDevize = 25m, CarinaProcenat = 10m });
            _stavke.Add(new UvoznaStavka { ArtikalId = 2, Kolicina = 50, InoCenaDevize = 40m, CarinaProcenat = 5m });

            Rekalkulisi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri inicijalizaciji uvoza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Rekalkulisi()
    {
        _kalkulacija.BrojKalkulacije = TxtBrojKalkulacije.Text.Trim();
        _kalkulacija.InoBrojFakture = TxtInoBrojFakture.Text.Trim();
        _kalkulacija.Valuta = TxtValuta.Text.Trim();

        decimal.TryParse(TxtKurs.Text.Trim(), out decimal kurs);
        _kalkulacija.KursValute = kurs > 0 ? kurs : 117.20m;

        decimal.TryParse(TxtSpedicija.Text.Trim(), out decimal spedicija);
        _kalkulacija.SpedicijaRsd = spedicija;

        decimal.TryParse(TxtPrevoz.Text.Trim(), out decimal prevoz);
        _kalkulacija.PrevozRsd = prevoz;

        decimal.TryParse(TxtOstaliTroskovi.Text.Trim(), out decimal ostali);
        _kalkulacija.OstaliZavisniTroskoviRsd = ostali;

        _kalkulacija.Stavke = _stavke.ToList();

        var service = new UvoznaKalkulacijaService(null!);
        service.ProracunajUvoznuKalkulaciju(_kalkulacija);

        DgStavke.Items.Refresh();
        TxtStatus.Text = $"Ino Faktura: {_kalkulacija.UkupnoDevize:N2} {_kalkulacija.Valuta} ({_kalkulacija.UkupnoFakturaRsd:N2} RSD) | Carina: {_kalkulacija.CarinaRsd:N2} RSD | Ukupna Nabavna Vrednost: {_kalkulacija.UkupnaNabavnaVrednostRsd:N2} RSD";
    }

    private void Troskovi_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded) Rekalkulisi();
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new UvoznaStavka { ArtikalId = _stavke.Count + 1, Kolicina = 10, InoCenaDevize = 15m, CarinaProcenat = 10m });
        Rekalkulisi();
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Rekalkulisi();
            _kalkulacija.InoPartnerId = (int)(CmbInoPartner.SelectedValue ?? 1);
            _kalkulacija.MagacinId = (int)(CmbMagacin.SelectedValue ?? 1);

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new UvoznaKalkulacijaService(db);

            var (success, message, kal) = await service.SacuvajIKnjiziUvozAsync(_kalkulacija);

            if (success)
            {
                MessageBox.Show($"✅ {message}", "Uvoz Proknjižen", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show($"Greška pri čuvanju uvoza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

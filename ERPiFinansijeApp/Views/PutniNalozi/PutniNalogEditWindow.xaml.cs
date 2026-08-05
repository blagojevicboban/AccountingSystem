using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;

namespace ERPiFinansijeApp.Views.PutniNalozi;

public partial class PutniNalogEditWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly PutniNalogService _service;
    private readonly PutniNalog _pn;

    private ObservableCollection<PutniNalogTrosakStavka> _stavkeTroskova = new();

    public PutniNalogEditWindow(PutniNalog pn, AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new PutniNalogService(_db);
        _pn = pn;

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += PutniNalogEditWindow_Loaded;
    }

    private void PutniNalogEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CmbVrsta.SelectedIndex = _pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? 1 : 0;
        TxtZaposleni.Text = _pn.ZaposleniIme;
        TxtJmbg.Text = _pn.Jmbg;
        TxtRelacija.Text = _pn.Relacija;

        DpPolazak.SelectedDate = _pn.DatumPolaska;
        DpPovratak.SelectedDate = _pn.DatumPovratka;

        TxtIznosDnevnice.Text = _pn.IznosDnevniceRsd > 0 ? _pn.IznosDnevniceRsd.ToString("G") : "3000";
        TxtAkontacija.Text = _pn.Akontacija.ToString("G");

        _stavkeTroskova = new ObservableCollection<PutniNalogTrosakStavka>(_pn.StavkeTroskova);
        DgStavkeTroskova.ItemsSource = _stavkeTroskova;

        OsveziProracun();
    }

    private void DpPolazak_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => OsveziProracun();
    private void DpPovratak_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => OsveziProracun();
    private void TxtIznosDnevnice_TextChanged(object sender, TextChangedEventArgs e) => OsveziProracun();
    private void TxtAkontacija_TextChanged(object sender, TextChangedEventArgs e) => OsveziProracun();

    private void OsveziProracun()
    {
        if (TxtSati == null || TxtBrojDnevnica == null || TxtUkupnoZaIsplatu == null) return;

        DateTime polazak = DpPolazak?.SelectedDate ?? DateTime.Now;
        DateTime povratak = DpPovratak?.SelectedDate ?? DateTime.Now.AddDays(1);

        decimal.TryParse(TxtIznosDnevnice?.Text, out decimal iznosDnevnice);
        if (iznosDnevnice <= 0) iznosDnevnice = 3000m;

        decimal.TryParse(TxtAkontacija?.Text, out decimal akontacija);

        var (sati, dnevnice, ukupnoDnevnice) = PutniNalogService.IzracunajDnevnice(polazak, povratak, iznosDnevnice);

        TxtSati.Text = $"{sati:F1} h";
        TxtBrojDnevnica.Text = $"{dnevnice:F1}";

        decimal zbirTroskova = _stavkeTroskova.Sum(s => s.Iznos);
        decimal ukupnoZaIsplatu = Math.Max(0, (ukupnoDnevnice + zbirTroskova) - akontacija);

        TxtUkupnoZaIsplatu.Text = $"{ukupnoZaIsplatu:N2} RSD";
    }

    private void BtnDodajTrosak_Click(object sender, RoutedEventArgs e)
    {
        string vrsta = (CmbVrstaTroska.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gorivo";
        string brojRacuna = TxtBrojRacunaTrosak.Text.Trim();
        decimal.TryParse(TxtIznosTrosak.Text, out decimal iznos);

        if (iznos <= 0)
        {
            MessageBox.Show("Unesite ispravan iznos troška.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _stavkeTroskova.Add(new PutniNalogTrosakStavka
        {
            RedniBroj = _stavkeTroskova.Count + 1,
            VrstaTroska = vrsta,
            BrojRacuna = brojRacuna,
            DatumRacuna = DateTime.Today,
            Iznos = iznos,
            Opis = $"Račun br. {brojRacuna}"
        });

        TxtBrojRacunaTrosak.Text = "";
        TxtIznosTrosak.Text = "";
        OsveziProracun();
    }

    private void BtnUkloniTrosak_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is PutniNalogTrosakStavka st)
        {
            _stavkeTroskova.Remove(st);
            int rbr = 1;
            foreach (var item in _stavkeTroskova) item.RedniBroj = rbr++;
            OsveziProracun();
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string zaposleni = TxtZaposleni.Text.Trim();
        string relacija = TxtRelacija.Text.Trim();

        if (string.IsNullOrWhiteSpace(zaposleni) || string.IsNullOrWhiteSpace(relacija))
        {
            MessageBox.Show("Unesite ime zaposlenog i relaciju putovanja.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal.TryParse(TxtIznosDnevnice.Text, out decimal iznosDnevnice);
        decimal.TryParse(TxtAkontacija.Text, out decimal akontacija);

        _pn.Vrsta = CmbVrsta.SelectedIndex == 1 ? VrstaSlužbenogPutovanja.Inostranstvo : VrstaSlužbenogPutovanja.Zemlja;
        _pn.ZaposleniIme = zaposleni;
        _pn.Jmbg = TxtJmbg.Text.Trim();
        _pn.Relacija = relacija;
        _pn.PrevoznoSredstvo = (CmbPrevoz.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Službeno vozilo";
        _pn.DatumPolaska = DpPolazak.SelectedDate ?? DateTime.Now;
        _pn.DatumPovratka = DpPovratak.SelectedDate ?? DateTime.Now.AddDays(1);
        _pn.IznosDnevniceRsd = iznosDnevnice > 0 ? iznosDnevnice : 3000m;
        _pn.Akontacija = akontacija;

        _pn.StavkeTroskova = _stavkeTroskova.ToList();

        try
        {
            await _service.SacuvajPutniNalogAsync(_pn);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju putnog naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

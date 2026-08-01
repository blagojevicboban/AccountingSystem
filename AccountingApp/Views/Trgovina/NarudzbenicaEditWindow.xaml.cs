using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class NarudzbenicaEditWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly KomercijalaService _service;
    private readonly NarudzbenicaDobavljacu _narudzbenica;
    private ObservableCollection<NarudzbenicaStavka> _stavke = new();
    private List<Artikal> _artikli = new();
    private List<Partner> _partneri = new();

    public NarudzbenicaEditWindow(NarudzbenicaDobavljacu narudzbenica, AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new KomercijalaService(_db);
        _narudzbenica = narudzbenica;

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += NarudzbenicaEditWindow_Loaded;
    }

    private async void NarudzbenicaEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _partneri = await _db.Partneri.OrderBy(p => p.Naziv).ToListAsync();
            CmbDobavljaci.ItemsSource = _partneri;

            _artikli = await _db.Artikli.OrderBy(a => a.Naziv).ToListAsync();
            CmbArtikli.ItemsSource = _artikli;

            DpDatum.SelectedDate = _narudzbenica.Datum;
            DpRokIsporuke.SelectedDate = _narudzbenica.RokIsporuke;

            if (_narudzbenica.PartnerId.HasValue)
            {
                CmbDobavljaci.SelectedValue = _narudzbenica.PartnerId.Value;
            }

            TxtNapomena.Text = _narudzbenica.Napomena;

            if (_narudzbenica.Stavke != null && _narudzbenica.Stavke.Count > 0)
            {
                _stavke = new ObservableCollection<NarudzbenicaStavka>(_narudzbenica.Stavke);
            }

            DgStavke.ItemsSource = _stavke;
            OsvezZbirove();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri inicijalizaciji: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbArtikli_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbArtikli.SelectedItem is Artikal art)
        {
            TxtCena.Text = $"{art.NabavnaCena:N2}";
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        if (CmbArtikli.SelectedItem is not Artikal art)
        {
            MessageBox.Show("Izaberite artikal.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal kol = ParseDecimal(TxtKolicina.Text);
        decimal cena = ParseDecimal(TxtCena.Text);

        if (kol <= 0)
        {
            MessageBox.Show("Unesite ispravnu količinu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        decimal neto = kol * cena;
        decimal pdv = neto * 0.20m;
        decimal bruto = neto + pdv;

        var st = new NarudzbenicaStavka
        {
            RedniBroj = _stavke.Count + 1,
            SifraArtikla = art.SifraArtikla,
            NazivArtikla = art.Naziv,
            JedinicaMere = art.JedinicaMere ?? "kom",
            KolicinaNarucena = kol,
            Cena = cena,
            PdvStopa = 20.0m,
            IznosNeto = neto,
            IznosPdv = pdv,
            IznosBruto = bruto
        };

        _stavke.Add(st);
        OsvezZbirove();
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is NarudzbenicaStavka st)
        {
            _stavke.Remove(st);
            int rbr = 1;
            foreach (var item in _stavke) item.RedniBroj = rbr++;
            OsvezZbirove();
        }
    }

    private void OsvezZbirove()
    {
        decimal ukupnoBruto = _stavke.Sum(s => s.IznosBruto);
        TxtUkupnoBruto.Text = $"{ukupnoBruto:N2} RSD";
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (_stavke.Count == 0)
        {
            MessageBox.Show("Narudžbenica mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _narudzbenica.Datum = DpDatum.SelectedDate ?? DateTime.Today;
        _narudzbenica.RokIsporuke = DpRokIsporuke.SelectedDate;

        if (CmbDobavljaci.SelectedItem is Partner p)
        {
            _narudzbenica.PartnerId = p.PartnerId;
            _narudzbenica.NazivDobavljaca = p.Naziv;
        }

        _narudzbenica.Napomena = TxtNapomena.Text.Trim();
        _narudzbenica.Stavke = _stavke.ToList();

        try
        {
            await _service.SacuvajNarudzbenicuAsync(_narudzbenica);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju narudžbenice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static decimal ParseDecimal(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0m;
        val = val.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

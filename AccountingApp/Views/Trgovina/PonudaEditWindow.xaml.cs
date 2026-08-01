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

public partial class PonudaEditWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly KomercijalaService _service;
    private readonly PonudaPredracun _ponuda;
    private ObservableCollection<PonudaStavka> _stavke = new();
    private List<Artikal> _artikli = new();
    private List<Partner> _partneri = new();

    public PonudaEditWindow(PonudaPredracun ponuda, AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new KomercijalaService(_db);
        _ponuda = ponuda;

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += PonudaEditWindow_Loaded;
    }

    private async void PonudaEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _partneri = await _db.Partneri.OrderBy(p => p.Naziv).ToListAsync();
            CmbPartneri.ItemsSource = _partneri;

            _artikli = await _db.Artikli.OrderBy(a => a.Naziv).ToListAsync();
            CmbArtikli.ItemsSource = _artikli;

            DpDatum.SelectedDate = _ponuda.Datum;
            DpRokVazenja.SelectedDate = _ponuda.RokVazenja;

            if (_ponuda.PartnerId.HasValue)
            {
                CmbPartneri.SelectedValue = _ponuda.PartnerId.Value;
            }

            TxtNapomena.Text = _ponuda.Napomena;

            if (_ponuda.Stavke != null && _ponuda.Stavke.Count > 0)
            {
                _stavke = new ObservableCollection<PonudaStavka>(_ponuda.Stavke);
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
            TxtCena.Text = $"{art.ProdajnaCena:N2}";
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

        var st = new PonudaStavka
        {
            RedniBroj = _stavke.Count + 1,
            SifraArtikla = art.SifraArtikla,
            NazivArtikla = art.Naziv,
            JedinicaMere = art.JedinicaMere ?? "kom",
            Kolicina = kol,
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
        if ((sender as Button)?.DataContext is PonudaStavka st)
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
            MessageBox.Show("Ponuda mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _ponuda.VrstaDokumenta = (CmbVrsta.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ponuda";
        _ponuda.Datum = DpDatum.SelectedDate ?? DateTime.Today;
        _ponuda.RokVazenja = DpRokVazenja.SelectedDate ?? DateTime.Today.AddDays(15);

        if (CmbPartneri.SelectedItem is Partner p)
        {
            _ponuda.PartnerId = p.PartnerId;
            _ponuda.NazivPartnera = p.Naziv;
        }

        _ponuda.Napomena = TxtNapomena.Text.Trim();
        _ponuda.Stavke = _stavke.ToList();

        try
        {
            await _service.SacuvajPonuduAsync(_ponuda);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju ponude: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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

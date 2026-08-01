using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Kompenzacije;

public class OtvorenaStavkaZaKompenzaciju : INotifyPropertyChanged
{
    public OtvorenaStavkaRed Stavka { get; }
    public OtvorenaStavkaZaKompenzaciju(OtvorenaStavkaRed stavka) => Stavka = stavka;

    public int StavkaNalogaId => Stavka.StavkaNalogaId;
    public string BrojDokumenta => Stavka.BrojDokumenta ?? "";
    public DateTime Datum => Stavka.Datum;
    public decimal Preostalo => Stavka.Preostalo;
    public string BrojKonta => Stavka.BrojKonta;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class KompenzacijaEditWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly KompenzacijaService _service;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;
    private readonly Kompenzacija _kompenzacija;

    private ObservableCollection<OtvorenaStavkaZaKompenzaciju> _kupciStavke = new();
    private ObservableCollection<OtvorenaStavkaZaKompenzaciju> _dobavljaciStavke = new();
    private List<Partner> _partneri = new();

    public KompenzacijaEditWindow(Kompenzacija kompenzacija, AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new KompenzacijaService(_db);
        _zatvaranjeService = new ZatvaranjeStavkiService(_db);
        _kompenzacija = kompenzacija;

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += KompenzacijaEditWindow_Loaded;
    }

    private async void KompenzacijaEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _partneri = await _db.Partneri.OrderBy(p => p.Naziv).ToListAsync();
            CmbPartneri.ItemsSource = _partneri;

            DpDatum.SelectedDate = _kompenzacija.Datum;
            TxtNapomena.Text = _kompenzacija.Napomena;

            if (_kompenzacija.PartnerId.HasValue)
            {
                CmbPartneri.SelectedValue = _kompenzacija.PartnerId.Value;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri inicijalizaciji: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void CmbPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPartneri.SelectedValue is int partnerId && partnerId > 0)
        {
            var otvorene = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partnerId, DpDatum.SelectedDate ?? DateTime.Today, samoOtvorene: true);

            var kupci = otvorene.Where(s => s.BrojKonta.StartsWith("2040") || s.BrojKonta.StartsWith("204"))
                                .Select(s => new OtvorenaStavkaZaKompenzaciju(s)).ToList();

            var dobavljaci = otvorene.Where(s => s.BrojKonta.StartsWith("4350") || s.BrojKonta.StartsWith("435"))
                                     .Select(s => new OtvorenaStavkaZaKompenzaciju(s)).ToList();

            foreach (var item in kupci)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
            foreach (var item in dobavljaci)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }

            _kupciStavke = new ObservableCollection<OtvorenaStavkaZaKompenzaciju>(kupci);
            _dobavljaciStavke = new ObservableCollection<OtvorenaStavkaZaKompenzaciju>(dobavljaci);

            DgKupciStavke.ItemsSource = _kupciStavke;
            DgDobavljaciStavke.ItemsSource = _dobavljaciStavke;

            RacunajIznos();
        }
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RacunajIznos();
    }

    private void RacunajIznos()
    {
        decimal zbirKupci = _kupciStavke.Where(s => s.IsSelected).Sum(s => s.Preostalo);
        decimal zbirDobavljaci = _dobavljaciStavke.Where(s => s.IsSelected).Sum(s => s.Preostalo);

        decimal kompenzacija = Math.Min(zbirKupci, zbirDobavljaci);
        if (kompenzacija <= 0 && (zbirKupci > 0 || zbirDobavljaci > 0))
        {
            kompenzacija = Math.Max(zbirKupci, zbirDobavljaci);
        }

        TxtUkupanIznos.Text = $"{kompenzacija:N2} RSD";
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (CmbPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var izabraneKupci = _kupciStavke.Where(s => s.IsSelected).ToList();
        var izabraneDobavljaci = _dobavljaciStavke.Where(s => s.IsSelected).ToList();

        if (izabraneKupci.Count == 0 && izabraneDobavljaci.Count == 0)
        {
            MessageBox.Show("Izaberite bar jednu stavku za kompenzaciju.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _kompenzacija.PartnerId = partner.PartnerId;
        _kompenzacija.NazivPartnera = partner.Naziv;
        _kompenzacija.Datum = DpDatum.SelectedDate ?? DateTime.Today;
        _kompenzacija.Napomena = TxtNapomena.Text.Trim();

        _kompenzacija.Stavke.Clear();
        int rbr = 1;

        foreach (var k in izabraneKupci)
        {
            _kompenzacija.Stavke.Add(new KompenzacijaStavka
            {
                RedniBroj = rbr++,
                StavkaNalogaId = k.StavkaNalogaId,
                BrojDokumenta = k.BrojDokumenta,
                DatumDokumenta = k.Datum,
                Strana = "Duguje",
                BrojKonta = k.BrojKonta,
                IznosFakture = k.Preostalo,
                IznosPreostalo = k.Preostalo,
                IznosZaKompenzaciju = k.Preostalo
            });
        }

        foreach (var d in izabraneDobavljaci)
        {
            _kompenzacija.Stavke.Add(new KompenzacijaStavka
            {
                RedniBroj = rbr++,
                StavkaNalogaId = d.StavkaNalogaId,
                BrojDokumenta = d.BrojDokumenta,
                DatumDokumenta = d.Datum,
                Strana = "Potražuje",
                BrojKonta = d.BrojKonta,
                IznosFakture = d.Preostalo,
                IznosPreostalo = d.Preostalo,
                IznosZaKompenzaciju = d.Preostalo
            });
        }

        try
        {
            await _service.SacuvajKompenzacijuAsync(_kompenzacija);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju kompenzacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

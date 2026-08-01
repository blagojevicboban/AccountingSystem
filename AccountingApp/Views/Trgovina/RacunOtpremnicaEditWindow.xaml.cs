using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class RacunOtpremnicaEditWindow : Window
{
    private readonly RacunOtpremnica? _existingRacun;
    private List<Artikal> _artikli = new();
    public ObservableCollection<RacunStavkaModel> StavkeModels { get; set; } = new();

    public RacunOtpremnicaEditWindow(RacunOtpremnica? existingRacun = null)
    {
        InitializeComponent();
        _existingRacun = existingRacun;
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _artikli = await db.Artikli.OrderBy(a => a.SifraArtikla).ToListAsync();

            ColArtikal.ItemsSource = _artikli;
            ColArtikal.DisplayMemberPath = "Naziv";
            ColArtikal.SelectedValuePath = "SifraArtikla";

            if (_existingRacun != null)
            {
                ChkPredracun.IsChecked = _existingRacun.TipDokumenta == TipRacunOtpremnice.Predracun;
                DpRokVazenja.SelectedDate = _existingRacun.RokVazenjaPredracuna;
                AzurirajNaslovITipPolja();
                TxtBrojRacuna.Text = _existingRacun.BrojRacuna.ToString();
                TxtBrojRacuna.IsReadOnly = true;
                TxtBrojOtpremnice.Text = _existingRacun.BrojOtpremnice ?? _existingRacun.BrojRacuna.ToString();
                DpDatum.SelectedDate = _existingRacun.DatumRacuna;
                TxtKontoKupca.Text = _existingRacun.Partner?.SifraPartnera ?? _existingRacun.KontoKupca;
                TxtRokPlacanja.Text = _existingRacun.RokPlacanjaDana.ToString();
                CmbNacinPlacanja.Text = _existingRacun.NacinPlacanja ?? "Virman (račun)";

                int rbr = 1;
                foreach (var st in _existingRacun.Stavke)
                {
                    StavkeModels.Add(new RacunStavkaModel
                    {
                        RedniBroj = rbr++,
                        SifraArtikla = st.Artikal?.SifraArtikla ?? st.SifraArtikla,
                        Kolicina = st.Kolicina,
                        Cena = st.Cena,
                        RabatProcenat = st.RabatProcenat,
                        PdvProcenat = st.PdvProcenat,
                        IznosBezPdv = st.IznosBezPdv,
                        PdvIznos = st.PdvIznos,
                        UkupanIznos = st.UkupanIznos
                    });
                }
            }
            else
            {
                AzurirajNaslovITipPolja();
                DpDatum.SelectedDate = DateTime.Now;

                int maxBr = (await db.RacuniOtpremnice.Select(r => (int?)r.BrojRacuna).MaxAsync() ?? 0) + 1;
                TxtBrojRacuna.Text = maxBr.ToString("D5");
                TxtBrojOtpremnice.Text = maxBr.ToString("D5");

                StavkeModels.Add(new RacunStavkaModel { RedniBroj = 1, PdvProcenat = 20 });
            }

            DgStavke.ItemsSource = StavkeModels;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju podataka: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChkPredracun_CheckedChanged(object sender, RoutedEventArgs e) => AzurirajNaslovITipPolja();

    private void AzurirajNaslovITipPolja()
    {
        bool jePredracun = ChkPredracun.IsChecked == true;
        TxtRokVazenjaLabel.Visibility = jePredracun ? Visibility.Visible : Visibility.Collapsed;
        DpRokVazenja.Visibility = jePredracun ? Visibility.Visible : Visibility.Collapsed;

        string osnova = jePredracun ? "predračuna" : "računa-otpremnice";
        TxtNaslov.Text = _existingRacun != null
            ? $"✏️ Izmena {osnova} #{_existingRacun.BrojRacuna}"
            : $"➕ Novi {(jePredracun ? "predračun" : "račun - otpremnica")}";
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        StavkeModels.Add(new RacunStavkaModel { RedniBroj = StavkeModels.Count + 1, PdvProcenat = 20 });
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is RacunStavkaModel model)
        {
            StavkeModels.Remove(model);
            int rbr = 1;
            foreach (var s in StavkeModels) s.RedniBroj = rbr++;
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string kontoKupca = TxtKontoKupca.Text.Trim();

        if (!int.TryParse(TxtBrojRacuna.Text.Trim(), out int brRacuna))
        {
            MessageBox.Show("Molimo unesite ispravan broj računa.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(kontoKupca))
        {
            MessageBox.Show("Molimo unesite kupca / konto kupca.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(TxtRokPlacanja.Text, out int rokDana);

        var validneStavke = StavkeModels.Where(s => !string.IsNullOrWhiteSpace(s.SifraArtikla) && s.Kolicina > 0).ToList();
        if (validneStavke.Count == 0)
        {
            MessageBox.Show("Unesite bar jednu validnu stavku robe sa količinom većom od 0.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new RacunOtpremnicaService(db);

            var partner = await db.Partneri.FirstOrDefaultAsync(p => p.SifraPartnera == kontoKupca || p.Naziv == kontoKupca);

            var racun = _existingRacun ?? new RacunOtpremnica();
            racun.TipDokumenta = ChkPredracun.IsChecked == true ? TipRacunOtpremnice.Predracun : TipRacunOtpremnice.Racun;
            racun.RokVazenjaPredracuna = racun.TipDokumenta == TipRacunOtpremnice.Predracun ? DpRokVazenja.SelectedDate : null;
            racun.BrojRacuna = brRacuna;
            racun.BrojOtpremnice = string.IsNullOrWhiteSpace(TxtBrojOtpremnice.Text) ? brRacuna.ToString() : TxtBrojOtpremnice.Text.Trim();
            racun.DatumRacuna = DpDatum.SelectedDate ?? DateTime.Now;
            racun.DatumOtpremnice = racun.DatumRacuna;
            racun.PartnerId = partner?.PartnerId;
            racun.KontoKupca = kontoKupca;
            racun.RokPlacanjaDana = rokDana;
            racun.NacinPlacanja = CmbNacinPlacanja.Text.Trim();

            racun.Stavke = validneStavke.Select((s, idx) =>
            {
                decimal bezPdv = s.IznosBezPdv > 0 ? s.IznosBezPdv : Math.Round(s.Kolicina * s.Cena * (1 - (s.RabatProcenat / 100m)), 2);
                decimal pdv = s.PdvIznos > 0 ? s.PdvIznos : Math.Round(bezPdv * (s.PdvProcenat / 100m), 2);
                decimal ukupno = s.UkupanIznos > 0 ? s.UkupanIznos : bezPdv + pdv;

                return new RacunOtpremnicaStavka
                {
                    RedniBroj = idx + 1,
                    ArtikalId = _artikli.FirstOrDefault(a => a.SifraArtikla == s.SifraArtikla)?.ArtikalId,
                    SifraArtikla = s.SifraArtikla,
                    Kolicina = s.Kolicina,
                    Cena = s.Cena,
                    RabatProcenat = s.RabatProcenat,
                    PdvProcenat = s.PdvProcenat,
                    IznosBezPdv = bezPdv,
                    PdvIznos = pdv,
                    UkupanIznos = ukupno
                };
            }).ToList();

            racun.IznosBezPdv = racun.Stavke.Sum(x => x.IznosBezPdv);
            racun.PdvIznos = racun.Stavke.Sum(x => x.PdvIznos);
            racun.UkupanIznos = racun.Stavke.Sum(x => x.UkupanIznos);

            await service.SaveRacunAsync(racun);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju računa-otpremnice:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class RacunStavkaModel
{
    public int RedniBroj { get; set; }
    public string SifraArtikla { get; set; } = string.Empty;
    public decimal Kolicina { get; set; }
    public decimal Cena { get; set; }
    public decimal RabatProcenat { get; set; }
    public decimal PdvProcenat { get; set; } = 20;
    public decimal IznosBezPdv { get; set; }
    public decimal PdvIznos { get; set; }
    public decimal UkupanIznos { get; set; }
}

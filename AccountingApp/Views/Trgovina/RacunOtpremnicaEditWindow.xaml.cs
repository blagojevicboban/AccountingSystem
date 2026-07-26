using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class RacunOtpremnicaEditWindow : Window
{
    private readonly ObservableCollection<RacunOtpremnicaStavka> _stavke = new();
    private int _existingId = 0;
    private List<Artikal> _sviArtikli = new();

    public RacunOtpremnicaEditWindow(RacunOtpremnica? existing = null)
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        _stavke.CollectionChanged += (s, e) => Preračunaj();

        if (existing != null)
        {
            _existingId = existing.RacunOtpremnicaId;
            TxtBrojRacuna.Text = existing.BrojRacuna;
            DpDatum.SelectedDate = existing.DatumRacuna;
            DpRok.SelectedDate = existing.RokPlacanja;
            TxtNapomena.Text = existing.Napomena;
            CmbPartner.SelectedValue = existing.PartnerId;
            CmbMagacin.SelectedValue = existing.MagacinId;

            foreach (var st in existing.Stavke)
            {
                _stavke.Add(new RacunOtpremnicaStavka
                {
                    RedniBroj = st.RedniBroj,
                    ArtikalId = st.ArtikalId,
                    Kolicina = st.Kolicina,
                    ProdajnaCena = st.ProdajnaCena,
                    RabatProcenat = st.RabatProcenat,
                    StopaPdv = st.StopaPdv,
                    Osnovica = st.Osnovica,
                    IznosPdv = st.IznosPdv,
                    Ukupno = st.Ukupno
                });
            }
            Title = $"Izmena računa #{existing.BrojRacuna}";
        }
        else
        {
            DpDatum.SelectedDate = DateTime.Now;
            DpRok.SelectedDate = DateTime.Now.AddDays(15);
            Title = "Novi račun - otpremnica";
            _ = PredloziBrojAsync();
        }

        _ = UcitajSifarnikeAsync();
    }

    private async Task PredloziBrojAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var brojevi = await db.RacuniOtpremnice.Select(r => r.BrojRacuna).ToListAsync();
            int max = 0;
            foreach (var b in brojevi)
            {
                if (int.TryParse(b, out var num) && num > max) max = num;
            }
            TxtBrojRacuna.Text = (max + 1).ToString();
        }
        catch { }
    }

    private async Task UcitajSifarnikeAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var partneri = await db.Partneri.OrderBy(p => p.Naziv).ToListAsync();
            CmbPartner.ItemsSource = partneri;

            var magacini = await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync();
            CmbMagacin.ItemsSource = magacini;
            if (CmbMagacin.SelectedIndex < 0 && magacini.Count > 0) CmbMagacin.SelectedIndex = 0;

            _sviArtikli = await db.Artikli.OrderBy(a => a.Naziv).ToListAsync();
            ColArtikli.ItemsSource = _sviArtikli;
            ColArtikli.DisplayMemberPath = "Naziv";
            ColArtikli.SelectedValuePath = "ArtikalId";
        }
        catch { }
    }

    private void Preračunaj()
    {
        decimal osn = 0m, pdv = 0m, tot = 0m;
        foreach (var s in _stavke)
        {
            decimal brutovrednost = s.Kolicina * s.ProdajnaCena;
            decimal iznosRabata = brutovrednost * (s.RabatProcenat / 100m);
            s.Osnovica = brutovrednost - iznosRabata;
            s.IznosPdv = s.Osnovica * (s.StopaPdv / 100m);
            s.Ukupno = s.Osnovica + s.IznosPdv;

            osn += s.Osnovica;
            pdv += s.IznosPdv;
            tot += s.Ukupno;
        }

        TxtUkupnoOsnovica.Text = $"{osn:N2} RSD";
        TxtUkupnoPdv.Text = $"{pdv:N2} RSD";
        TxtUkupnoZaUplatu.Text = $"{tot:N2} RSD";
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new RacunOtpremnicaStavka { RedniBroj = _stavke.Count + 1, Kolicina = 1m, StopaPdv = 20m });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is RacunOtpremnicaStavka st)
        {
            _stavke.Remove(st);
            int rb = 1;
            foreach (var item in _stavke) item.RedniBroj = rb++;
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(Preračunaj), System.Windows.Threading.DispatcherPriority.Background);
    }

    private async void BtnSačuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtBrojRacuna.Text))
        {
            MessageBox.Show("Molimo unesite broj računa.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbMagacin.SelectedValue == null)
        {
            MessageBox.Show("Molimo izaberite magacin.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_stavke.Count == 0)
        {
            MessageBox.Show("Račun mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var racun = new RacunOtpremnica
        {
            RacunOtpremnicaId = _existingId,
            BrojRacuna = TxtBrojRacuna.Text.Trim(),
            DatumRacuna = DpDatum.SelectedDate ?? DateTime.Now,
            RokPlacanja = DpRok.SelectedDate,
            PartnerId = (int?)CmbPartner.SelectedValue,
            MagacinId = (int)CmbMagacin.SelectedValue,
            Napomena = TxtNapomena.Text.Trim(),
            Stavke = _stavke.ToList()
        };

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var service = new RacunOtpremnicaService(db);
            await service.SaveRacunAsync(racun);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}

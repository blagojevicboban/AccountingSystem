using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class KalkulacijaEditWindow : Window
{
    private readonly ObservableCollection<KalkulacijaStavka> _stavke = new();
    private readonly Kalkulacija? _existingKalkulacija;
    private bool _updating;

    public KalkulacijaEditWindow(Kalkulacija? existingKalkulacija = null)
    {
        InitializeComponent();
        _existingKalkulacija = existingKalkulacija;
        DgStavke.ItemsSource = _stavke;
        // Podrazumevane vrednosti se postavljaju ovde (posle InitializeComponent), ne
        // kao XAML Text="..." literali — Text="0"/"20" bi okinulo TextChanged tokom
        // same InitializeComponent() (kada TextBox biva konstruisan), pre nego što su
        // kasnije deklarisani elementi (TxtSvegaTroskovi i dr. u "Obračun" sekciji)
        // uopšte kreirani, izazivajući NullReferenceException u Prikazi().
        TxtPoreskaStopaProcenat.Text = "20";
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var kartice = new MaterijalnaKarticaService(db);

            var magacini = await kartice.GetMagaciniAsync();
            CmbMagacin.ItemsSource = magacini;

            if (_existingKalkulacija != null)
            {
                Title = $"Izmena kalkulacije #{_existingKalkulacija.BrojKalkulacije}";
                TxtBrojKalkulacije.Text = _existingKalkulacija.BrojKalkulacije.ToString();
                DpDatum.SelectedDate = _existingKalkulacija.Datum;
                TxtSifraDobavljaca.Text = _existingKalkulacija.SifraDobavljaca;
                TxtBrojRacuna.Text = _existingKalkulacija.BrojRacuna;
                TxtBrojOtpremnice.Text = _existingKalkulacija.BrojOtpremnice;
                CmbMagacin.SelectedItem = magacini.FirstOrDefault(m => m.SifraMagacina == _existingKalkulacija.SifraMagacina);
                TxtTransportniTroskovi.Text = _existingKalkulacija.TransportniTroskovi.ToString("N2");
                TxtTroskoviUskladistenja.Text = _existingKalkulacija.TroskoviUskladistenja.ToString("N2");
                TxtUtovarIstovar.Text = _existingKalkulacija.UtovarIstovar.ToString("N2");
                TxtTransportnoOsiguranje.Text = _existingKalkulacija.TransportnoOsiguranje.ToString("N2");
                TxtOstaliTroskovi.Text = _existingKalkulacija.OstaliTroskovi.ToString("N2");
                TxtMarzaProcenat.Text = _existingKalkulacija.MarzaProcenat.ToString("N2");
                TxtPoreskaStopaProcenat.Text = _existingKalkulacija.PoreskaStopaProcenat.ToString("N2");

                if (_existingKalkulacija.Stavke.Count == 0)
                {
                    TxtNabavnaVrednost.Text = _existingKalkulacija.NabavnaVrednost.ToString("N2");
                }

                // Nove KalkulacijaStavka instance (bez originalnog Id-ja) — SaveKalkulacijuAsync
                // pri izmeni briše sve postojeće stavke i upisuje ceo tekući spisak iznova, pa
                // čuvanje starog Id-ja ovde nema svrhe i samo bi otvorilo rizik od konflikta.
                foreach (var s in _existingKalkulacija.Stavke.OrderBy(s => s.RedniBroj))
                {
                    _stavke.Add(new KalkulacijaStavka
                    {
                        RedniBroj = s.RedniBroj,
                        SifraArtikla = s.SifraArtikla,
                        Kolicina = s.Kolicina,
                        NabavnaCena = s.NabavnaCena,
                        ProdajnaCena = s.ProdajnaCena
                    });
                }
            }
            else
            {
                DpDatum.SelectedDate = DateTime.Now;
                if (magacini.Count > 0) CmbMagacin.SelectedIndex = 0;

                int max = await db.Kalkulacije.Select(k => (int?)k.BrojKalkulacije).MaxAsync() ?? 0;
                TxtBrojKalkulacije.Text = (max + 1).ToString();
            }
        }
        catch (Exception ex)
        {
            if (_existingKalkulacija != null)
            {
                MessageBox.Show($"Greška pri učitavanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Za novu kalkulaciju, predlog broja/magacina je pogodnost — nije blokirajuće ako ne uspe.
        }

        Prikazi();
    }

    private static decimal ParseUneto(string text)
    {
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return v;
        return 0m;
    }

    private Kalkulacija SkupiUnos()
    {
        return new Kalkulacija
        {
            KalkulacijaId = _existingKalkulacija?.KalkulacijaId ?? 0,
            BrojKalkulacije = int.TryParse(TxtBrojKalkulacije.Text.Trim(), out int brojKalk) ? brojKalk : 0,
            Datum = DpDatum.SelectedDate ?? DateTime.Now,
            SifraDobavljaca = TxtSifraDobavljaca.Text.Trim(),
            BrojRacuna = TxtBrojRacuna.Text.Trim(),
            BrojOtpremnice = TxtBrojOtpremnice.Text.Trim(),
            SifraMagacina = (CmbMagacin.SelectedItem as AccountingData.Models.Magacin)?.SifraMagacina,
            NabavnaVrednost = ParseUneto(TxtNabavnaVrednost.Text),
            TransportniTroskovi = ParseUneto(TxtTransportniTroskovi.Text),
            TroskoviUskladistenja = ParseUneto(TxtTroskoviUskladistenja.Text),
            UtovarIstovar = ParseUneto(TxtUtovarIstovar.Text),
            TransportnoOsiguranje = ParseUneto(TxtTransportnoOsiguranje.Text),
            OstaliTroskovi = ParseUneto(TxtOstaliTroskovi.Text),
            MarzaProcenat = ParseUneto(TxtMarzaProcenat.Text),
            PoreskaStopaProcenat = ParseUneto(TxtPoreskaStopaProcenat.Text),
            Stavke = _stavke.ToList()
        };
    }

    private void Input_Changed(object sender, TextChangedEventArgs e) => Prikazi();

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new KalkulacijaStavka { RedniBroj = _stavke.Count + 1 });
        Prikazi();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is KalkulacijaStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            Prikazi();
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        // U trenutku CellEditEnding izmena još nije commit-ovana u izvorni objekat
        // (dešava se posle ovog eventa) — Prikazi() se zato odlaže na Background
        // prioritet da vidi već ažurirane vrednosti stavke.
        Dispatcher.BeginInvoke(new Action(Prikazi), System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Ako postoje stavke, prodajna cena se računa po artiklu preko
    /// <see cref="KalkulacijaService.IzracunajSaStavkama"/> (a "Nabavna vrednost" postaje
    /// readonly zbir stavki); bez stavki, ponaša se kao ranije (header-only unos preko
    /// <see cref="KalkulacijaService.Izracunaj"/>). Reentrancy guard (_updating) je neophodan
    /// jer ova metoda programski menja TxtNabavnaVrednost.Text, što bi inače ponovo okinulo
    /// TextChanged (Input_Changed) i izazvalo beskonačnu rekurziju.
    /// </summary>
    private void Prikazi()
    {
        if (_updating) return;
        _updating = true;
        try
        {
            var k = SkupiUnos();
            if (k.Stavke.Count > 0)
            {
                KalkulacijaService.IzracunajSaStavkama(k);
                TxtNabavnaVrednost.Text = k.NabavnaVrednost.ToString("N2");
                TxtNabavnaVrednost.IsReadOnly = true;
                // Items.Refresh() baca InvalidOperationException ("'Refresh' is not allowed
                // during an AddNew or EditItem transaction") ako grid ima otvorenu (makar i
                // tuđu, npr. korisnik je već tabovao u sledeću ćeliju) edit transakciju u
                // trenutku kad ovaj (na Dispatcher odloženi) poziv izvrši — commit-ovati je
                // prvo da bi refresh bio bezbedan.
                DgStavke.CommitEdit(DataGridEditingUnit.Cell, true);
                DgStavke.CommitEdit(DataGridEditingUnit.Row, true);
                DgStavke.Items.Refresh();
            }
            else
            {
                TxtNabavnaVrednost.IsReadOnly = false;
                KalkulacijaService.Izracunaj(k);
            }

            TxtSvegaTroskovi.Text = k.SvegaTroskovi.ToString("N2");
            TxtSvegaNabavno.Text = k.SvegaNabavno.ToString("N2");
            TxtRazlika.Text = k.Razlika.ToString("N2");
            TxtPorez.Text = k.Porez.ToString("N2");
            TxtProdajnaVrednost.Text = k.ProdajnaVrednost.ToString("N2");
        }
        finally
        {
            _updating = false;
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojKalkulacije.Text.Trim(), out _))
        {
            MessageBox.Show("Unesite ispravan broj kalkulacije.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var s in _stavke)
        {
            if (string.IsNullOrWhiteSpace(s.SifraArtikla))
            {
                MessageBox.Show("Svaka stavka mora imati šifru artikla.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new KalkulacijaService(db);

            await service.SaveKalkulacijuAsync(SkupiUnos());
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

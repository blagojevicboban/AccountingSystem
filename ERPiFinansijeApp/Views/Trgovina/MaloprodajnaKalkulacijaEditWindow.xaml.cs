using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeApp.Services;
using ERPiFinansijeApp.Views.Pomoc;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Trgovina;

public partial class MaloprodajnaKalkulacijaEditWindow : Window
{
    private readonly ObservableCollection<MaloprodajnaKalkulacijaStavka> _stavke = new();
    private readonly MaloprodajnaKalkulacija? _existingKalkulacija;
    private bool _updating;

    public MaloprodajnaKalkulacijaEditWindow(MaloprodajnaKalkulacija? existingKalkulacija = null)
    {
        InitializeComponent();
        _existingKalkulacija = existingKalkulacija;
        DgStavke.ItemsSource = _stavke;
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
            CmbMagacinDaje.ItemsSource = magacini;
            CmbMagacinPrima.ItemsSource = magacini;

            // Šifarnik artikala za padajuću listu u stavkama — sortiran po šifri jer se
            // artikal bira kucanjem šifre (legacy MAT3.PRG: osvezi_art).
            ColArtikal.ItemsSource = await db.Artikli.OrderBy(a => a.SifraArtikla).ToListAsync();

            // Konto dobavljača se bira iz kontnog plana (legacy daj_konto(2), FIN2.PRG:1226).
            KontoPicker.PoveziDobavljace(CmbKontoDobavljaca, await db.Konta.ToListAsync());

            if (_existingKalkulacija != null)
            {
                Title = $"Izmena kalkulacije (maloprodaja) #{_existingKalkulacija.BrojKalkulacije}";
                TxtBrojKalkulacije.Text = _existingKalkulacija.BrojKalkulacije.ToString();
                DpDatum.SelectedDate = _existingKalkulacija.Datum;
                TxtSifraProdavnice.Text = _existingKalkulacija.SifraProdavnice.ToString();
                CmbMagacinDaje.SelectedItem = magacini.FirstOrDefault(m => m.SifraMagacina == _existingKalkulacija.SifraMagacinaDaje);
                CmbMagacinPrima.SelectedItem = magacini.FirstOrDefault(m => m.SifraMagacina == _existingKalkulacija.SifraMagacinaPrima);
                KontoPicker.PostaviKonto(CmbKontoDobavljaca, _existingKalkulacija.SifraDobavljaca);
                TxtBrojOtpremnice.Text = _existingKalkulacija.BrojOtpremnice;
                DpDatumOtpremnice.SelectedDate = _existingKalkulacija.DatumOtpremnice;
                TxtBrojRacuna.Text = _existingKalkulacija.BrojRacuna;
                DpDatumRacuna.SelectedDate = _existingKalkulacija.DatumRacuna;
                TxtTransportniTroskovi.Text = _existingKalkulacija.TransportniTroskovi.ToString("N2");
                TxtTroskoviUskladistenja.Text = _existingKalkulacija.TroskoviUskladistenja.ToString("N2");
                TxtUtovarIstovar.Text = _existingKalkulacija.UtovarIstovar.ToString("N2");
                TxtTransportnoOsiguranje.Text = _existingKalkulacija.TransportnoOsiguranje.ToString("N2");
                TxtOstaliTroskovi.Text = _existingKalkulacija.OstaliTroskovi.ToString("N2");
                TxtMarzaProcenat.Text = _existingKalkulacija.MarzaProcenat.ToString("N2");
                TxtPoreskaStopaProcenat.Text = _existingKalkulacija.PoreskaStopaProcenat.ToString("N2");
                TxtRabatPri.Text = _existingKalkulacija.RabatPri.ToString("N2");

                if (_existingKalkulacija.Stavke.Count == 0)
                {
                    TxtNabavnaVrednost.Text = _existingKalkulacija.NabavnaVrednost.ToString("N2");
                }

                foreach (var s in _existingKalkulacija.Stavke.OrderBy(s => s.RedniBroj))
                {
                    _stavke.Add(new MaloprodajnaKalkulacijaStavka
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
                if (magacini.Count > 0)
                {
                    CmbMagacinDaje.SelectedIndex = 0;
                    CmbMagacinPrima.SelectedIndex = 0;
                }

                int max = await db.MaloprodajneKalkulacije.Select(k => (int?)k.BrojKalkulacije).MaxAsync() ?? 0;
                TxtBrojKalkulacije.Text = (max + 1).ToString();
                TxtPoreskaStopaProcenat.Text = "20";
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

    private MaloprodajnaKalkulacija SkupiUnos()
    {
        return new MaloprodajnaKalkulacija
        {
            MaloprodajnaKalkulacijaId = _existingKalkulacija?.MaloprodajnaKalkulacijaId ?? 0,
            BrojKalkulacije = int.TryParse(TxtBrojKalkulacije.Text.Trim(), out int brojKalk) ? brojKalk : 0,
            Datum = DpDatum.SelectedDate ?? DateTime.Now,
            SifraProdavnice = int.TryParse(TxtSifraProdavnice.Text.Trim(), out int sifraProd) ? sifraProd : 0,
            SifraMagacinaDaje = (CmbMagacinDaje.SelectedItem as ERPiFinansijeData.Models.Magacin)?.SifraMagacina,
            SifraMagacinaPrima = (CmbMagacinPrima.SelectedItem as ERPiFinansijeData.Models.Magacin)?.SifraMagacina,
            SifraDobavljaca = KontoPicker.IzabraniKonto(CmbKontoDobavljaca),
            BrojOtpremnice = TxtBrojOtpremnice.Text.Trim(),
            DatumOtpremnice = DpDatumOtpremnice.SelectedDate,
            BrojRacuna = TxtBrojRacuna.Text.Trim(),
            DatumRacuna = DpDatumRacuna.SelectedDate,
            NabavnaVrednost = ParseUneto(TxtNabavnaVrednost.Text),
            TransportniTroskovi = ParseUneto(TxtTransportniTroskovi.Text),
            TroskoviUskladistenja = ParseUneto(TxtTroskoviUskladistenja.Text),
            UtovarIstovar = ParseUneto(TxtUtovarIstovar.Text),
            TransportnoOsiguranje = ParseUneto(TxtTransportnoOsiguranje.Text),
            OstaliTroskovi = ParseUneto(TxtOstaliTroskovi.Text),
            MarzaProcenat = ParseUneto(TxtMarzaProcenat.Text),
            PoreskaStopaProcenat = ParseUneto(TxtPoreskaStopaProcenat.Text),
            RabatPri = ParseUneto(TxtRabatPri.Text),
            IsKnjizen = _existingKalkulacija?.IsKnjizen ?? false,
            IsTrgovinskiKnjizen = _existingKalkulacija?.IsTrgovinskiKnjizen ?? false,
            Stavke = _stavke.ToList()
        };
    }

    private void Input_Changed(object sender, TextChangedEventArgs e) => Prikazi();

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new MaloprodajnaKalkulacijaStavka { RedniBroj = _stavke.Count + 1 });
        Prikazi();
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is MaloprodajnaKalkulacijaStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            Prikazi();
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(Prikazi), System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Ako postoje stavke, prodajna cena se računa po artiklu preko
    /// <see cref="MaloprodajnaKalkulacijaService.IzracunajSaStavkama"/> (a "Nabavna vrednost"
    /// postaje readonly zbir stavki); bez stavki, ponaša se kao header-only unos preko
    /// <see cref="MaloprodajnaKalkulacijaService.Izracunaj"/>. Reentrancy guard (_updating) je
    /// neophodan jer ova metoda programski menja TxtNabavnaVrednost.Text.
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
                MaloprodajnaKalkulacijaService.IzracunajSaStavkama(k);
                TxtNabavnaVrednost.Text = k.NabavnaVrednost.ToString("N2");
                TxtNabavnaVrednost.IsReadOnly = true;
                DgStavke.CommitEdit(DataGridEditingUnit.Cell, true);
                DgStavke.CommitEdit(DataGridEditingUnit.Row, true);
                DgStavke.Items.Refresh();
            }
            else
            {
                TxtNabavnaVrednost.IsReadOnly = false;
                MaloprodajnaKalkulacijaService.Izracunaj(k);
            }

            TxtSvegaTroskovi.Text = k.SvegaTroskovi.ToString("N2");
            TxtSvegaNabavno.Text = k.SvegaNabavno.ToString("N2");
            TxtRazlika.Text = k.Razlika.ToString("N2");
            TxtPorez.Text = k.Porez.ToString("N2");
            TxtRabatIznos.Text = k.RabatIznos.ToString("N2");
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
            var service = new MaloprodajnaKalkulacijaService(db);

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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
        else if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "📦 Pomoć — Kalkulacija (maloprodaja)",
            "Obračun ukalkulisane marže i PDV-a za robu koja ide u prodavnicu.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
                ("➕ Dodaj stavku", "Dodaje artikal — bira se iz šifarnika, kucanjem šifre ili naziva."),
                ("Konto dobavljača", "Bira se iz kontnog plana (grupa dobavljača), pretraga i po broju i po nazivu."),
                ("Magacin daje", "Ostavite prazno kad roba stiže od dobavljača pravo u prodavnicu — tada roba ULAZI u magacin koji prima. Popunite ga samo za prenos iz veleprodaje, kada se taj magacin razdužuje."),
            },
            "Rabat dobavljača je informativni obračun i ne umanjuje prodajnu vrednost.\n\n" +
            "Knjiženje pravi i nalog u Glavnoj knjizi: roba u prodavnici (1340) duguje po ceni SA PDV, a potražuju ukalkulisani PDV (1344), " +
            "ukalkulisana razlika u ceni (1348) i konto dobavljača (neto, svega nabavno). To je 'korak više' koji veleprodaja nema — " +
            "roba se u prodavnici vodi sa porezom, pa se porez izdvaja dok se ne ostvari promet.\n\n" +
            "Pretporez i bruto obaveza po ulaznom računu nisu deo ovog naloga. Bez konta dobavljača nalog se ne pravi (ne bi bio u ravnoteži), " +
            "ali se kalkulacija svejedno knjiži u magacin."
        ) { Owner = this }.ShowDialog();
    }
}

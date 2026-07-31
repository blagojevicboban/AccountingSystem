using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

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

            if (_existingKalkulacija != null)
            {
                Title = $"Izmena kalkulacije (maloprodaja) #{_existingKalkulacija.BrojKalkulacije}";
                TxtBrojKalkulacije.Text = _existingKalkulacija.BrojKalkulacije.ToString();
                DpDatum.SelectedDate = _existingKalkulacija.Datum;
                TxtSifraProdavnice.Text = _existingKalkulacija.SifraProdavnice.ToString();
                CmbMagacinDaje.SelectedItem = magacini.FirstOrDefault(m => m.SifraMagacina == _existingKalkulacija.SifraMagacinaDaje);
                CmbMagacinPrima.SelectedItem = magacini.FirstOrDefault(m => m.SifraMagacina == _existingKalkulacija.SifraMagacinaPrima);
                TxtSifraDobavljaca.Text = _existingKalkulacija.SifraDobavljaca;
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
            SifraMagacinaDaje = (CmbMagacinDaje.SelectedItem as AccountingData.Models.Magacin)?.SifraMagacina,
            SifraMagacinaPrima = (CmbMagacinPrima.SelectedItem as AccountingData.Models.Magacin)?.SifraMagacina,
            SifraDobavljaca = TxtSifraDobavljaca.Text.Trim(),
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
}

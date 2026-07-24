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
    public KalkulacijaEditWindow()
    {
        InitializeComponent();
        DpDatum.SelectedDate = DateTime.Now;
        // Podrazumevane vrednosti se postavljaju ovde (posle InitializeComponent), ne
        // kao XAML Text="..." literali — Text="0"/"20" bi okinulo TextChanged tokom
        // same InitializeComponent() (kada TextBox biva konstruisan), pre nego što su
        // kasnije deklarisani elementi (TxtSvegaTroskovi i dr. u "Obračun" sekciji)
        // uopšte kreirani, izazivajući NullReferenceException u Prikazi().
        TxtPoreskaStopaProcenat.Text = "20";
        LoadMagaciniIPredlogBroja();
        Prikazi();
    }

    private async void LoadMagaciniIPredlogBroja()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var kartice = new MaterijalnaKarticaService(db);

            var magacini = await kartice.GetMagaciniAsync();
            CmbMagacin.ItemsSource = magacini;
            if (magacini.Count > 0) CmbMagacin.SelectedIndex = 0;

            var brojevi = await db.Kalkulacije.Select(k => k.BrojKalkulacije).ToListAsync();
            int max = 0;
            foreach (var b in brojevi)
            {
                if (int.TryParse(b, out var v) && v > max) max = v;
            }
            TxtBrojKalkulacije.Text = (max + 1).ToString();
        }
        catch
        {
            // Predlog broja/magacina je pogodnost — nije blokirajuće ako ne uspe.
        }
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
            BrojKalkulacije = TxtBrojKalkulacije.Text.Trim(),
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
            PoreskaStopaProcenat = ParseUneto(TxtPoreskaStopaProcenat.Text)
        };
    }

    private void Input_Changed(object sender, TextChangedEventArgs e) => Prikazi();

    private void Prikazi()
    {
        var k = SkupiUnos();
        KalkulacijaService.Izracunaj(k);

        TxtSvegaTroskovi.Text = k.SvegaTroskovi.ToString("N2");
        TxtSvegaNabavno.Text = k.SvegaNabavno.ToString("N2");
        TxtRazlika.Text = k.Razlika.ToString("N2");
        TxtPorez.Text = k.Porez.ToString("N2");
        TxtProdajnaVrednost.Text = k.ProdajnaVrednost.ToString("N2");
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtBrojKalkulacije.Text))
        {
            MessageBox.Show("Unesite broj kalkulacije.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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

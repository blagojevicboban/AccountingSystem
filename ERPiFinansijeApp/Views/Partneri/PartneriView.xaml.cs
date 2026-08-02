using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Partneri;

public partial class PartneriView : UserControl
{
    private List<Partner> _sviPartneri = new();
    private Partner? _izabraniPartner;

    public PartneriView()
    {
        InitializeComponent();
        LoadPartnere();
    }

    private async void LoadPartnere()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            _sviPartneri = await service.GetPartneriAsync();
            LstPartneri.ItemsSource = _sviPartneri;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretragaPartnera_TextChanged(object sender, TextChangedEventArgs e)
    {
        string search = TxtPretragaPartnera.Text.Trim().ToLower();
        LstPartneri.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviPartneri
            : _sviPartneri.Where(p => p.SifraPartnera.ToLower().Contains(search) || p.Naziv.ToLower().Contains(search)).ToList();
    }

    private async void LstPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            return;
        }

        _izabraniPartner = partner;
        TxtNaslovPartnera.Text = partner.Naziv;
        TxtPodnaslovPartnera.Text = $"Šifra: {partner.SifraPartnera}" + (string.IsNullOrWhiteSpace(partner.Pib) ? "" : $" | PIB: {partner.Pib}");

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            var stavke = await service.GetOtvoreneStavkeAsync(partner.PartnerId);
            DgOtvoreneStavke.ItemsSource = stavke;
            TxtSaldoPartnera.Text = (stavke.Count > 0 ? stavke[^1].Saldo : 0m).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju otvorenih stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        if (TabStavke.SelectedIndex == 1)
        {
            await LoadPraveOtvoreneStavkeAsync();
        }
    }

    private async void TabStavke_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabStavke.SelectedIndex == 1 && _izabraniPartner != null)
        {
            await LoadPraveOtvoreneStavkeAsync();
        }
    }

    private async Task LoadPraveOtvoreneStavkeAsync()
    {
        if (_izabraniPartner == null) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new ZatvaranjeStavkiService(db);

            DgPraveOtvoreneStavke.ItemsSource = await service.GetOtvoreneStavkeZaPartneraAsync(_izabraniPartner.PartnerId);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju otvorenih stavki (IOS): {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnZatvoriStavke_Click(object sender, RoutedEventArgs e)
    {
        if (_izabraniPartner == null)
        {
            MessageBox.Show("Izaberite partnera.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var izabraniIds = DgPraveOtvoreneStavke.SelectedItems
            .OfType<OtvorenaStavkaRed>()
            .Select(s => s.StavkaNalogaId);

        var dijalog = new ZatvaranjeStavkiWindow(_izabraniPartner, izabraniIds) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            await LoadPraveOtvoreneStavkeAsync();
        }
    }

    private void BtnIstorijaZatvaranja_Click(object sender, RoutedEventArgs e)
    {
        if (_izabraniPartner == null)
        {
            MessageBox.Show("Izaberite partnera.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new IstorijaZatvaranjaWindow(_izabraniPartner) { Owner = Window.GetWindow(this) };
        dijalog.ShowDialog();
        if (dijalog.NestoOtkazano)
        {
            _ = LoadPraveOtvoreneStavkeAsync();
        }
    }

    private async void BtnStampajIOS_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za izvoz IOS obrasca.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);
            var stavke = await service.GetOtvoreneStavkeAsync(partner.PartnerId);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            byte[] pdfBytes = PdfReportService.GenerisiIOSPdf(firma, partner, stavke);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"IOS_{partner.SifraPartnera}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnObracunKamate_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za obračun kamate.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new KamataWindow(partner) { Owner = Window.GetWindow(this) };
        dijalog.ShowDialog();
    }

    private void BtnKursnaLista_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var win = new KursnaListaWindow { Owner = Window.GetWindow(this) };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju kursne liste: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnVerifikujRacun_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Molimo izaberite partnera sa liste za verifikaciju tekućeg računa.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        string pibIliMb = !string.IsNullOrWhiteSpace(partner.Pib) ? partner.Pib : partner.MaticniBroj ?? "";
        if (string.IsNullOrWhiteSpace(pibIliMb))
        {
            MessageBox.Show($"Partner '{partner.Naziv}' nema unet PIB ni matični broj.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var client = new NbsApiClient();
        var res = await client.ProveriTekuciRacunPartneraAsync(pibIliMb);

        if (res.Success)
        {
            string poruka = $"🏛️ NBS REGISTAR TEKUĆIH RAČUNA:\n\n" +
                            $"• Partner: {partner.Naziv}\n" +
                            $"• PIB / MB: {pibIliMb}\n" +
                            $"• Tekući račun: {res.TekuciRacun ?? partner.ZiroRacun ?? "Nije registrovan"}\n" +
                            $"• Status naloga: {res.StatusBlokade}\n\n" +
                            $"Aplikacija je verifikovala podatke u zvaničnom registru NBS.";

            MessageBox.Show(poruka, "Verifikacija tekućeg računa NBS", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"❌ {res.Message}", "Greška pri verifikaciji", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelPartneri_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgOtvoreneStavke, TxtNaslovPartnera.Text, "Partneri_Otvorene_Stavke");
}

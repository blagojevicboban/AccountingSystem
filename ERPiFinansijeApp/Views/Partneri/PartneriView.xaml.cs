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
    private bool _ucitavanjeKonta;

    public PartneriView()
    {
        InitializeComponent();
        LoadPartnere();
    }

    private async void LoadPartnere() => await OsveziPartnereAsync();

    private async Task OsveziPartnereAsync(int? selektujPartnerId = null)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            _sviPartneri = await service.GetPartneriAsync();
            PrimeniFilterPartnera();

            if (selektujPartnerId is int id)
            {
                LstPartneri.SelectedItem = _sviPartneri.FirstOrDefault(p => p.PartnerId == id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Desni klik ne selektuje stavku sam po sebi (za razliku od levog) — biramo je ručno da kontekst meni radi nad partnerom pod mišem.</summary>
    private void LstPartneri_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && ItemsControl.ContainerFromElement(LstPartneri, d) is ListBoxItem item)
        {
            item.IsSelected = true;
        }
    }

    private async void MiIzmeniPartnera_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not Partner partner)
        {
            MessageBox.Show("Izaberite partnera za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new PartnerEditWindow(partner) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true && dijalog.Sacuvan != null)
        {
            await OsveziPartnereAsync(dijalog.Sacuvan.PartnerId);
        }
    }

    private void TxtPretragaPartnera_TextChanged(object sender, TextChangedEventArgs e) => PrimeniFilterPartnera();

    private void RbFilterPartneri_Checked(object sender, RoutedEventArgs e) => PrimeniFilterPartnera();

    private void PrimeniFilterPartnera()
    {
        if (LstPartneri == null) return; // stiže i tokom InitializeComponent, pre nego što je kontrola spremna

        IEnumerable<Partner> izvor = _sviPartneri;
        if (RbPartneriKupci?.IsChecked == true)
        {
            izvor = izvor.Where(JeKontoKupca);
        }
        else if (RbPartneriDobavljaci?.IsChecked == true)
        {
            izvor = izvor.Where(JeKontoDobavljaca);
        }

        string search = TxtPretragaPartnera.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(search))
        {
            izvor = izvor.Where(p => p.SifraPartnera.ToLower().Contains(search) || p.Naziv.ToLower().Contains(search));
        }

        LstPartneri.ItemsSource = izvor.ToList();
    }

    /// <summary>Konto kupca je 204 (novi zakon) ili 120 (stari) — isti prefiksi kao KamataService.IsKupacKonto.</summary>
    private static bool JeKontoKupca(Partner p)
    {
        string konto = p.KontoPartnera ?? p.SifraPartnera;
        return konto.StartsWith(KontoPicker.Grupe.KupciNoviZakon, StringComparison.Ordinal)
            || konto.StartsWith(KontoPicker.Grupe.KupciStariZakon, StringComparison.Ordinal);
    }

    private static bool JeKontoDobavljaca(Partner p)
    {
        string konto = p.KontoPartnera ?? p.SifraPartnera;
        return konto.StartsWith(KontoPicker.Grupe.DobavljaciNoviZakon, StringComparison.Ordinal)
            || konto.StartsWith(KontoPicker.Grupe.DobavljaciStariZakon, StringComparison.Ordinal);
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

            // Partner sa PartnerId=0 je "sintetički" — legacy analitički konto (204xxx/435xxx)
            // bez veze u Partneri tabeli (vidi OtvoreneStavkeService.GetPartneriAsync). Za njega
            // postoji tačno jedan konto (partner.KontoPartnera), pa GetPartnerKontaAsync (koja
            // pretražuje po PartnerId-ju) nema smisla — kombo se puni direktno.
            List<PartnerKontoInfo> konta = partner.PartnerId > 0
                ? await service.GetPartnerKontaAsync(partner.PartnerId)
                : new List<PartnerKontoInfo> { new() { BrojKonta = partner.KontoPartnera ?? partner.SifraPartnera, NazivKonta = partner.Naziv, BrojStavki = 0 } };

            _ucitavanjeKonta = true;
            CmbKontoKartice.ItemsSource = konta;
            CmbKontoKartice.SelectedIndex = konta.Count > 0 ? 0 : -1;
            _ucitavanjeKonta = false;

            // Padajuća lista ima smisla samo kad partner vodi više konta (npr. i kupac i
            // dobavljač). Za uobičajen slučaj tačno jednog konta prikazujemo običan tekst —
            // dropdown strelica bez izbora samo zbunjuje.
            bool viseKonta = konta.Count > 1;
            CmbKontoKartice.Visibility = viseKonta ? Visibility.Visible : Visibility.Collapsed;
            TxtKontoJedini.Visibility = viseKonta ? Visibility.Collapsed : Visibility.Visible;
            TxtKontoJedini.Text = konta.Count == 1 ? konta[0].Prikaz : "—";

            if (konta.Count > 0)
            {
                await UcitajKarticuZaKontoAsync(partner, konta[0].BrojKonta);
            }
            else
            {
                DgOtvoreneStavke.ItemsSource = null;
                TxtSaldoPartnera.Text = 0m.ToString("N2");
            }

            AzurirajStanjeObracunaKamate();
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

    private async void CmbKontoKartice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ucitavanjeKonta || _izabraniPartner == null) return;
        if (CmbKontoKartice.SelectedItem is not PartnerKontoInfo konto) return;

        await UcitajKarticuZaKontoAsync(_izabraniPartner, konto.BrojKonta);
        AzurirajStanjeObracunaKamate();
    }

    /// <summary>Obračun kamate ima smisla samo na kontu kupca (204/120) — isto pravilo kao KamataService.IsKupacKonto.</summary>
    private void AzurirajStanjeObracunaKamate()
    {
        string? brojKonta = (CmbKontoKartice.SelectedItem as PartnerKontoInfo)?.BrojKonta;
        bool jeKupac = !string.IsNullOrWhiteSpace(brojKonta) &&
            (brojKonta.StartsWith(KontoPicker.Grupe.KupciNoviZakon, StringComparison.Ordinal) ||
             brojKonta.StartsWith(KontoPicker.Grupe.KupciStariZakon, StringComparison.Ordinal));

        BtnObracunKamate.IsEnabled = jeKupac;
        BtnObracunKamate.ToolTip = jeKupac
            ? "Kalkulacija i obračun zatezne kamate za dospela potraživanja"
            : "Obračun kamate je moguć samo za konto kupca (204/120)";
    }

    private async Task UcitajKarticuZaKontoAsync(Partner partner, string brojKonta)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            var stavke = partner.PartnerId > 0
                ? await service.GetOtvoreneStavkeAsync(partner.PartnerId, brojKonta)
                : await service.GetOtvoreneStavkeZaKontoAsync(brojKonta);

            DgOtvoreneStavke.ItemsSource = stavke;
            TxtSaldoPartnera.Text = (stavke.Count > 0 ? stavke[^1].Saldo : 0m).ToString("N2");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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

            DgPraveOtvoreneStavke.ItemsSource = _izabraniPartner.PartnerId > 0
                ? await service.GetOtvoreneStavkeZaPartneraAsync(_izabraniPartner.PartnerId)
                : await service.GetOtvoreneStavkeZaKontoAsync(_izabraniPartner.KontoPartnera ?? _izabraniPartner.SifraPartnera);
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

        if (_izabraniPartner.PartnerId <= 0)
        {
            MessageBox.Show("Ručno zatvaranje stavki je za sada dostupno samo za partnere iz šifarnika, ne i za legacy analitičke konte (204xxx/435xxx bez matičnog partnera).", "Nije podržano", MessageBoxButton.OK, MessageBoxImage.Information);
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

        if (_izabraniPartner.PartnerId <= 0)
        {
            MessageBox.Show("Istorija zatvaranja je za sada dostupna samo za partnere iz šifarnika, ne i za legacy analitičke konte (204xxx/435xxx bez matičnog partnera).", "Nije podržano", MessageBoxButton.OK, MessageBoxImage.Information);
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
            string? brojKonta = (CmbKontoKartice.SelectedItem as PartnerKontoInfo)?.BrojKonta;
            var stavke = partner.PartnerId > 0
                ? await service.GetOtvoreneStavkeAsync(partner.PartnerId, brojKonta)
                : await service.GetOtvoreneStavkeZaKontoAsync(brojKonta ?? partner.KontoPartnera ?? partner.SifraPartnera);
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

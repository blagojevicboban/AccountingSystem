using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class TrgovinaView : UserControl
{
    private List<Kalkulacija> _sveKalkulacije = new();
    private List<RacunOtpremnica> _sviRacuni = new();
    private List<NivelacijaCena> _sveNivelacije = new();
    private List<Artikal> _sviArtikliRobno = new();

    public TrgovinaView()
    {
        InitializeComponent();
        LoadAllData();
    }

    private void LoadAllData()
    {
        LoadKalkulacije();
        LoadRacune();
        LoadNivelacije();
        LoadMagacineIRobneKartice();
    }

    private List<MaloprodajnaKalkulacija> _sveMaloprodajneKalkulacije = new();

    private async void LoadKalkulacije()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new KalkulacijaService(db);
            _sveKalkulacije = await service.GetKalkulacijeAsync();
            _sveMaloprodajneKalkulacije = await db.MaloprodajneKalkulacije.OrderByDescending(m => m.Datum).ToListAsync();
            FiltrirajKalkulacije();
        }
        catch { }
    }

    private void CmbTipKalkulacije_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FiltrirajKalkulacije();
    }

    private void FiltrirajKalkulacije()
    {
        if (DgKalkulacije == null) return;
        var search = TxtPretraga.Text.Trim().ToLower();
        bool isVeleprodaja = CmbTipKalkulacije?.SelectedIndex != 1;

        if (isVeleprodaja)
        {
            DgKalkulacije.ItemsSource = string.IsNullOrEmpty(search)
                ? _sveKalkulacije
                : _sveKalkulacije.Where(k => k.BrojKalkulacije.ToLower().Contains(search) || (k.SifraDobavljaca != null && k.SifraDobavljaca.ToLower().Contains(search))).ToList();
        }
        else
        {
            DgKalkulacije.ItemsSource = string.IsNullOrEmpty(search)
                ? _sveMaloprodajneKalkulacije
                : _sveMaloprodajneKalkulacije.Where(k => k.BrojKalkulacije.ToLower().Contains(search)).ToList();
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajKalkulacije();

    private void BtnNovaKalkulacija_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new KalkulacijaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadKalkulacije();
    }

    private async void BtnKnjiziKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
        {
            MessageBox.Show("Molimo izaberite kalkulaciju sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show($"Kalkulacija #{selektovana.BrojKalkulacije} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new KalkulacijaService(db);
            await service.KnjiziKalkulacijuAsync(selektovana.KalkulacijaId);
            MessageBox.Show($"Kalkulacija #{selektovana.BrojKalkulacije} je uspešno proknjižena!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadKalkulacije();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnStampajKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MessageBox.Show($"Štampa kalkulacije #{selektovana.BrojKalkulacije} je poslata.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ================= RAČUNI - OTPREMNICE (FAKTURE) =================
    private async void LoadRacune()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new RacunOtpremnicaService(db);
            _sviRacuni = await service.GetRacuneAsync();
            FiltrirajRacune();
        }
        catch { }
    }

    private void FiltrirajRacune()
    {
        var search = TxtPretragaRacuna.Text.Trim().ToLower();
        DgRacuni.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviRacuni
            : _sviRacuni.Where(r => r.BrojRacuna.ToLower().Contains(search) || (r.Partner != null && r.Partner.Naziv.ToLower().Contains(search))).ToList();
    }

    private void TxtPretragaRacuna_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajRacune();

    private void BtnNoviRacun_Click(object sender, RoutedEventArgs e)
    {
        var win = new RacunOtpremnicaEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadRacune();
    }

    private async void BtnKnjiziRacun_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovani)
        {
            MessageBox.Show("Izaberite račun-otpremnicu sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovani.IsKnjizen)
        {
            MessageBox.Show($"Račun #{selektovani.BrojRacuna} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var service = new RacunOtpremnicaService(db);
            await service.KnjiziRacunAsync(selektovani.RacunOtpremnicaId);
            MessageBox.Show($"Račun #{selektovani.BrojRacuna} je uspešno proknjižen u finansijskom i robnom poslovanju!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadRacune();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajRacun_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovani)
        {
            MessageBox.Show("Izaberite račun za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var racunFull = await new RacunOtpremnicaService(db).GetRacunByIdAsync(selektovani.RacunOtpremnicaId);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL DOO", Pib = "100000000" };

            if (racunFull == null) return;

            byte[] pdf = PdfReportService.GenerisiRacunOtpremnicuPdf(firma, racunFull);
            string folder = @"C:\KNJIGE\Radni\Stampe";
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"Faktura_{racunFull.BrojRacuna}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            File.WriteAllBytes(path, pdf);

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF fakture: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ================= NIVELACIJE CENA =================
    private async void LoadNivelacije()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new NivelacijaService(db);
            _sveNivelacije = await service.GetNivelacijeAsync();
            FiltrirajNivelacije();
        }
        catch { }
    }

    private void FiltrirajNivelacije()
    {
        var search = TxtPretragaNivelacija.Text.Trim().ToLower();
        DgNivelacije.ItemsSource = string.IsNullOrEmpty(search)
            ? _sveNivelacije
            : _sveNivelacije.Where(n => n.BrojNivelacije.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaNivelacija_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajNivelacije();

    private void BtnNovaNivelacija_Click(object sender, RoutedEventArgs e)
    {
        var win = new NivelacijaEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadNivelacije();
    }

    private async void BtnKnjiziNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena selektovana)
        {
            MessageBox.Show("Izaberite nivelaciju sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show($"Nivelacija #{selektovana.BrojNivelacije} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var service = new NivelacijaService(db);
            await service.KnjiziNivelacijuAsync(selektovana.NivelacijaCenaId);
            MessageBox.Show($"Nivelacija #{selektovana.BrojNivelacije} je uspešno proknjižena!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadNivelacije();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena selektovana)
        {
            MessageBox.Show("Izaberite nivelaciju za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var nivelacijaFull = await db.NivelacijeCena
                .Include(n => n.Magacin)
                .Include(n => n.Stavke)
                    .ThenInclude(s => s.Artikal)
                .FirstOrDefaultAsync(n => n.NivelacijaCenaId == selektovana.NivelacijaCenaId);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL DOO", Pib = "100000000" };

            if (nivelacijaFull == null) return;

            byte[] pdf = PdfReportService.GenerisiNivelacijuPdf(firma, nivelacijaFull);
            string folder = @"C:\KNJIGE\Radni\Stampe";
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"Nivelacija_{nivelacijaFull.BrojNivelacije}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            File.WriteAllBytes(path, pdf);

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF zapisnika: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ================= ROBNE KARTICE =================
    private async void LoadMagacineIRobneKartice()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var magacini = await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync();
            CmbMagacinRobno.ItemsSource = magacini;
            if (magacini.Count > 0) CmbMagacinRobno.SelectedIndex = 0;

            _sviArtikliRobno = await db.Artikli.OrderBy(a => a.Naziv).ToListAsync();
            FiltrirajArtikleRobno();
        }
        catch { }
    }

    private void FiltrirajArtikleRobno()
    {
        var search = TxtPretragaArtiklaRobno.Text.Trim().ToLower();
        LstArtikliRobno.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviArtikliRobno
            : _sviArtikliRobno.Where(a => a.Naziv.ToLower().Contains(search) || a.SifraArtikla.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaArtiklaRobno_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajArtikleRobno();
    private void CmbMagacinRobno_SelectionChanged(object sender, SelectionChangedEventArgs e) => UcitajRobnuKarticu();
    private void LstArtikliRobno_SelectionChanged(object sender, SelectionChangedEventArgs e) => UcitajRobnuKarticu();

    private void UcitajRobnuKarticu()
    {
        if (LstArtikliRobno.SelectedItem is not Artikal artikal)
        {
            TxtNaslovArtiklaRobno.Text = "Izaberite artikal sa liste";
            TxtStanjeArtiklaRobno.Text = "";
            DgRobnaKartica.ItemsSource = null;
            return;
        }

        TxtNaslovArtiklaRobno.Text = $"{artikal.Naziv} (Šifra: {artikal.SifraArtikla})";
        TxtStanjeArtiklaRobno.Text = $"Jedinica mere: {artikal.JedinicaMere} | Nabavna cena: {artikal.NabavnaCena:N2} RSD | Prodajna cena: {artikal.ProdajnaCena:N2} RSD";
    }

    private void BtnStampajRobnuKarticu_Click(object sender, RoutedEventArgs e)
    {
        if (LstArtikliRobno.SelectedItem is not Artikal artikal)
        {
            MessageBox.Show("Izaberite artikal sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MessageBox.Show($"Štampa robne kartice za {artikal.Naziv} je poslata.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public class ArtikalIzbor : INotifyPropertyChanged
{
    public Artikal Artikal { get; }
    public ArtikalIzbor(Artikal artikal) => Artikal = artikal;

    public string SifraArtikla => Artikal.SifraArtikla;
    public string Naziv => Artikal.Naziv;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class TrgovinaView : UserControl
{
    private List<Kalkulacija> _sveKalkulacije = new();
    private List<RacunOtpremnica> _sviRacuni = new();
    private List<NivelacijaCena> _sveNivelacije = new();
    private List<Artikal> _sviArtikliRobno = new();
    private List<RobniBrutoBilansRed> _sviBrutoRedovi = new();

    public TrgovinaView()
    {
        InitializeComponent();
        ChkSamoSaKarticom.IsChecked = true;
        RbSviKalkulacije.IsChecked = true;
        RbSviRacuni.IsChecked = true;
        RbSviNivelacije.IsChecked = true;
        RbSviZaduzenja.IsChecked = true;
        RbSviRazduzenja.IsChecked = true;
        RbSviPrimopredajeTrg.IsChecked = true;
        LoadAllData();
    }

    private void LoadAllData()
    {
        LoadKalkulacije();
        LoadRacune();
        LoadNivelacije();
        LoadMagacineIRobneKartice();
        LoadRacunopolagace();
        LoadSifrarnikArtikala();
        LoadPoreskeTarife();
        LoadPrimopredaje();
        LoadRobniBrutoBilans();
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
        bool samoProknjizeni = RbProknjizeniKalkulacije?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniKalkulacije?.IsChecked == true;

        if (isVeleprodaja)
        {
            DgKalkulacije.ItemsSource = _sveKalkulacije.Where(k =>
                (string.IsNullOrEmpty(search) || k.BrojKalkulacije.ToString().Contains(search) || (k.SifraDobavljaca != null && k.SifraDobavljaca.ToLower().Contains(search))) &&
                (!samoProknjizeni || k.IsKnjizen) &&
                (!samoNeproknjizeni || !k.IsKnjizen)
            ).ToList();
        }
        else
        {
            DgKalkulacije.ItemsSource = _sveMaloprodajneKalkulacije.Where(k =>
                (string.IsNullOrEmpty(search) || k.BrojKalkulacije.ToString().Contains(search)) &&
                (!samoProknjizeni || k.IsKnjizen) &&
                (!samoNeproknjizeni || !k.IsKnjizen)
            ).ToList();
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajKalkulacije();
    private void Filter_Kalkulacije_Changed(object sender, RoutedEventArgs e) => FiltrirajKalkulacije();

    private void BtnNovaKalkulacija_Click(object sender, RoutedEventArgs e)
    {
        bool isVeleprodaja = CmbTipKalkulacije?.SelectedIndex != 1;
        if (isVeleprodaja)
        {
            var dijalog = new KalkulacijaEditWindow { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadKalkulacije();
        }
        else
        {
            var dijalog = new MaloprodajnaKalkulacijaEditWindow { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadKalkulacije();
        }
    }

    private void BtnIzmeniKalkulaciju_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuKalkulacije();
    private void DgKalkulacije_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuKalkulacije();
    private void DgKalkulacije_SelectionChanged(object sender, SelectionChangedEventArgs e) => PrikaziStavkeKalkulacije();

    private async void PrikaziStavkeKalkulacije()
    {
        if (DgKalkulacije.SelectedItem is MaloprodajnaKalkulacija selektovanaMp)
        {
            try
            {
                var opcijeMp = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var dbMp = new AccountingDbContext(opcijeMp);

                var punaMp = await dbMp.MaloprodajneKalkulacije
                    .Include(k => k.Stavke)
                    .FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == selektovanaMp.MaloprodajnaKalkulacijaId);

                if (punaMp != null)
                {
                    var artikliDictMp = await dbMp.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
                    foreach (var st in punaMp.Stavke)
                    {
                        if (artikliDictMp.TryGetValue(st.SifraArtikla, out var art))
                        {
                            st.NazivArtikla = art.Naziv;
                            st.JedinicaMere = art.JedinicaMere;
                        }
                    }
                    DgKalkulacijaStavke.ItemsSource = punaMp.Stavke;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri učitavanju stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }

        if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
        {
            DgKalkulacijaStavke.ItemsSource = null;
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var puna = await db.Kalkulacije
                .Include(k => k.Stavke)
                .FirstOrDefaultAsync(k => k.KalkulacijaId == selektovana.KalkulacijaId);

            if (puna != null)
            {
                var artikliDict = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
                foreach (var st in puna.Stavke)
                {
                    if (artikliDict.TryGetValue(st.SifraArtikla, out var art))
                    {
                        st.NazivArtikla = art.Naziv;
                        st.JedinicaMere = art.JedinicaMere;
                    }
                }
                DgKalkulacijaStavke.ItemsSource = puna.Stavke;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OtvoriIzmenuKalkulacije()
    {
        if (DgKalkulacije.SelectedItem is MaloprodajnaKalkulacija selektovanaMp)
        {
            if (selektovanaMp.IsKnjizen)
            {
                var odgovorMp = MessageBox.Show(
                    $"Kalkulacija #{selektovanaMp.BrojKalkulacije} je proknjižena i ne može se menjati u ovom statusu.\n\nDa li želite da je rasknjižite radi izmene?",
                    "Proknjižena kalkulacija", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (odgovorMp != MessageBoxResult.Yes) return;

                if (!AppSession.IsAdministrator)
                {
                    MessageBox.Show("Rasknjižavanje kalkulacije dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var opcijeRMp = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                    using var dbRMp = new AccountingDbContext(opcijeRMp);
                    var servisRMp = new MaloprodajnaKalkulacijaService(dbRMp);
                    await servisRMp.RasknjiziKalkulacijuAsync(selektovanaMp.MaloprodajnaKalkulacijaId);

                    LoadKalkulacije();

                    var osvezenaMp = await dbRMp.MaloprodajneKalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == selektovanaMp.MaloprodajnaKalkulacijaId);
                    if (osvezenaMp != null)
                    {
                        var dijalogRMp = new MaloprodajnaKalkulacijaEditWindow(osvezenaMp) { Owner = Window.GetWindow(this) };
                        if (dijalogRMp.ShowDialog() == true) LoadKalkulacije();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            try
            {
                var opcijeMp = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var dbMp = new AccountingDbContext(opcijeMp);
                var punaMp = await dbMp.MaloprodajneKalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == selektovanaMp.MaloprodajnaKalkulacijaId);
                if (punaMp == null)
                {
                    MessageBox.Show("Kalkulacija nije pronađena.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dijalogMp = new MaloprodajnaKalkulacijaEditWindow(punaMp) { Owner = Window.GetWindow(this) };
                if (dijalogMp.ShowDialog() == true) LoadKalkulacije();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri otvaranju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }

        if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
        {
            MessageBox.Show("Izaberite kalkulaciju sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Kalkulacija #{selektovana.BrojKalkulacije} je proknjižena i ne može se menjati u ovom statusu.\n\nDa li želite da je rasknjižite radi izmene?",
                "Proknjižena kalkulacija", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje kalkulacije dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var opcijeR = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var dbR = new AccountingDbContext(opcijeR);
                var servisR = new KalkulacijaService(dbR);
                await servisR.RasknjiziKalkulacijuAsync(selektovana.KalkulacijaId);

                LoadKalkulacije();

                var osvezena = await dbR.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == selektovana.KalkulacijaId);
                if (osvezena != null)
                {
                    var dijalogR = new KalkulacijaEditWindow(osvezena) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadKalkulacije();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var puna = await db.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == selektovana.KalkulacijaId);
            if (puna == null)
            {
                MessageBox.Show("Kalkulacija nije pronađena.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dijalog = new KalkulacijaEditWindow(puna) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadKalkulacije();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjiziKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgKalkulacije.SelectedItem is MaloprodajnaKalkulacija selektovanaMp)
        {
            if (selektovanaMp.IsKnjizen)
            {
                MessageBox.Show($"Kalkulacija #{selektovanaMp.BrojKalkulacije} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var opcijeMp = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var dbMp = new AccountingDbContext(opcijeMp);
                var servisMp = new MaloprodajnaKalkulacijaService(dbMp);
                await servisMp.KnjiziKalkulacijuAsync(selektovanaMp.MaloprodajnaKalkulacijaId);
                MessageBox.Show($"Kalkulacija #{selektovanaMp.BrojKalkulacije} je uspešno proknjižena!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadKalkulacije();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }

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

    private async void BtnStampajKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        bool isVeleprodaja = CmbTipKalkulacije?.SelectedIndex != 1;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            byte[] pdfBytes;
            string brojZaFajl;

            if (isVeleprodaja)
            {
                if (DgKalkulacije.SelectedItem is not Kalkulacija selektovana)
                {
                    MessageBox.Show("Izaberite kalkulaciju za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var puna = await db.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == selektovana.KalkulacijaId);
                if (puna == null) return;

                var artikliDict = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
                foreach (var st in puna.Stavke)
                {
                    if (artikliDict.TryGetValue(st.SifraArtikla, out var art))
                    {
                        st.NazivArtikla = art.Naziv;
                        st.JedinicaMere = art.JedinicaMere;
                    }
                }

                var dobavljac = await db.Partneri.FirstOrDefaultAsync(p => p.SifraPartnera == puna.SifraDobavljaca);
                var magacin = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == puna.SifraMagacina);

                pdfBytes = Services.PdfReportService.GenerisiKalkulacijuPdf(firma, puna, dobavljac, magacin);
                brojZaFajl = puna.BrojKalkulacije.ToString();
            }
            else
            {
                if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
                {
                    MessageBox.Show("Izaberite kalkulaciju za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var punaMp = await db.MaloprodajneKalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == selektovana.MaloprodajnaKalkulacijaId);
                if (punaMp == null) return;

                var artikliDictMp = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
                foreach (var st in punaMp.Stavke)
                {
                    if (artikliDictMp.TryGetValue(st.SifraArtikla, out var art))
                    {
                        st.NazivArtikla = art.Naziv;
                        st.JedinicaMere = art.JedinicaMere;
                    }
                }

                var dobavljac = await db.Partneri.FirstOrDefaultAsync(p => p.SifraPartnera == punaMp.SifraDobavljaca);
                var magacinDaje = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == punaMp.SifraMagacinaDaje);
                var magacinPrima = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == punaMp.SifraMagacinaPrima);

                pdfBytes = Services.PdfReportService.GenerisiMaloprodajnuKalkulacijuPdf(firma, punaMp, dobavljac, magacinDaje, magacinPrima);
                brojZaFajl = punaMp.BrojKalkulacije.ToString();
            }

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Kalkulacija_{brojZaFajl}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ================= RAČUNI - OTPREMNICE (MAT5 - rac_otpremnica) =================
    private async void LoadRacune()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new RacunOtpremnicaService(db);
            _sviRacuni = await service.GetRacuneAsync();
            foreach (var r in _sviRacuni)
            {
                if (r.Partner != null) r.KontoKupca = r.Partner.SifraPartnera;
            }
            FiltrirajRacune();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FiltrirajRacune()
    {
        if (DgRacuni == null) return;
        var search = TxtPretragaRacuna.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniRacuni?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniRacuni?.IsChecked == true;

        DgRacuni.ItemsSource = _sviRacuni.Where(r =>
            (string.IsNullOrEmpty(search) || r.BrojRacuna.ToString().Contains(search) ||
                                   (r.BrojOtpremnice != null && r.BrojOtpremnice.ToLower().Contains(search)) ||
                                   r.KontoKupca.ToLower().Contains(search)) &&
            (!samoProknjizeni || r.IsKnjizen) &&
            (!samoNeproknjizeni || !r.IsKnjizen)
        ).ToList();

        if (DgRacuni.Items.Count > 0) DgRacuni.SelectedIndex = 0;
        else DgRacunStavke.ItemsSource = null;
    }

    private void TxtPretragaRacuna_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajRacune();
    private void Filter_Racuni_Changed(object sender, RoutedEventArgs e) => FiltrirajRacune();

    private async void DgRacuni_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica racun)
        {
            DgRacunStavke.ItemsSource = null;
            BtnIzmeniRacun.IsEnabled = true;
            BtnKnjiziRacun.IsEnabled = true;
            return;
        }

        BtnIzmeniRacun.IsEnabled = !racun.IsKnjizen;
        BtnKnjiziRacun.IsEnabled = !racun.IsKnjizen;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var fullRacun = await new RacunOtpremnicaService(db).GetRacunByIdAsync(racun.RacunOtpremnicaId);
            if (fullRacun != null)
            {
                foreach (var st in fullRacun.Stavke)
                {
                    if (st.Artikal != null)
                    {
                        st.SifraArtikla = st.Artikal.SifraArtikla;
                        st.NazivArtikla = st.Artikal.Naziv;
                    }
                }
                DgRacunStavke.ItemsSource = fullRacun.Stavke;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju stavki računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnNoviRacun_Click(object sender, RoutedEventArgs e)
    {
        var win = new RacunOtpremnicaEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadRacune();
    }

    private void BtnIzmeniRacun_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuRacuna();
    private void DgRacuni_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuRacuna();

    private async void OtvoriIzmenuRacuna()
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovani)
        {
            MessageBox.Show("Izaberite račun-otpremnicu sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovani.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Račun-otpremnica #{selektovani.BrojRacuna} je proknjižena i ne može se menjati u ovom statusu.\n\nDa li želite da je rasknjižite radi izmene?",
                "Proknjižen račun", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje računa dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new RacunOtpremnicaService(db);
                await service.RasknjiziRacunAsync(selektovani.RacunOtpremnicaId);

                LoadRacune();

                var osvezeni = await service.GetRacunByIdAsync(selektovani.RacunOtpremnicaId);
                if (osvezeni != null)
                {
                    var dijalog = new RacunOtpremnicaEditWindow(osvezeni) { Owner = Window.GetWindow(this) };
                    if (dijalog.ShowDialog() == true) LoadRacune();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var win = new RacunOtpremnicaEditWindow(selektovani) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true) LoadRacune();
    }

    private async void BtnKnjiziRacun_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovani)
        {
            MessageBox.Show("Izaberite račun-otpremnicu za knjiženje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovani.IsKnjizen)
        {
            MessageBox.Show($"Račun #{selektovani.BrojRacuna} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potv = MessageBox.Show($"Da li ste sigurni da želite proknjižiti račun-otpremnicu br. {selektovani.BrojRacuna}?",
            "Potvrda knjiženja (knjiz_racotp - MAT5)", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potv == MessageBoxResult.Yes)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);

                var service = new RacunOtpremnicaService(db);
                await service.KnjiziRacunAsync(selektovani.RacunOtpremnicaId);
                MessageBox.Show($"Račun #{selektovani.BrojRacuna} je uspešno proknjižen u robnom i finansijskom poslovanju!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRacune();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri knjiženju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnMasovnoKnjizenjeRacuna_Click(object sender, RoutedEventArgs e)
    {
        var neknjizeni = _sviRacuni.Where(r => !r.IsKnjizen).ToList();
        if (neknjizeni.Count == 0)
        {
            MessageBox.Show("Nema neknjiženih računa za knjiženje.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potv = MessageBox.Show($"Pronađeno je {neknjizeni.Count} neknjiženih računa-otpremnica.\nDa li želite masovno proknjižiti sve račune? (knjiz_racotp 0 - MAT5)",
            "Masovno knjiženje", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potv == MessageBoxResult.Yes)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new RacunOtpremnicaService(db);

                int uspesno = 0;
                foreach (var r in neknjizeni)
                {
                    await service.KnjiziRacunAsync(r.RacunOtpremnicaId);
                    uspesno++;
                }

                MessageBox.Show($"Uspešno je proknjiženo {uspesno} računa-otpremnica!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRacune();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri masovnom knjiženju računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var racunFull = await new RacunOtpremnicaService(db).GetRacunByIdAsync(selektovani.RacunOtpremnicaId);
            if (racunFull == null) return;

            foreach (var st in racunFull.Stavke)
            {
                if (st.Artikal != null)
                {
                    st.SifraArtikla = st.Artikal.SifraArtikla;
                    st.NazivArtikla = st.Artikal.Naziv;
                }
            }
            if (racunFull.Partner != null) racunFull.KontoKupca = racunFull.Partner.SifraPartnera;

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL DOO", Pib = "100000000" };

            byte[] pdf = PdfReportService.GenerisiRacunOtpremnicuPdf(firma, racunFull, racunFull.Partner);
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Racun_{racunFull.BrojRacuna}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdf);

            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi računa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
            var stavkeZaCombo = new List<AccountingData.Models.Magacin> { SviMagaciniOpcija };
            stavkeZaCombo.AddRange(magacini);
            CmbMagacinRobno.ItemsSource = stavkeZaCombo;
            CmbMagacinRobno.SelectedIndex = 0;

            _sviArtikliRobno = await db.Artikli.OrderBy(a => a.Naziv).ToListAsync();
            await OsveziArtikleSaKarticomAsync();
            FiltrirajArtikleRobno();
        }
        catch { }
    }

    private HashSet<string> _artikliSaKarticom = new(StringComparer.OrdinalIgnoreCase);

    private async Task OsveziArtikleSaKarticomAsync()
    {
        if (CmbMagacinRobno.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            _artikliSaKarticom = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
        using var db = new AccountingDbContext(options);

        var upit = db.MaterijalneKartice.AsQueryable();
        if (!JeSviMagacini(magacin)) upit = upit.Where(k => k.SifraMagacina == magacin.SifraMagacina);

        var sifre = await upit.Select(k => k.SifraArtikla).Distinct().ToListAsync();
        _artikliSaKarticom = new HashSet<string>(sifre, StringComparer.OrdinalIgnoreCase);
    }

    private void FiltrirajArtikleRobno()
    {
        var search = TxtPretragaArtiklaRobno.Text.Trim().ToLower();
        IEnumerable<Artikal> izvor = _sviArtikliRobno;

        if (ChkSamoSaKarticom.IsChecked == true)
            izvor = izvor.Where(a => _artikliSaKarticom.Contains(a.SifraArtikla));

        if (!string.IsNullOrEmpty(search))
            izvor = izvor.Where(a => a.Naziv.ToLower().Contains(search) || a.SifraArtikla.ToLower().Contains(search));

        var izbori = izvor.Select(a => new ArtikalIzbor(a)).ToList();
        foreach (var izbor in izbori) izbor.PropertyChanged += ArtikalIzborRobno_PropertyChanged;
        LstArtikliRobno.ItemsSource = izbori;
        UpdateBtnStampajRobnaKarticaState();
    }

    private void ArtikalIzborRobno_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ArtikalIzbor.IsSelected))
        {
            UpdateBtnStampajRobnaKarticaState();
        }
    }

    private bool _updatingChkSviArtikliRobno;

    private void UpdateBtnStampajRobnaKarticaState()
    {
        var izbori = LstArtikliRobno.ItemsSource as List<ArtikalIzbor>;
        bool imaCekiranih = izbori?.Any(i => i.IsSelected) ?? false;
        bool imaPrikazanuKarticu = LstArtikliRobno.SelectedItem is ArtikalIzbor && _trenutnaRobnaKartica.Count > 0;
        BtnStampajRobnaKartica.IsEnabled = imaCekiranih || imaPrikazanuKarticu;

        if (ChkSviArtikliRobno == null) return;

        _updatingChkSviArtikliRobno = true;
        if (izbori == null || izbori.Count == 0)
            ChkSviArtikliRobno.IsChecked = false;
        else if (izbori.All(i => i.IsSelected))
            ChkSviArtikliRobno.IsChecked = true;
        else if (izbori.All(i => !i.IsSelected))
            ChkSviArtikliRobno.IsChecked = false;
        else
            ChkSviArtikliRobno.IsChecked = null;
        _updatingChkSviArtikliRobno = false;
    }

    private void ChkSviArtikliRobno_Checked(object sender, RoutedEventArgs e) => SetSviArtikliRobnoIzabrani(true);

    private void ChkSviArtikliRobno_Unchecked(object sender, RoutedEventArgs e) => SetSviArtikliRobnoIzabrani(false);

    private void SetSviArtikliRobnoIzabrani(bool izabrano)
    {
        if (_updatingChkSviArtikliRobno) return;
        if (LstArtikliRobno.ItemsSource is not List<ArtikalIzbor> izbori) return;

        foreach (var izbor in izbori) izbor.IsSelected = izabrano;
        UpdateBtnStampajRobnaKarticaState();
    }

    private void LstArtikliRobno_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var red = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (red?.Item is ArtikalIzbor izbor)
        {
            LstArtikliRobno.SelectedItem = izbor;
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void CtxStampajRobnuKarticu_Click(object sender, RoutedEventArgs e) => BtnStampajRobnaKartica_Click(sender, e);

    private void CtxExportExcelRobnaKartica_Click(object sender, RoutedEventArgs e) => BtnExportExcelRobnaKartica_Click(sender, e);

    private void TxtPretragaArtiklaRobno_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajArtikleRobno();
    private void ChkSamoSaKarticom_Changed(object sender, RoutedEventArgs e) => FiltrirajArtikleRobno();

    private async void CmbMagacinRobno_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await OsveziArtikleSaKarticomAsync();
        FiltrirajArtikleRobno();
        UcitajRobnuKarticu();
    }
    private void LstArtikliRobno_SelectionChanged(object sender, SelectionChangedEventArgs e) => UcitajRobnuKarticu();

    private List<MaterijalnaKartica> _trenutnaRobnaKartica = new();

    private static readonly AccountingData.Models.Magacin SviMagaciniOpcija = new()
    {
        MagacinId = -1,
        SifraMagacina = "*",
        NazivMagacina = "🏢 Svi magacini"
    };

    private static bool JeSviMagacini(AccountingData.Models.Magacin? m) => m == null || m.MagacinId == -1;

    private static string SifraZaFajl(AccountingData.Models.Magacin m) => JeSviMagacini(m) ? "SVI" : m.SifraMagacina;

    /// <summary>Skuplja (magacin, artikal, kartice) trojke sa prometom. magacinFilter=null znači svi magacini; artikliFilter=null znači svi artikli.</summary>
    private static async Task<List<(AccountingData.Models.Magacin Magacin, Artikal Artikal, List<MaterijalnaKartica> Kartice)>> PrikupiRobneKarticeAsync(
        AccountingDbContext db, AccountingData.Models.Magacin? magacinFilter, IReadOnlyCollection<Artikal>? artikliFilter)
    {
        var magaciniZaObradu = magacinFilter == null
            ? await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync()
            : new List<AccountingData.Models.Magacin> { magacinFilter };

        var sifreFiltera = artikliFilter?.Select(a => a.SifraArtikla).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var artikliDict = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
        var rezultat = new List<(AccountingData.Models.Magacin, Artikal, List<MaterijalnaKartica>)>();

        foreach (var mag in magaciniZaObradu)
        {
            var upit = db.MaterijalneKartice.Where(k => k.SifraMagacina == mag.SifraMagacina);
            if (sifreFiltera != null) upit = upit.Where(k => sifreFiltera.Contains(k.SifraArtikla));

            var sifreArtikala = await upit.Select(k => k.SifraArtikla).Distinct().ToListAsync();

            foreach (var sifra in sifreArtikala.OrderBy(s => s))
            {
                var kartice = await db.MaterijalneKartice
                    .Where(k => k.SifraMagacina == mag.SifraMagacina && k.SifraArtikla == sifra)
                    .OrderBy(k => k.DatumPromene)
                    .ThenBy(k => k.MaterijalnaKarticaId)
                    .ToListAsync();

                if (kartice.Count == 0) continue;

                var artikal = artikliDict.TryGetValue(sifra, out var art) ? art : new Artikal { SifraArtikla = sifra, Naziv = sifra };
                rezultat.Add((mag, artikal, kartice));
            }
        }

        return rezultat;
    }

    private async void UcitajRobnuKarticu()
    {
        if (CmbMagacinRobno.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            TxtNaslovArtiklaRobno.Text = "Izaberite magacin i artikal sa liste";
            TxtStanjeArtiklaRobno.Text = "";
            DgRobnaKartica.ItemsSource = null;
            _trenutnaRobnaKartica.Clear();
            PrikaziSumeRobno();
            UpdateBtnStampajRobnaKarticaState();
            return;
        }

        if (LstArtikliRobno.SelectedItem is not ArtikalIzbor izbor)
        {
            TxtNaslovArtiklaRobno.Text = "Izaberite magacin i artikal sa liste";
            TxtStanjeArtiklaRobno.Text = "";
            DgRobnaKartica.ItemsSource = null;
            _trenutnaRobnaKartica.Clear();
            PrikaziSumeRobno();
            UpdateBtnStampajRobnaKarticaState();
            return;
        }

        var artikal = izbor.Artikal;
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var upit = db.MaterijalneKartice.Where(k => k.SifraArtikla == artikal.SifraArtikla);
            if (!JeSviMagacini(magacin)) upit = upit.Where(k => k.SifraMagacina == magacin.SifraMagacina);

            _trenutnaRobnaKartica = await upit
                .OrderBy(k => k.DatumPromene)
                .ThenBy(k => k.MaterijalnaKarticaId)
                .ToListAsync();

            DgRobnaKartica.ItemsSource = _trenutnaRobnaKartica;
            PrikaziSumeRobno();

            decimal zadnjeStanje = _trenutnaRobnaKartica.LastOrDefault()?.Stanje ?? 0m;

            TxtNaslovArtiklaRobno.Text = $"{artikal.Naziv} ({artikal.SifraArtikla}) - Magacin: {magacin.NazivMagacina}";
            TxtStanjeArtiklaRobno.Text = $"Zaliha: {zadnjeStanje:N2} {artikal.JedinicaMere} | Prodajna cena: {artikal.ProdajnaCena:N2} RSD | Stavki prometa: {_trenutnaRobnaKartica.Count}";
            UpdateBtnStampajRobnaKarticaState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju robne kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrikaziSumeRobno()
    {
        TxtSumaUlazRobno.Text = _trenutnaRobnaKartica.Sum(k => k.Ulaz).ToString("N2");
        TxtSumaIzlazRobno.Text = _trenutnaRobnaKartica.Sum(k => k.Izlaz).ToString("N2");
        TxtSumaDugujeRobno.Text = _trenutnaRobnaKartica.Sum(k => k.Duguje).ToString("N2");
        TxtSumaPotrazujeRobno.Text = _trenutnaRobnaKartica.Sum(k => k.Potrazuje).ToString("N2");
        TxtSumaSaldoRobno.Text = (_trenutnaRobnaKartica.Count > 0 ? _trenutnaRobnaKartica[^1].Saldo : 0m).ToString("N2");
    }

    private async void DgRobnaKartica_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DgRobnaKartica.SelectedItem is not MaterijalnaKartica red)
        {
            return;
        }

        string opis = red.OpisPromene ?? "";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var mKalk = Regex.Match(opis, @"^Kalkulacija (\d+)$");
            if (mKalk.Success && int.TryParse(mKalk.Groups[1].Value, out int brojKalk))
            {
                var kalkulacija = await db.Kalkulacije.Include(k => k.Stavke)
                    .FirstOrDefaultAsync(k => k.BrojKalkulacije == brojKalk && k.SifraMagacina == red.SifraMagacina);
                if (kalkulacija != null)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Kalkulacija #{kalkulacija.BrojKalkulacije} od {kalkulacija.Datum:dd.MM.yyyy}");
                    sb.AppendLine($"Magacin: {red.SifraMagacina}   Proknjižena: {(kalkulacija.IsKnjizen ? "Da" : "Ne")}");
                    sb.AppendLine();
                    foreach (var s in kalkulacija.Stavke)
                    {
                        sb.AppendLine($"{s.SifraArtikla}   {s.Kolicina:N2} × {s.NabavnaCena:N2} = {s.Iznos:N2}");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"Svega nabavno: {kalkulacija.SvegaNabavno:N2} RSD   Prodajna vrednost: {kalkulacija.ProdajnaVrednost:N2} RSD");
                    MessageBox.Show(sb.ToString(), "Izvorni dokument — Kalkulacija", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            var mPP = Regex.Match(opis, @"^Primopredaja br\. (\d+)");
            if (mPP.Success && int.TryParse(mPP.Groups[1].Value, out int brojPP))
            {
                var primopredaja = await db.PrimopredajaNalozi.Include(p => p.Stavke)
                    .FirstOrDefaultAsync(p => p.BrojNaloga == brojPP &&
                        (p.SifraMagacinaDaje == red.SifraMagacina || p.SifraMagacinaPrima == red.SifraMagacina));
                if (primopredaja != null)
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"Primopredaja #{primopredaja.BrojNaloga} od {primopredaja.Datum:dd.MM.yyyy}");
                    sb.AppendLine($"Iz magacina {primopredaja.SifraMagacinaDaje} u magacin {primopredaja.SifraMagacinaPrima}");
                    sb.AppendLine($"Proknjižena: {(primopredaja.IsKnjizen ? "Da" : "Ne")}");
                    sb.AppendLine();
                    foreach (var s in primopredaja.Stavke)
                    {
                        sb.AppendLine($"{s.SifraArtikla}   {s.Kolicina:N2} × {s.Cena:N2} = {s.Iznos:N2}");
                    }
                    MessageBox.Show(sb.ToString(), "Izvorni dokument — Primopredaja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            MessageBox.Show(
                "Izvorni dokument nije pronađen za ovu stavku (verovatno je uvezena iz starih/legacy podataka).",
                "Izvorni dokument", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri traženju izvornog dokumenta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajRobnaKartica_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMagacinRobno.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin za štampu kartice.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var izbori = LstArtikliRobno.ItemsSource as List<ArtikalIzbor> ?? new();
        var izabraniArtikli = izbori.Where(i => i.IsSelected).Select(i => i.Artikal).ToList();

        if (izabraniArtikli.Count == 0 && LstArtikliRobno.SelectedItem is ArtikalIzbor trenutni)
        {
            izabraniArtikli.Add(trenutni.Artikal);
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };

            byte[] pdfBytes;
            string sifraZaNaziv;

            if (izabraniArtikli.Count == 0)
            {
                var potvrda = MessageBox.Show("Nijedan artikal nije čekiran. Da li želite da štampate kartice SVIH artikala?", "Štampa svih kartica", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (potvrda != MessageBoxResult.Yes) return;

                var sveSekcije = await PrikupiRobneKarticeAsync(db, magacinFilter: JeSviMagacini(magacin) ? null : magacin, artikliFilter: null);
                if (sveSekcije.Count == 0)
                {
                    MessageBox.Show($"Nema prometa ni na jednoj robnoj kartici{(JeSviMagacini(magacin) ? "" : $" u magacinu '{magacin.NazivMagacina}'")}.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiSveRobneKarticePdf(firma, sveSekcije);
                sifraZaNaziv = "SVI_ARTIKLI";
            }
            else if (izabraniArtikli.Count == 1 && !JeSviMagacini(magacin))
            {
                if (_trenutnaRobnaKartica.Count == 0)
                {
                    MessageBox.Show($"Nema prometa na robnoj kartici za artikal '{izabraniArtikli[0].Naziv}' u magacinu '{magacin.NazivMagacina}'.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiRobnuKarticuPdf(firma, magacin, izabraniArtikli[0], _trenutnaRobnaKartica);
                sifraZaNaziv = izabraniArtikli[0].SifraArtikla;
            }
            else
            {
                var sekcije = await PrikupiRobneKarticeAsync(db, magacinFilter: JeSviMagacini(magacin) ? null : magacin, artikliFilter: izabraniArtikli);
                if (sekcije.Count == 0)
                {
                    MessageBox.Show("Nema prometa ni na jednoj robnoj kartici za izabrane artikle.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiSveRobneKarticePdf(firma, sekcije);
                sifraZaNaziv = $"{izabraniArtikli.Count}_artikala";
            }

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Robna_Kartica_{SifraZaFajl(magacin)}_{sifraZaNaziv}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi robne kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ŠIFRARNIK RAČUNOPOLAGAČA =====================

    private List<AccountingData.Models.Magacin> _sviRacunopolagaci = new();

    private async void LoadRacunopolagace()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _sviRacunopolagaci = await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync();
            ApplyFilterRacunopol();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju računopolagača: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterRacunopol()
    {
        string search = TxtPretragaRacunopol.Text.Trim().ToLower();
        DgRacunopolagaci.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviRacunopolagaci
            : _sviRacunopolagaci.Where(m => m.SifraMagacina.ToLower().Contains(search) ||
                                            m.NazivMagacina.ToLower().Contains(search) ||
                                            (m.OdgovornoLice != null && m.OdgovornoLice.ToLower().Contains(search))).ToList();
    }

    private void TxtPretragaRacunopol_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterRacunopol();

    private void BtnNoviRacunopol_Click(object sender, RoutedEventArgs e)
    {
        var win = new Views.Magacin.MagacinEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadRacunopolagace();
            LoadMagacineIRobneKartice();
        }
    }

    private void BtnIzmeniRacunopol_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuRacunopolagaca();

    private void DgRacunopolagaci_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuRacunopolagaca();

    private void OtvoriIzmenuRacunopolagaca()
    {
        if (DgRacunopolagaci.SelectedItem is not AccountingData.Models.Magacin selektovan)
        {
            MessageBox.Show("Izaberite računopolagača sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var win = new Views.Magacin.MagacinEditWindow(selektovan) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadRacunopolagace();
            LoadMagacineIRobneKartice();
        }
    }

    private async void BtnBrisiRacunopol_Click(object sender, RoutedEventArgs e)
    {
        if (DgRacunopolagaci.SelectedItem is not AccountingData.Models.Magacin selektovan)
        {
            MessageBox.Show("Izaberite računopolagača za brisanje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            // Zaštita iz MAT1.PRG (brisanjeracunopol): provera da li postoje robne kartice ili nalozi
            bool imaKartice = await db.MaterijalneKartice.AnyAsync(mk => mk.SifraMagacina == selektovan.SifraMagacina);
            bool imaUlaze = await db.UlazNalozi.AnyAsync(u => u.SifraMagacina == selektovan.SifraMagacina);
            bool imaTrebovanja = await db.TrebovanjeNalozi.AnyAsync(t => t.SifraMagacina == selektovan.SifraMagacina);
            bool imaKalkulacije = await db.Kalkulacije.AnyAsync(k => k.SifraMagacina == selektovan.SifraMagacina);

            if (imaKartice || imaUlaze || imaTrebovanja || imaKalkulacije)
            {
                MessageBox.Show($"Računopolagač '{selektovan.NazivMagacina}' (šifra {selektovan.SifraMagacina}) ima otvorene robne kartice i promet!\n\nBrisanje računopolagača nije dozvoljeno jer postoje knjiženja u sistemu.",
                    "Zaštita brisanja (brisanjeracunopol - MAT1)", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var potv = MessageBox.Show($"Da li ste sigurni da želite trajno obrisati računopolagača '{selektovan.NazivMagacina}' (šifra {selektovan.SifraMagacina})?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potv == MessageBoxResult.Yes)
            {
                var m = await db.Magacini.FirstOrDefaultAsync(x => x.MagacinId == selektovan.MagacinId);
                if (m != null)
                {
                    db.Magacini.Remove(m);
                    await db.SaveChangesAsync();
                }
                LoadRacunopolagace();
                LoadMagacineIRobneKartice();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju računopolagača: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampaRacunopol_Click(object sender, RoutedEventArgs e)
    {
        if (_sviRacunopolagaci.Count == 0)
        {
            MessageBox.Show("Nema računopolagača za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiSifrarnikRacunopolagacaPdf(firma, _sviRacunopolagaci);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Sifrarnik_Racunopolagaca_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi šifarnika: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ŠIFRARNIK ARTIKALA (MAT2) =====================

    private List<Artikal> _sviArtikliSifrarnik = new();

    private async void LoadSifrarnikArtikala()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _sviArtikliSifrarnik = await db.Artikli.OrderBy(a => a.SifraArtikla).ToListAsync();
            ApplyFilterSifrarnikArtikala();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju artikala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterSifrarnikArtikala()
    {
        string search = TxtPretragaSifrarnikArtikala.Text.Trim().ToLower();
        DgSifrarnikArtikala.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviArtikliSifrarnik
            : _sviArtikliSifrarnik.Where(a => a.SifraArtikla.ToLower().Contains(search) ||
                                              a.Naziv.ToLower().Contains(search) ||
                                              (a.TarifniBroj != null && a.TarifniBroj.ToLower().Contains(search))).ToList();
    }

    private void TxtPretragaSifrarnikArtikala_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterSifrarnikArtikala();

    private void BtnNoviArtikal_Click(object sender, RoutedEventArgs e)
    {
        var win = new ArtikalEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadSifrarnikArtikala();
            LoadMagacineIRobneKartice();
        }
    }

    private void BtnIzmeniArtikal_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuArtikla();
    private void DgSifrarnikArtikala_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuArtikla();

    private void OtvoriIzmenuArtikla()
    {
        if (DgSifrarnikArtikala.SelectedItem is not Artikal selektovan)
        {
            MessageBox.Show("Izaberite artikal sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var win = new ArtikalEditWindow(selektovan) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadSifrarnikArtikala();
            LoadMagacineIRobneKartice();
        }
    }

    private async void BtnBrisiArtikal_Click(object sender, RoutedEventArgs e)
    {
        if (DgSifrarnikArtikala.SelectedItem is not Artikal selektovan)
        {
            MessageBox.Show("Izaberite artikal za brisanje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            // Provera otvorenih robnih kartica i stavki kalkulacija (brisanjeartikala - MAT1.PRG)
            bool imaKartice = await db.MaterijalneKartice.AnyAsync(mk => mk.SifraArtikla == selektovan.SifraArtikla);
            bool imaKalkStavke = await db.Kalkulacije.AnyAsync(k => k.Stavke.Any(ks => ks.SifraArtikla == selektovan.SifraArtikla));

            if (imaKartice || imaKalkStavke)
            {
                MessageBox.Show($"Artikal '{selektovan.Naziv}' (šifra {selektovan.SifraArtikla}) ima otvorene robne kartice i promet!\n\nBrisanje artikla nije dozvoljeno jer postoje knjiženja u sistemu.",
                    "Zaštita brisanja (brisanjeartikala - MAT1)", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var potv = MessageBox.Show($"Da li ste sigurni da želite trajno obrisati artikal '{selektovan.Naziv}' (šifra {selektovan.SifraArtikla})?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potv == MessageBoxResult.Yes)
            {
                var a = await db.Artikli.FirstOrDefaultAsync(x => x.ArtikalId == selektovan.ArtikalId);
                if (a != null)
                {
                    db.Artikli.Remove(a);
                    await db.SaveChangesAsync();
                }
                LoadSifrarnikArtikala();
                LoadMagacineIRobneKartice();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju artikla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampaSifrarnikaArtikala_Click(object sender, RoutedEventArgs e)
    {
        if (_sviArtikliSifrarnik.Count == 0)
        {
            MessageBox.Show("Nema artikala za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiSifrarnikArtikalaPdf(firma, _sviArtikliSifrarnik);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Sifrarnik_Artikala_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi šifarnika artikala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== PORESKE TARIFE (MAT6 - tarifni brojevi) =====================

    private List<PoreskaTarifa> _svePoreskeTarife = new();

    private async void LoadPoreskeTarife()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var tarife = await db.PoreskeTarife.ToListAsync();
            _svePoreskeTarife = tarife.OrderBy(t => int.Parse(t.TarifniBroj)).ToList();
            ApplyFilterPoreskeTarife();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju poreskih tarifa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterPoreskeTarife()
    {
        string search = TxtPretragaPoreskeTarife.Text.Trim().ToLower();
        DgPoreskeTarife.ItemsSource = string.IsNullOrEmpty(search)
            ? _svePoreskeTarife
            : _svePoreskeTarife.Where(t => t.TarifniBroj.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaPoreskeTarife_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPoreskeTarife();

    private void BtnNovaPoreskaTarifa_Click(object sender, RoutedEventArgs e)
    {
        var win = new PoreskaTarifaEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadPoreskeTarife();
        }
    }

    private void BtnIzmeniPoreskaTarifa_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuPoreskaTarifa();
    private void DgPoreskeTarife_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuPoreskaTarifa();

    private void OtvoriIzmenuPoreskaTarifa()
    {
        if (DgPoreskeTarife.SelectedItem is not PoreskaTarifa selektovana)
        {
            MessageBox.Show("Izaberite poresku tarifu sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var win = new PoreskaTarifaEditWindow(selektovana) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadPoreskeTarife();
        }
    }

    private async void BtnBrisiPoreskaTarifa_Click(object sender, RoutedEventArgs e)
    {
        var selektovane = DgPoreskeTarife.SelectedItems.Cast<PoreskaTarifa>().ToList();
        if (selektovane.Count == 0)
        {
            MessageBox.Show("Izaberite poresku tarifu (ili više njih) za brisanje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            // Zaštita brisanja identična legacy brisanjetarifa() (MAT6.PRG) - ne dozvoljava
            // brisanje tarife dok postoji artikal koji je koristi. Proverava se svaka
            // izabrana tarifa pojedinačno, kao u legacy sistemu.
            var zabranjene = new List<string>();
            var zaBrisanje = new List<PoreskaTarifa>();

            foreach (var t in selektovane)
            {
                bool koristiSeNaArtiklu = await db.Artikli.AnyAsync(a => a.TarifniBroj == t.TarifniBroj);
                if (koristiSeNaArtiklu) zabranjene.Add(t.TarifniBroj);
                else zaBrisanje.Add(t);
            }

            if (zabranjene.Count > 0)
            {
                MessageBox.Show($"Sledeće tarife imaju povezane artikle pa nije dozvoljeno njihovo brisanje: {string.Join(", ", zabranjene)}.",
                    "Zaštita brisanja (brisanjetarifa - MAT6)", MessageBoxButton.OK, MessageBoxImage.Stop);
            }

            if (zaBrisanje.Count == 0) return;

            string poruka = zaBrisanje.Count == 1
                ? $"Da li ste sigurni da želite trajno obrisati poresku tarifu broj '{zaBrisanje[0].TarifniBroj}'?"
                : $"Da li ste sigurni da želite trajno obrisati {zaBrisanje.Count} izabranih poreskih tarifa ({string.Join(", ", zaBrisanje.Select(t => t.TarifniBroj))})?";

            var potv = MessageBox.Show(poruka, "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potv == MessageBoxResult.Yes)
            {
                foreach (var t in zaBrisanje)
                {
                    var entitet = await db.PoreskeTarife.FirstOrDefaultAsync(x => x.PoreskaTarifaId == t.PoreskaTarifaId);
                    if (entitet != null) db.PoreskeTarife.Remove(entitet);
                }
                await db.SaveChangesAsync();
                LoadPoreskeTarife();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju poreskih tarifa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajPoreskeTarife_Click(object sender, RoutedEventArgs e)
    {
        if (_svePoreskeTarife.Count == 0)
        {
            MessageBox.Show("Nema poreskih tarifa za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiSifrarnikPoreskihTarifaPdf(firma, _svePoreskeTarife);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Sifrarnik_Poreskih_Tarifa_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi šifarnika poreskih tarifa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ZADUŽENJA / RAZDUŽENJA / PRIMOPREDAJE ROBE (MAT4) =====================
    // U legacy sistemu ovo su tri odvojena dokumenta (ZADUZ.DBF, RAZDUZ.DBF, MAT_NAL.DBF), sve tri
    // se čuvaju u istoj PrimopredajaNalog tabeli, razlikovane preko VrstaDokumenta ("Zaduženje" /
    // "Razduženje" / "Primopredaja"). Otuda tri taba nad istim učitanim spiskom (_svePrimopredaje),
    // svaki filtriran na svoju vrstu preko deljenih parametrizovanih metoda ispod.

    private List<PrimopredajaNalog> _svePrimopredaje = new();

    private async void LoadPrimopredaje()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            _svePrimopredaje = await service.GetPrimopredajeAsync();
            ApplyFilterZaduzenja();
            ApplyFilterRazduzenja();
            ApplyFilterPrimopredaje();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju primopredaja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterZaduzenja() => FiltrirajPrimopredajeTab("Zaduženje", DgZaduzenja, DgZaduzenjaStavke, TxtPretragaZaduzenja, RbProknjizeniZaduzenja, RbNeproknjizeniZaduzenja);
    private void ApplyFilterRazduzenja() => FiltrirajPrimopredajeTab("Razduženje", DgRazduzenja, DgRazduzenjaStavke, TxtPretragaRazduzenja, RbProknjizeniRazduzenja, RbNeproknjizeniRazduzenja);
    private void ApplyFilterPrimopredaje() => FiltrirajPrimopredajeTab("Primopredaja", DgPrimopredaje, DgPrimopredajaStavke, TxtPretragaPrimopredaja, RbProknjizeniPrimopredajeTrg, RbNeproknjizeniPrimopredajeTrg);

    private void FiltrirajPrimopredajeTab(string vrsta, DataGrid dgNalozi, DataGrid dgStavke, TextBox txtPretraga, RadioButton rbProknjizeni, RadioButton rbNeproknjizeni)
    {
        if (dgNalozi == null) return;

        string search = txtPretraga.Text.Trim().ToLower();
        bool samoProknjizeni = rbProknjizeni?.IsChecked == true;
        bool samoNeproknjizeni = rbNeproknjizeni?.IsChecked == true;

        IEnumerable<PrimopredajaNalog> izvor = _svePrimopredaje.Where(p => string.Equals(p.VrstaDokumenta, vrsta, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(search))
        {
            izvor = izvor.Where(p => p.BrojNaloga.ToString().Contains(search) ||
                                      p.SifraMagacinaDaje.ToLower().Contains(search) ||
                                      p.SifraMagacinaPrima.ToLower().Contains(search));
        }
        if (samoProknjizeni) izvor = izvor.Where(p => p.IsKnjizen);
        if (samoNeproknjizeni) izvor = izvor.Where(p => !p.IsKnjizen);

        dgNalozi.ItemsSource = izvor.ToList();

        if (dgNalozi.Items.Count > 0) dgNalozi.SelectedIndex = 0;
        else dgStavke.ItemsSource = null;
    }

    private void TxtPretragaZaduzenja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterZaduzenja();
    private void TxtPretragaRazduzenja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterRazduzenja();
    private void TxtPretragaPrimopredaja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPrimopredaje();
    private void Filter_Zaduzenja_Changed(object sender, RoutedEventArgs e) => ApplyFilterZaduzenja();
    private void Filter_Razduzenja_Changed(object sender, RoutedEventArgs e) => ApplyFilterRazduzenja();
    private void Filter_PrimopredajeTrg_Changed(object sender, RoutedEventArgs e) => ApplyFilterPrimopredaje();

    private void DgZaduzenja_SelectionChanged(object sender, SelectionChangedEventArgs e) => PrikaziStavkePrimopredaje(DgZaduzenja, DgZaduzenjaStavke);
    private void DgRazduzenja_SelectionChanged(object sender, SelectionChangedEventArgs e) => PrikaziStavkePrimopredaje(DgRazduzenja, DgRazduzenjaStavke);
    private void DgPrimopredaje_SelectionChanged(object sender, SelectionChangedEventArgs e) => PrikaziStavkePrimopredaje(DgPrimopredaje, DgPrimopredajaStavke);

    private async void PrikaziStavkePrimopredaje(DataGrid dgNalozi, DataGrid dgStavke)
    {
        if (dgNalozi.SelectedItem is not PrimopredajaNalog nalog)
        {
            dgStavke.ItemsSource = null;
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var fullNalog = await db.PrimopredajaNalozi
                .Include(p => p.Stavke)
                .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == nalog.PrimopredajaNalogId);

            if (fullNalog != null)
            {
                var artikliDict = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
                foreach (var st in fullNalog.Stavke)
                {
                    if (artikliDict.TryGetValue(st.SifraArtikla, out var art))
                    {
                        st.NazivArtikla = art.Naziv;
                        st.JedinicaMere = art.JedinicaMere;
                    }
                }
                dgStavke.ItemsSource = fullNalog.Stavke;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnNovoZaduzenje_Click(object sender, RoutedEventArgs e) => NovaPrimopredaja("Zaduženje");
    private void BtnNovoRazduzenje_Click(object sender, RoutedEventArgs e) => NovaPrimopredaja("Razduženje");
    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e) => NovaPrimopredaja("Primopredaja");

    private void NovaPrimopredaja(string vrsta)
    {
        var win = new PrimopredajaEditWindow(vrstaZaNovu: vrsta) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadPrimopredaje();
            LoadMagacineIRobneKartice();
        }
    }

    private void BtnIzmeniZaduzenje_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuPrimopredaje(DgZaduzenja);
    private void DgZaduzenja_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuPrimopredaje(DgZaduzenja);
    private void BtnIzmeniRazduzenje_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuPrimopredaje(DgRazduzenja);
    private void DgRazduzenja_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuPrimopredaje(DgRazduzenja);
    private void BtnIzmeniPrimopredaju_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuPrimopredaje(DgPrimopredaje);
    private void DgPrimopredaje_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuPrimopredaje(DgPrimopredaje);

    private async void OtvoriIzmenuPrimopredaje(DataGrid dgNalozi)
    {
        if (dgNalozi.SelectedItem is not PrimopredajaNalog nalog)
        {
            MessageBox.Show("Izaberite nalog sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (nalog.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Nalog #{nalog.BrojNaloga} je proknjižen i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                "Proknjižen nalog", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new PrimopredajaService(db);
                await service.RasknjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);

                LoadPrimopredaje();
                LoadMagacineIRobneKartice();

                var osvezeni = _svePrimopredaje.FirstOrDefault(p => p.PrimopredajaNalogId == nalog.PrimopredajaNalogId);
                if (osvezeni != null)
                {
                    var dijalog = new PrimopredajaEditWindow(osvezeni) { Owner = Window.GetWindow(this) };
                    if (dijalog.ShowDialog() == true)
                    {
                        LoadPrimopredaje();
                        LoadMagacineIRobneKartice();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var win = new PrimopredajaEditWindow(nalog) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadPrimopredaje();
            LoadMagacineIRobneKartice();
        }
    }

    private void BtnKnjiziZaduzenje_Click(object sender, RoutedEventArgs e) => KnjiziPrimopredaju(DgZaduzenja);
    private void BtnKnjiziRazduzenje_Click(object sender, RoutedEventArgs e) => KnjiziPrimopredaju(DgRazduzenja);
    private void BtnKnjiziPrimopredaju_Click(object sender, RoutedEventArgs e) => KnjiziPrimopredaju(DgPrimopredaje);

    private async void KnjiziPrimopredaju(DataGrid dgNalozi)
    {
        if (dgNalozi.SelectedItem is not PrimopredajaNalog nalog)
        {
            MessageBox.Show("Izaberite nalog za knjiženje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (nalog.IsKnjizen)
        {
            MessageBox.Show("Izabrani nalog je već proknjižen.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potv = MessageBox.Show($"Da li ste sigurni da želite proknjižiti nalog br. {nalog.BrojNaloga}?",
            "Potvrda knjiženja (knjiz_m_naloga - MAT4)", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potv == MessageBoxResult.Yes)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new PrimopredajaService(db);

                await service.KnjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);
                MessageBox.Show($"Nalog #{nalog.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPrimopredaje();
                LoadMagacineIRobneKartice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri knjiženju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnMasovnoKnjizenjeZaduzenja_Click(object sender, RoutedEventArgs e) => MasovnoKnjizenjePrimopredaja("Zaduženje");
    private void BtnMasovnoKnjizenjeRazduzenja_Click(object sender, RoutedEventArgs e) => MasovnoKnjizenjePrimopredaja("Razduženje");
    private void BtnMasovnoKnjizenjePrimopredaja_Click(object sender, RoutedEventArgs e) => MasovnoKnjizenjePrimopredaja("Primopredaja");

    private async void MasovnoKnjizenjePrimopredaja(string vrsta)
    {
        var neknjizeni = _svePrimopredaje.Where(p => !p.IsKnjizen && string.Equals(p.VrstaDokumenta, vrsta, StringComparison.OrdinalIgnoreCase)).ToList();
        if (neknjizeni.Count == 0)
        {
            MessageBox.Show("Nema neknjiženih naloga za knjiženje.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potv = MessageBox.Show($"Pronađeno je {neknjizeni.Count} neknjiženih naloga.\nDa li želite masovno proknjižiti sve naloge? (knjiz_m_naloga 0 - MAT4)",
            "Masovno knjiženje", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potv == MessageBoxResult.Yes)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new PrimopredajaService(db);

                int uspesno = 0;
                foreach (var nalog in neknjizeni)
                {
                    await service.KnjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);
                    uspesno++;
                }

                MessageBox.Show($"Uspešno je proknjiženo {uspesno} naloga u robnom knjigovodstvu!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPrimopredaje();
                LoadMagacineIRobneKartice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri masovnom knjiženju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnStampajZaduzenje_Click(object sender, RoutedEventArgs e) => StampajPrimopredaju(DgZaduzenja);
    private void BtnStampajRazduzenje_Click(object sender, RoutedEventArgs e) => StampajPrimopredaju(DgRazduzenja);
    private void BtnStampajPrimopredaju_Click(object sender, RoutedEventArgs e) => StampajPrimopredaju(DgPrimopredaje);

    private async void StampajPrimopredaju(DataGrid dgNalozi)
    {
        if (dgNalozi.SelectedItem is not PrimopredajaNalog nalog)
        {
            MessageBox.Show("Izaberite nalog za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var fullNalog = await db.PrimopredajaNalozi
                .Include(p => p.Stavke)
                .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == nalog.PrimopredajaNalogId);

            if (fullNalog == null) return;

            var artikliDict = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a);
            foreach (var st in fullNalog.Stavke)
            {
                if (artikliDict.TryGetValue(st.SifraArtikla, out var art))
                {
                    st.NazivArtikla = art.Naziv;
                    st.JedinicaMere = art.JedinicaMere;
                }
            }

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var magDaje = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == fullNalog.SifraMagacinaDaje) ?? new AccountingData.Models.Magacin { SifraMagacina = fullNalog.SifraMagacinaDaje, NazivMagacina = fullNalog.SifraMagacinaDaje };
            var magPrima = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == fullNalog.SifraMagacinaPrima) ?? new AccountingData.Models.Magacin { SifraMagacina = fullNalog.SifraMagacinaPrima, NazivMagacina = fullNalog.SifraMagacinaPrima };

            var pdfBytes = Services.PdfReportService.GenerisiPrimopredajuPdf(firma, fullNalog, magDaje, magPrima);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Primopredaja_{fullNalog.BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== NIVELACIJE CENA (MAT6) =====================

    private async void LoadNivelacije()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            _sveNivelacije = await NivelacijaService.GetNivelacijeAsync(db);
            FiltrirajNivelacije();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju nivelacija cena: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FiltrirajNivelacije()
    {
        if (DgNivelacije == null) return;
        var search = TxtPretragaNivelacija?.Text.Trim().ToLower() ?? "";
        bool samoProknjizeni = RbProknjizeniNivelacije?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniNivelacije?.IsChecked == true;

        var filtrirane = _sveNivelacije.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNivelacije.ToString().Contains(search) || (n.Opis != null && n.Opis.ToLower().Contains(search))) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();

        DgNivelacije.ItemsSource = filtrirane;
        if (filtrirane.Count > 0)
        {
            DgNivelacije.SelectedIndex = 0;
        }
        else
        {
            DgNivelacijaStavke.ItemsSource = null;
        }
    }

    private void TxtPretragaNivelacija_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajNivelacije();
    private void Filter_Nivelacije_Changed(object sender, RoutedEventArgs e) => FiltrirajNivelacije();

    private void DgNivelacije_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is NivelacijaCena selektovana)
        {
            DgNivelacijaStavke.ItemsSource = selektovana.Stavke;
        }
        else
        {
            DgNivelacijaStavke.ItemsSource = null;
        }
    }

    private void DgNivelacije_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BtnIzmeniNivelaciju_Click(sender, e);
    }

    private void BtnNovaNivelacija_Click(object sender, RoutedEventArgs e)
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
        using var db = new AccountingDbContext(options);

        var dijalog = new NivelacijaEditWindow(db) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            _ = NivelacijaService.SaveNivelacijaAsync(db, dijalog.Nivelacija);
            LoadNivelacije();
        }
    }

    private async void BtnIzmeniNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena niv)
        {
            MessageBox.Show("Izaberite nivelaciju cenu za izmenu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (niv.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Nivelacija #{niv.BrojNivelacije} je proknjižena i ne može se menjati u ovom statusu.\n\nDa li želite da je rasknjižite radi izmene?",
                "Proknjižena nivelacija", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje nivelacije dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var opcijeR = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var dbR = new AccountingDbContext(opcijeR);
                await NivelacijaService.RasknjiziNivelacijuAsync(dbR, niv.NivelacijaCenaId);

                LoadNivelacije();
                LoadMagacineIRobneKartice();

                var osvezena = await NivelacijaService.GetNivelacijaByIdAsync(dbR, niv.NivelacijaCenaId);
                if (osvezena != null)
                {
                    var dijalogR = new NivelacijaEditWindow(dbR, osvezena) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true)
                    {
                        await NivelacijaService.SaveNivelacijaAsync(dbR, dijalogR.Nivelacija);
                        LoadNivelacije();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
        using var db = new AccountingDbContext(options);

        var fullNiv = await NivelacijaService.GetNivelacijaByIdAsync(db, niv.NivelacijaCenaId);
        if (fullNiv == null) return;

        var dijalog = new NivelacijaEditWindow(db, fullNiv) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            await NivelacijaService.SaveNivelacijaAsync(db, dijalog.Nivelacija);
            LoadNivelacije();
        }
    }

    private async void BtnKnjiziNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena selektovana)
        {
            MessageBox.Show("Izaberite nivelaciju cena za knjiženje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovana.IsKnjizen)
        {
            MessageBox.Show("Izabrana nivelacija je već proknjižena.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var res = MessageBox.Show($"Da li ste sigurni da želite da proknjižite nivelaciju br. {selektovana.BrojNivelacije}?", "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            bool ok = await NivelacijaService.KnjiziNivelacijuAsync(db, selektovana.NivelacijaCenaId);
            if (ok)
            {
                MessageBox.Show($"Uspešno proknjižena nivelacija br. {selektovana.BrojNivelacije} i ažurirane cene artikala!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadNivelacije();
            }
        }
    }

    private async void BtnMasovnoKnjizenjeNivelacija_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show("Da li ste sigurni da želite da proknjižite sve neproknjižene nivelacije cena?", "Masovno knjiženje", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            int broj = await NivelacijaService.MasovnoKnjizenjeNivelacijaAsync(db);
            MessageBox.Show($"Uspešno proknjiženo {broj} nivelacija cena!", "Masovno knjiženje", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadNivelacije();
        }
    }

    private async void BtnStampajNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        if (DgNivelacije.SelectedItem is not NivelacijaCena niv)
        {
            MessageBox.Show("Izaberite nivelaciju cena za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var fullNiv = await NivelacijaService.GetNivelacijaByIdAsync(db, niv.NivelacijaCenaId);
            if (fullNiv == null) return;

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiZapisnikONivelacijiPdf(fullNiv, firma);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Zapisnik_Nivelacija_{fullNiv.BrojNivelacije}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi Zapisnika o nivelaciji cena: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnGenerisiNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var magacini = await db.Magacini.ToListAsync();
            if (magacini.Count == 0)
            {
                MessageBox.Show("Nema registrovanih magacina.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int prvMagId = magacini[0].MagacinId;
            var niv = await NivelacijaService.SvodjenjeNaProdajnuVrednostAsync(db, prvMagId, DateTime.Now);
            if (niv != null)
            {
                MessageBox.Show($"Uspešno generisan automatski Zapisnik o nivelaciji br. {niv.BrojNivelacije} sa {niv.Stavke.Count} stavki i ukupnom razlikom {niv.UkupnoRazlika:N2} RSD!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadNivelacije();
            }
            else
            {
                MessageBox.Show("Nema artikala sa razlikom u ceni na zalihama za generisanje nivelacije.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri automatskom generisanju nivelacije cena: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ROBNI BRUTO BILANS (MAT6) =====================

    private async void LoadRobniBrutoBilans()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            if (CmbMagacinBruto != null && CmbMagacinBruto.ItemsSource == null)
            {
                var magacini = await db.Magacini.ToListAsync();
                magacini.Insert(0, new AccountingData.Models.Magacin { MagacinId = 0, SifraMagacina = "SVI", NazivMagacina = "--- Svi magacini ---" });
                CmbMagacinBruto.ItemsSource = magacini;
                CmbMagacinBruto.SelectedIndex = 0;
            }

            int? magId = (CmbMagacinBruto?.SelectedValue is int idVal && idVal > 0) ? idVal : null;
            DateTime? doDatuma = DpDoDatumaBruto?.SelectedDate;
            string? pretraga = TxtPretragaBruto?.Text.Trim();

            _sviBrutoRedovi = await RobniBrutoBilansService.GetRobniBrutoBilansAsync(db, magId, doDatuma, pretraga);
            DgRobniBrutoBilans.ItemsSource = _sviBrutoRedovi;

            decimal ukDug = _sviBrutoRedovi.Sum(r => r.UlazVrednost);
            decimal ukPot = _sviBrutoRedovi.Sum(r => r.IzlazVrednost);
            decimal ukSal = _sviBrutoRedovi.Sum(r => r.SaldoVrednosni);

            TxtUkupnoDugujeBruto.Text = $"Ukupno Duguje: {ukDug:N2} RSD";
            TxtUkupnoPotrazujeBruto.Text = $"Ukupno Potražuje: {ukPot:N2} RSD";
            TxtUkupnoSaldoBruto.Text = $"Saldo Zaliha: {ukSal:N2} RSD";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri računanju Robnog Bruto bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbMagacinBruto_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadRobniBrutoBilans();

    private void DpDoDatumaBruto_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadRobniBrutoBilans();

    private void TxtPretragaBruto_TextChanged(object sender, TextChangedEventArgs e) => LoadRobniBrutoBilans();

    private void BtnOsveziBruto_Click(object sender, RoutedEventArgs e) => LoadRobniBrutoBilans();

    private async void BtnStampajRobniBruto_Click(object sender, RoutedEventArgs e)
    {
        if (_sviBrutoRedovi.Count == 0)
        {
            MessageBox.Show("Nema podataka u Robnom Bruto bilansu za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            DateTime? doDatuma = DpDoDatumaBruto?.SelectedDate;

            var pdfBytes = Services.PdfReportService.GenerisiRobniBrutoBilansPdf(firma, _sviBrutoRedovi, doDatuma);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Robni_Bruto_Bilans_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi Robnog Bruto bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Raspored artikala - analitika (MAT1.PRG:mat91): za svaki artikal, stanje/cena/vrednost po magacinu na zadati datum.</summary>
    private async void BtnStampajRasporedArtikala_Click(object sender, RoutedEventArgs e)
    {
        if (_sviBrutoRedovi.Count == 0)
        {
            MessageBox.Show("Nema podataka za raspored artikala.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            DateTime? doDatuma = DpDoDatumaBruto?.SelectedDate;

            var pdfBytes = Services.PdfReportService.GenerisiRasporedArtikalaPdf(firma, _sviBrutoRedovi, doDatuma);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Raspored_Artikala_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi rasporeda artikala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Stanje po artiklima - sintetika (MAT1.PRG:mat92): uvek sumira preko SVIH magacina (bez obzira na filter magacina na ekranu), do zadatog datuma.</summary>
    private async void BtnStampajStanjePoArtiklima_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            DateTime? doDatuma = DpDoDatumaBruto?.SelectedDate;
            var sviRedovi = await RobniBrutoBilansService.GetRobniBrutoBilansAsync(db, magacinId: null, doDatuma: doDatuma, pretraga: null);

            if (sviRedovi.Count == 0)
            {
                MessageBox.Show("Nema podataka za stanje po artiklima.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiStanjePoArtiklimaPdf(firma, sviRedovi, doDatuma);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Stanje_Po_Artiklima_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi stanja po artiklima: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== EXCEL EXPORT DUGMIĆI =====================

    private void BtnExportExcelRacunopol_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgRacunopolagaci, "Šifrarnik računopolagača", "Sifrarnik_Racunopolagaca");

    private void BtnExportExcelArtikli_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgSifrarnikArtikala, "Šifrarnik artikala", "Sifrarnik_Artikala");

    private void BtnExportExcelTarife_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgPoreskeTarife, "Poreske tarife", "Poreske_Tarife");

    private void BtnExportExcelZaduzenja_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgZaduzenja, "Zaduženja", "Zaduzenja");

    private void BtnExportExcelRazduzenja_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgRazduzenja, "Razduženja", "Razduzenja");

    private void BtnExportExcelPrimopredajeTrg_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgPrimopredaje, "Primopredaje robe", "Primopredaje_Robe");

    private void BtnExportExcelKalkulacije_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgKalkulacije, "Kalkulacije", "Kalkulacije");

    private void BtnExportExcelRacuni_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgRacuni, "Računi - Otpremnice", "Racuni_Otpremnice");

    private void BtnExportExcelNivelacije_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgNivelacije, "Nivelacije cena", "Nivelacije_Cena");

    private void BtnExportExcelRobnaKartica_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgRobnaKartica, TxtNaslovArtiklaRobno.Text, "Robna_Kartica");

    private void BtnExportExcelRobniBruto_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgRobniBrutoBilans, "Robni Bruto bilans", "Robni_Bruto_Bilans");
}

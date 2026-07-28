using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    private List<RobniBrutoBilansRed> _sviBrutoRedovi = new();

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
                brojZaFajl = puna.BrojKalkulacije;
            }
            else
            {
                if (DgKalkulacije.SelectedItem is not MaloprodajnaKalkulacija selektovana)
                {
                    MessageBox.Show("Izaberite kalkulaciju za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dobavljac = await db.Partneri.FirstOrDefaultAsync(p => p.SifraPartnera == selektovana.SifraDobavljaca);
                var magacinDaje = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == selektovana.SifraMagacinaDaje);
                var magacinPrima = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == selektovana.SifraMagacinaPrima);

                pdfBytes = Services.PdfReportService.GenerisiMaloprodajnuKalkulacijuPdf(firma, selektovana, dobavljac, magacinDaje, magacinPrima);
                brojZaFajl = selektovana.BrojKalkulacije;
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
        var search = TxtPretragaRacuna.Text.Trim().ToLower();
        DgRacuni.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviRacuni
            : _sviRacuni.Where(r => r.BrojRacuna.ToLower().Contains(search) ||
                                   (r.BrojOtpremnice != null && r.BrojOtpremnice.ToLower().Contains(search)) ||
                                   r.KontoKupca.ToLower().Contains(search)).ToList();

        if (DgRacuni.Items.Count > 0) DgRacuni.SelectedIndex = 0;
        else DgRacunStavke.ItemsSource = null;
    }

    private void DgRacuni_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (e.Column.SortMemberPath != "BrojRacuna") return;

        e.Handled = true;
        bool ascending = e.Column.SortDirection != ListSortDirection.Ascending;

        var view = (ListCollectionView)CollectionViewSource.GetDefaultView(DgRacuni.ItemsSource);
        view.CustomSort = Comparer<object>.Create((a, b) =>
        {
            var brojA = ((RacunOtpremnica)a).BrojRacuna;
            var brojB = ((RacunOtpremnica)b).BrojRacuna;
            int cmp = int.TryParse(brojA, out int nA) && int.TryParse(brojB, out int nB)
                ? nA.CompareTo(nB)
                : string.Compare(brojA, brojB, StringComparison.OrdinalIgnoreCase);
            return ascending ? cmp : -cmp;
        });

        foreach (var col in DgRacuni.Columns) col.SortDirection = null;
        e.Column.SortDirection = ascending ? ListSortDirection.Ascending : ListSortDirection.Descending;
    }

    private void TxtPretragaRacuna_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajRacune();

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

    private void OtvoriIzmenuRacuna()
    {
        if (DgRacuni.SelectedItem is not RacunOtpremnica selektovani)
        {
            MessageBox.Show("Izaberite račun-otpremnicu sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selektovani.IsKnjizen)
        {
            MessageBox.Show("Proknjiženi račun se ne može menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private List<MaterijalnaKartica> _trenutnaRobnaKartica = new();

    private async void UcitajRobnuKarticu()
    {
        if (LstArtikliRobno.SelectedItem is not Artikal artikal || CmbMagacinRobno.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            TxtNaslovArtiklaRobno.Text = "Izaberite magacin i artikal sa liste";
            TxtStanjeArtiklaRobno.Text = "";
            DgRobnaKartica.ItemsSource = null;
            _trenutnaRobnaKartica.Clear();
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _trenutnaRobnaKartica = await db.MaterijalneKartice
                .Where(k => k.SifraMagacina == magacin.SifraMagacina && k.SifraArtikla == artikal.SifraArtikla)
                .OrderBy(k => k.DatumPromene)
                .ThenBy(k => k.MaterijalnaKarticaId)
                .ToListAsync();

            DgRobnaKartica.ItemsSource = _trenutnaRobnaKartica;

            decimal zadnjeStanje = _trenutnaRobnaKartica.LastOrDefault()?.Stanje ?? 0m;
            decimal zadnjiSaldo = _trenutnaRobnaKartica.LastOrDefault()?.Saldo ?? 0m;

            TxtNaslovArtiklaRobno.Text = $"{artikal.Naziv} ({artikal.SifraArtikla}) - Magacin: {magacin.NazivMagacina}";
            TxtStanjeArtiklaRobno.Text = $"Zaliha: {zadnjeStanje:N2} {artikal.JedinicaMere} | Saldo: {zadnjiSaldo:N2} RSD | Prodajna cena: {artikal.ProdajnaCena:N2} RSD | Stavki prometa: {_trenutnaRobnaKartica.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju robne kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajRobnuKarticu_Click(object sender, RoutedEventArgs e)
    {
        if (LstArtikliRobno.SelectedItem is not Artikal artikal || CmbMagacinRobno.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin i artikal sa liste za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_trenutnaRobnaKartica.Count == 0)
        {
            MessageBox.Show($"Nema prometa na robnoj kartici za artikal '{artikal.Naziv}' u magacinu '{magacin.NazivMagacina}'.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiRobnuKarticuPdf(firma, magacin, artikal, _trenutnaRobnaKartica);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Robna_Kartica_{magacin.SifraMagacina}_{artikal.SifraArtikla}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
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

    // ===================== PRIMOPREDAJE ROBE (MAT4) =====================

    private List<PrimopredajaNalog> _svePrimopredaje = new();

    private async void LoadPrimopredaje()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            _svePrimopredaje = await service.GetPrimopredajeAsync();
            ApplyFilterPrimopredaje();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju primopredaja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterPrimopredaje()
    {
        string search = TxtPretragaPrimopredaja.Text.Trim().ToLower();
        DgPrimopredaje.ItemsSource = string.IsNullOrEmpty(search)
            ? _svePrimopredaje
            : _svePrimopredaje.Where(p => p.BrojNaloga.ToLower().Contains(search) ||
                                           p.SifraMagacinaDaje.ToLower().Contains(search) ||
                                           p.SifraMagacinaPrima.ToLower().Contains(search)).ToList();

        if (DgPrimopredaje.Items.Count > 0) DgPrimopredaje.SelectedIndex = 0;
        else DgPrimopredajaStavke.ItemsSource = null;
    }

    private void TxtPretragaPrimopredaja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPrimopredaje();

    private async void DgPrimopredaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog nalog)
        {
            DgPrimopredajaStavke.ItemsSource = null;
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
                DgPrimopredajaStavke.ItemsSource = fullNalog.Stavke;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju stavki primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var win = new PrimopredajaEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadPrimopredaje();
            LoadMagacineIRobneKartice();
        }
    }

    private void BtnIzmeniPrimopredaju_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuPrimopredaje();
    private void DgPrimopredaje_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuPrimopredaje();

    private void OtvoriIzmenuPrimopredaje()
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog nalog)
        {
            MessageBox.Show("Izaberite nalog primopredaje sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (nalog.IsKnjizen)
        {
            MessageBox.Show("Proknjiženi nalog primopredaje se ne može menjati.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var win = new PrimopredajaEditWindow(nalog) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadPrimopredaje();
            LoadMagacineIRobneKartice();
        }
    }

    private async void BtnKnjiziPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog nalog)
        {
            MessageBox.Show("Izaberite nalog primopredaje za knjiženje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (nalog.IsKnjizen)
        {
            MessageBox.Show("Izabrani nalog primopredaje je već proknjižen.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potv = MessageBox.Show($"Da li ste sigurni da želite proknjižiti nalog primopredaje br. {nalog.BrojNaloga}?",
            "Potvrda knjiženja (knjiz_m_naloga - MAT4)", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potv == MessageBoxResult.Yes)
        {
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new PrimopredajaService(db);

                await service.KnjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);
                MessageBox.Show($"Nalog primopredaje #{nalog.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadPrimopredaje();
                LoadMagacineIRobneKartice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri knjiženju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnMasovnoKnjizenjePrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var neknjizeni = _svePrimopredaje.Where(p => !p.IsKnjizen).ToList();
        if (neknjizeni.Count == 0)
        {
            MessageBox.Show("Nema neknjiženih naloga primopredaje za knjiženje.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potv = MessageBox.Show($"Pronađeno je {neknjizeni.Count} neknjiženih naloga primopredaje.\nDa li želite masovno proknjižiti sve naloge? (knjiz_m_naloga 0 - MAT4)",
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

                MessageBox.Show($"Uspešno je proknjiženo {uspesno} naloga primopredaje u robnom knjigovodstvu!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPrimopredaje();
                LoadMagacineIRobneKartice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri masovnom knjiženju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnStampajPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog nalog)
        {
            MessageBox.Show("Izaberite nalog primopredaje za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show($"Greška pri štampi naloga primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
        var filtrirane = string.IsNullOrEmpty(search)
            ? _sveNivelacije
            : _sveNivelacije.Where(n => n.BrojNivelacije.ToLower().Contains(search) || (n.Opis != null && n.Opis.ToLower().Contains(search))).ToList();

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
}

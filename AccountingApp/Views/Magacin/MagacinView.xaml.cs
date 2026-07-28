using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class MagacinView : UserControl
{
    private List<Artikal> _sviArtikli = new();
    private List<UlazNalog> _sviUlazi = new();
    private List<TrebovanjeNalog> _svaTrebovanja = new();

    public MagacinView()
    {
        InitializeComponent();
        // Postavljeno u kodu, ne kao XAML literal — IsChecked="True" bi Checked event
        // ispalio sinhrono usred InitializeComponent(), pre nego što LstArtikli (deklarisan
        // kasnije u istom XAML stablu) uopšte postoji, i FiltrirajArtikle() bi pukao na null.
        ChkSamoSaKarticom.IsChecked = true;
        LoadAllData();
    }

    private void LoadAllData()
    {
        LoadSifrarnikMaterijala();
        LoadMagaciniIArtikli();
        LoadUlazi();
        LoadTrebovanja();
        LoadPrimopredaje();
        LoadBrutoBilansMaterijala();
    }

    // ===================== ŠIFRARNIK MATERIJALA =====================

    private List<Artikal> _sviMaterijaliSifrarnik = new();

    private async void LoadSifrarnikMaterijala()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _sviMaterijaliSifrarnik = await db.Artikli.Where(a => a.Vrsta == "Materijal").OrderBy(a => a.SifraArtikla).ToListAsync();
            ApplyFilterSifrarnikMaterijala();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju šifarnika materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterSifrarnikMaterijala()
    {
        string search = TxtPretragaSifrarnikMaterijala.Text.Trim().ToLower();
        DgSifrarnikMaterijala.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviMaterijaliSifrarnik
            : _sviMaterijaliSifrarnik.Where(a => a.SifraArtikla.ToLower().Contains(search) || a.Naziv.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaSifrarnikMaterijala_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterSifrarnikMaterijala();

    private void BtnNoviMaterijal_Click(object sender, RoutedEventArgs e)
    {
        var win = new MaterijalEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadSifrarnikMaterijala();
            LoadMagaciniIArtikli();
        }
    }

    private void BtnIzmeniMaterijal_Click(object sender, RoutedEventArgs e) => OtvoriIzmenuMaterijala();
    private void DgSifrarnikMaterijala_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OtvoriIzmenuMaterijala();

    private void OtvoriIzmenuMaterijala()
    {
        if (DgSifrarnikMaterijala.SelectedItem is not Artikal selektovan)
        {
            MessageBox.Show("Izaberite materijal sa liste.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var win = new MaterijalEditWindow(selektovan) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() == true)
        {
            LoadSifrarnikMaterijala();
            LoadMagaciniIArtikli();
        }
    }

    private async void BtnBrisiMaterijal_Click(object sender, RoutedEventArgs e)
    {
        if (DgSifrarnikMaterijala.SelectedItem is not Artikal selektovan)
        {
            MessageBox.Show("Izaberite materijal za brisanje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            bool imaKartice = await db.MaterijalneKartice.AnyAsync(mk => mk.SifraArtikla == selektovan.SifraArtikla);
            if (imaKartice)
            {
                MessageBox.Show($"Materijal '{selektovan.Naziv}' (šifra {selektovan.SifraArtikla}) ima otvorene materijalne kartice i promet!\n\nBrisanje nije dozvoljeno jer postoje knjiženja u sistemu.",
                    "Zaštita brisanja (brisanjematerijala - MAT1)", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var potv = MessageBox.Show($"Da li ste sigurni da želite trajno obrisati materijal '{selektovan.Naziv}' (šifra {selektovan.SifraArtikla})?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (potv == MessageBoxResult.Yes)
            {
                var a = await db.Artikli.FirstOrDefaultAsync(x => x.ArtikalId == selektovan.ArtikalId);
                if (a != null)
                {
                    db.Artikli.Remove(a);
                    await db.SaveChangesAsync();
                }
                LoadSifrarnikMaterijala();
                LoadMagaciniIArtikli();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampaSifrarnikaMaterijala_Click(object sender, RoutedEventArgs e)
    {
        if (_sviMaterijaliSifrarnik.Count == 0)
        {
            MessageBox.Show("Nema materijala za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiSifrarnikArtikalaPdf(firma, _sviMaterijaliSifrarnik);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Sifrarnik_Materijala_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi šifarnika materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static readonly AccountingData.Models.Magacin SviMagaciniOpcija = new()
    {
        MagacinId = -1,
        SifraMagacina = "*",
        NazivMagacina = "🏢 Svi magacini"
    };

    private static bool JeSviMagacini(AccountingData.Models.Magacin? m) => m == null || m.MagacinId == -1;
    private static string SifraZaFajl(AccountingData.Models.Magacin m) => JeSviMagacini(m) ? "SVI" : m.SifraMagacina;

    private HashSet<string> _materijaliSaKarticom = new(StringComparer.OrdinalIgnoreCase);

    private async void LoadMagaciniIArtikli()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);

            var magacini = await service.GetMagaciniAsync();
            var stavkeZaCombo = new List<AccountingData.Models.Magacin> { SviMagaciniOpcija };
            stavkeZaCombo.AddRange(magacini);
            CmbMagacin.ItemsSource = stavkeZaCombo;
            CmbMagacin.SelectedIndex = 0;

            _sviArtikli = await db.Artikli.Where(a => a.Vrsta == "Materijal").OrderBy(a => a.Naziv).ToListAsync();
            await OsveziMaterijaleSaKarticomAsync();
            FiltrirajArtikle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju magacina/materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OsveziMaterijaleSaKarticomAsync()
    {
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            _materijaliSaKarticom = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
        using var db = new AccountingDbContext(options);
        var service = new MaterijalnaKarticaService(db);

        _materijaliSaKarticom = await service.GetArtikliSaKarticomAsync(JeSviMagacini(magacin) ? null : magacin.SifraMagacina);
    }

    private void FiltrirajArtikle()
    {
        string search = TxtPretragaArtikla.Text.Trim().ToLower();
        IEnumerable<Artikal> izvor = _sviArtikli;

        if (ChkSamoSaKarticom.IsChecked == true)
            izvor = izvor.Where(a => _materijaliSaKarticom.Contains(a.SifraArtikla));

        if (!string.IsNullOrEmpty(search))
            izvor = izvor.Where(a => a.SifraArtikla.ToLower().Contains(search) || a.Naziv.ToLower().Contains(search));

        LstArtikli.ItemsSource = izvor.ToList();
    }

    private void TxtPretragaArtikla_TextChanged(object sender, TextChangedEventArgs e) => FiltrirajArtikle();
    private void ChkSamoSaKarticom_Changed(object sender, RoutedEventArgs e) => FiltrirajArtikle();

    private async void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await OsveziMaterijaleSaKarticomAsync();
        FiltrirajArtikle();
        LoadKarticaMaterijala();
    }
    private void LstArtikli_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadKarticaMaterijala();

    private List<MaterijalnaKartica> _trenutnaKarticaMaterijala = new();

    private async void LoadKarticaMaterijala()
    {
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            TxtNaslovArtikla.Text = "Izaberite magacin i materijal sa leve strane";
            TxtStanjeArtikla.Text = "";
            DgKarticaMaterijala.ItemsSource = null;
            _trenutnaKarticaMaterijala.Clear();
            return;
        }

        if (LstArtikli.SelectedItems.Count > 1)
        {
            TxtNaslovArtikla.Text = $"{LstArtikli.SelectedItems.Count} materijala izabrano";
            TxtStanjeArtikla.Text = "Koristite 'Štampaj izabrane kartice (PDF)' za štampu kartica svih izabranih materijala.";
            DgKarticaMaterijala.ItemsSource = null;
            _trenutnaKarticaMaterijala.Clear();
            return;
        }

        if (LstArtikli.SelectedItem is not Artikal artikal)
        {
            TxtNaslovArtikla.Text = "Izaberite magacin i materijal sa leve strane";
            TxtStanjeArtikla.Text = "";
            DgKarticaMaterijala.ItemsSource = null;
            _trenutnaKarticaMaterijala.Clear();
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var upit = db.MaterijalneKartice.Where(k => k.SifraArtikla == artikal.SifraArtikla);
            if (!JeSviMagacini(magacin)) upit = upit.Where(k => k.SifraMagacina == magacin.SifraMagacina);

            _trenutnaKarticaMaterijala = await upit
                .OrderBy(k => k.DatumPromene)
                .ThenBy(k => k.MaterijalnaKarticaId)
                .ToListAsync();

            DgKarticaMaterijala.ItemsSource = _trenutnaKarticaMaterijala;

            decimal zadnjeStanje = _trenutnaKarticaMaterijala.LastOrDefault()?.Stanje ?? 0m;
            decimal zadnjiSaldo = _trenutnaKarticaMaterijala.LastOrDefault()?.Saldo ?? 0m;
            decimal prosecnaCena = zadnjeStanje != 0 ? zadnjiSaldo / zadnjeStanje : 0;

            TxtNaslovArtikla.Text = $"{artikal.Naziv} ({artikal.SifraArtikla}) — {magacin.NazivMagacina}";
            TxtStanjeArtikla.Text = $"Trenutno stanje: {zadnjeStanje:N2} {artikal.JedinicaMere} | Prosečna cena: {prosecnaCena:N2} RSD | Vrednost zaliha: {zadnjiSaldo:N2} RSD | Stavki prometa: {_trenutnaKarticaMaterijala.Count}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajKarticu_Click(object sender, RoutedEventArgs e)
    {
        var izabraniMaterijali = LstArtikli.SelectedItems.Cast<Artikal>().ToList();
        if (izabraniMaterijali.Count == 0 || CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin i bar jedan materijal sa liste za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };

            byte[] pdfBytes;
            if (izabraniMaterijali.Count == 1 && !JeSviMagacini(magacin))
            {
                if (_trenutnaKarticaMaterijala.Count == 0)
                {
                    MessageBox.Show($"Nema prometa na materijalnoj kartici za materijal '{izabraniMaterijali[0].Naziv}' u magacinu '{magacin.NazivMagacina}'.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiMaterijalnuKarticuPdf(firma, magacin, izabraniMaterijali[0], _trenutnaKarticaMaterijala);
            }
            else
            {
                var sekcije = await service.PrikupiKarticeAsync(JeSviMagacini(magacin) ? null : magacin.SifraMagacina, izabraniMaterijali);
                if (sekcije.Count == 0)
                {
                    MessageBox.Show("Nema prometa ni na jednoj materijalnoj kartici za izabrane materijale.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiSveMaterijalneKarticePdf(firma, sekcije);
            }

            string sifraZaNaziv = izabraniMaterijali.Count == 1 ? izabraniMaterijali[0].SifraArtikla : $"{izabraniMaterijali.Count}_materijala";
            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Materijalna_Kartica_{SifraZaFajl(magacin)}_{sifraZaNaziv}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi materijalne kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajSveKartice_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin za štampu svih kartica.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);

            var sekcije = await service.PrikupiKarticeAsync(JeSviMagacini(magacin) ? null : magacin.SifraMagacina, null);

            if (sekcije.Count == 0)
            {
                MessageBox.Show($"Nema prometa ni na jednoj materijalnoj kartici{(JeSviMagacini(magacin) ? "" : $" u magacinu '{magacin.NazivMagacina}'")}.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var pdfBytes = Services.PdfReportService.GenerisiSveMaterijalneKarticePdf(firma, sekcije);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Materijalne_Kartice_{SifraZaFajl(magacin)}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi svih materijalnih kartica: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnProveraKartica_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);

            var negativni = await service.GetNegativnaStanjaAsync();
            if (negativni.Count == 0)
            {
                MessageBox.Show("Nema negativnih stanja ni negativnih cena u materijalnim karticama.", "Provera materijalnih kartica", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new ProveraKarticaWindow(negativni) { Owner = Window.GetWindow(this) };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri proveri materijalnih kartica: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ULAZI =====================

    private async void LoadUlazi()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new UlazService(db);

            _sviUlazi = await service.GetUlaziAsync();
            ApplyFilterUlazi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterUlazi()
    {
        string search = TxtPretragaUlaz.Text.Trim().ToLower();
        DgUlazi.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviUlazi
            : _sviUlazi.Where(n => n.BrojNaloga.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaUlaz_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterUlazi();

    private void DgUlazi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgUlazStavke.ItemsSource = DgUlazi.SelectedItem is UlazNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNoviUlaz_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new UlazEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadUlazi();
        }
    }

    private void BtnIzmeniUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new UlazEditWindow(selektovan) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadUlazi();
        }
    }

    private async void BtnStampajUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var artikliMap = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            var pdfBytes = Services.PdfReportService.GenerisiUlazPdf(firma, selektovan, artikliMap);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Ulaz_{selektovan.BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjiziUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovan.IsKnjizen)
        {
            MessageBox.Show($"Ulaz #{selektovan.BrojNaloga} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new UlazService(db);

            await service.KnjiziUlazAsync(selektovan.UlazNalogId);
            MessageBox.Show($"Ulaz #{selektovan.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadUlazi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== TREBOVANJA =====================

    private async void LoadTrebovanja()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new TrebovanjeService(db);

            _svaTrebovanja = await service.GetTrebovanjaAsync();
            ApplyFilterTrebovanja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterTrebovanja()
    {
        string search = TxtPretragaTrebovanje.Text.Trim().ToLower();
        DgTrebovanja.ItemsSource = string.IsNullOrEmpty(search)
            ? _svaTrebovanja
            : _svaTrebovanja.Where(n => n.BrojNaloga.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaTrebovanje_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterTrebovanja();

    private void DgTrebovanja_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgTrebovanjeStavke.ItemsSource = DgTrebovanja.SelectedItem is TrebovanjeNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNovoTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new TrebovanjeEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadTrebovanja();
        }
    }

    private void BtnIzmeniTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new TrebovanjeEditWindow(selektovano) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadTrebovanja();
        }
    }

    private async void BtnStampajTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var artikliMap = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            var pdfBytes = Services.PdfReportService.GenerisiTrebovanjePdf(firma, selektovano, artikliMap);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Trebovanje_{selektovano.BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjiziTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Trebovanje #{selektovano.BrojNaloga} je već proknjiženo!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new TrebovanjeService(db);

            await service.KnjiziTrebovanjeAsync(selektovano.TrebovanjeNalogId);
            MessageBox.Show($"Trebovanje #{selektovano.BrojNaloga} je uspešno proknjiženo!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadTrebovanja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== PRIMOPREDAJE (M4) =====================

    private List<PrimopredajaNalog> _svePrimopredaje = new();

    private async void LoadPrimopredaje()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            _svePrimopredaje = await service.GetPrimopredajeAsync();
            ApplyFilterPrimopredaja();
        }
        catch { }
    }

    private void ApplyFilterPrimopredaja()
    {
        string search = TxtPretragaPrimopredaja.Text.Trim().ToLower();
        DgPrimopredaje.ItemsSource = string.IsNullOrEmpty(search)
            ? _svePrimopredaje
            : _svePrimopredaje.Where(n => n.BrojNaloga.ToLower().Contains(search) || n.SifraMagacinaDaje.ToLower().Contains(search) || n.SifraMagacinaPrima.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaPrimopredaja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPrimopredaja();

    private void DgPrimopredaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgPrimopredajaStavke.ItemsSource = DgPrimopredaje.SelectedItem is PrimopredajaNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new PrimopredajaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadPrimopredaje();
        }
    }

    private void BtnIzmeniPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je proknjižena i nisu dozvoljene nikakve izmene.", "Izmena nije moguća", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dijalog = new PrimopredajaEditWindow(selektovano) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadPrimopredaje();
        }
    }

    private async void BtnStampajPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };
            var artikliMap = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            var pdfBytes = Services.PdfReportService.GenerisiPrimopredajuPdf(firma, selektovano, artikliMap);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Primopredaja_{selektovano.BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnKnjiziPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju sa liste.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            await service.KnjiziPrimopredajuAsync(selektovano.PrimopredajaNalogId);
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je uspešno proknjižena u materijalnom knjigovodstvu!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPrimopredaje();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== BRUTO BILANS MATERIJALA =====================

    private List<RobniBrutoBilansRed> _sviBrutoRedoviMat = new();

    private async void LoadBrutoBilansMaterijala()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            if (CmbMagacinBrutoMat != null && CmbMagacinBrutoMat.ItemsSource == null)
            {
                var magacini = await db.Magacini.ToListAsync();
                magacini.Insert(0, new AccountingData.Models.Magacin { MagacinId = 0, SifraMagacina = "SVI", NazivMagacina = "--- Svi magacini ---" });
                CmbMagacinBrutoMat.ItemsSource = magacini;
                CmbMagacinBrutoMat.SelectedIndex = 0;
            }

            int? magId = (CmbMagacinBrutoMat?.SelectedValue is int idVal && idVal > 0) ? idVal : null;
            DateTime? doDatuma = DpDoDatumaBrutoMat?.SelectedDate;
            string? pretraga = TxtPretragaBrutoMat?.Text.Trim();

            var sviRedovi = await RobniBrutoBilansService.GetRobniBrutoBilansAsync(db, magId, doDatuma, pretraga);

            var materijaliSifre = await db.Artikli.Where(a => a.Vrsta == "Materijal").Select(a => a.SifraArtikla).ToListAsync();
            var materijaliSet = new HashSet<string>(materijaliSifre, StringComparer.OrdinalIgnoreCase);
            _sviBrutoRedoviMat = sviRedovi.Where(r => materijaliSet.Contains(r.SifraArtikla)).ToList();

            DgBrutoBilansMat.ItemsSource = _sviBrutoRedoviMat;

            decimal ukDug = _sviBrutoRedoviMat.Sum(r => r.UlazVrednost);
            decimal ukPot = _sviBrutoRedoviMat.Sum(r => r.IzlazVrednost);
            decimal ukSal = _sviBrutoRedoviMat.Sum(r => r.SaldoVrednosni);

            TxtUkupnoDugujeBrutoMat.Text = $"Ukupno Duguje: {ukDug:N2} RSD";
            TxtUkupnoPotrazujeBrutoMat.Text = $"Ukupno Potražuje: {ukPot:N2} RSD";
            TxtUkupnoSaldoBrutoMat.Text = $"Saldo Zaliha: {ukSal:N2} RSD";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri računanju Bruto bilansa materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CmbMagacinBrutoMat_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadBrutoBilansMaterijala();
    private void DpDoDatumaBrutoMat_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadBrutoBilansMaterijala();
    private void TxtPretragaBrutoMat_TextChanged(object sender, TextChangedEventArgs e) => LoadBrutoBilansMaterijala();
    private void BtnOsveziBrutoMat_Click(object sender, RoutedEventArgs e) => LoadBrutoBilansMaterijala();

    private async void BtnStampajBrutoMat_Click(object sender, RoutedEventArgs e)
    {
        if (_sviBrutoRedoviMat.Count == 0)
        {
            MessageBox.Show("Nema podataka u Bruto bilansu materijala za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };

            DateTime? doDatuma = DpDoDatumaBrutoMat?.SelectedDate;
            var pdfBytes = Services.PdfReportService.GenerisiRobniBrutoBilansPdf(firma, _sviBrutoRedoviMat, doDatuma);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Bruto_Bilans_Materijala_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi Bruto bilansa materijala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

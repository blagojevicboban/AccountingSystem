using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public class MaterijalIzbor : INotifyPropertyChanged
{
    public Materijal Materijal { get; }
    public MaterijalIzbor(Materijal materijal) => Materijal = materijal;

    public string SifraArtikla => Materijal.SifraArtikla;
    public string Naziv => Materijal.Naziv;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class MagacinView : UserControl
{
    private List<Materijal> _sviArtikli = new();
    private List<UlazNalog> _sviUlazi = new();
    private List<TrebovanjeNalog> _svaTrebovanja = new();

    public MagacinView()
    {
        InitializeComponent();
        // Postavljeno u kodu, ne kao XAML literal — IsChecked="True" bi Checked event
        // ispalio sinhrono usred InitializeComponent(), pre nego što LstArtikli (deklarisan
        // kasnije u istom XAML stablu) uopšte postoji, i FiltrirajArtikle() bi pukao na null.
        ChkSamoSaKarticom.IsChecked = true;
        // Isti razlog kao gore — DgUlazi/DgTrebovanja/DgPrimopredaje su deklarisani kasnije
        // u istom XAML stablu (u drugom Border-u), pa bi Checked event ovde upucen kao
        // XAML literal pukao na null pre nego što oni uopšte postoje.
        RbSviUlazi.IsChecked = true;
        RbSviTrebovanja.IsChecked = true;
        RbSviPrimopredaje.IsChecked = true;
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

    private List<Materijal> _sviMaterijaliSifrarnik = new();

    private async void LoadSifrarnikMaterijala()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _sviMaterijaliSifrarnik = await db.Materijali.OrderBy(a => a.SifraArtikla).ToListAsync();
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
        if (DgSifrarnikMaterijala.SelectedItem is not Materijal selektovan)
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
        if (DgSifrarnikMaterijala.SelectedItem is not Materijal selektovan)
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
                var a = await db.Materijali.FirstOrDefaultAsync(x => x.MaterijalId == selektovan.MaterijalId);
                if (a != null)
                {
                    db.Materijali.Remove(a);
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
            var pdfBytes = Services.PdfReportService.GenerisiSifrarnikMaterijalaPdf(firma, _sviMaterijaliSifrarnik);

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

            _sviArtikli = await db.Materijali.OrderBy(a => a.Naziv).ToListAsync();
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
        IEnumerable<Materijal> izvor = _sviArtikli;

        if (ChkSamoSaKarticom.IsChecked == true)
            izvor = izvor.Where(a => _materijaliSaKarticom.Contains(a.SifraArtikla));

        if (!string.IsNullOrEmpty(search))
            izvor = izvor.Where(a => a.SifraArtikla.ToLower().Contains(search) || a.Naziv.ToLower().Contains(search));

        var izbori = izvor.Select(a => new MaterijalIzbor(a)).ToList();
        foreach (var izbor in izbori) izbor.PropertyChanged += MaterijalIzbor_PropertyChanged;
        LstArtikli.ItemsSource = izbori;
        UpdateBtnStampajKarticuState();
    }

    private void MaterijalIzbor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MaterijalIzbor.IsSelected))
        {
            UpdateBtnStampajKarticuState();
        }
    }

    private bool _updatingChkSviArtikli;

    private void UpdateBtnStampajKarticuState()
    {
        var izbori = LstArtikli.ItemsSource as List<MaterijalIzbor>;
        bool imaCekiranih = izbori?.Any(i => i.IsSelected) ?? false;
        bool imaPrikazanuKarticu = LstArtikli.SelectedItem is MaterijalIzbor && _trenutnaKarticaMaterijala.Count > 0;
        BtnStampajKarticu.IsEnabled = imaCekiranih || imaPrikazanuKarticu;

        if (ChkSviArtikli == null) return;

        _updatingChkSviArtikli = true;
        if (izbori == null || izbori.Count == 0)
            ChkSviArtikli.IsChecked = false;
        else if (izbori.All(i => i.IsSelected))
            ChkSviArtikli.IsChecked = true;
        else if (izbori.All(i => !i.IsSelected))
            ChkSviArtikli.IsChecked = false;
        else
            ChkSviArtikli.IsChecked = null;
        _updatingChkSviArtikli = false;
    }

    private void ChkSviArtikli_Checked(object sender, RoutedEventArgs e) => SetSviArtikliIzabrani(true);

    private void ChkSviArtikli_Unchecked(object sender, RoutedEventArgs e) => SetSviArtikliIzabrani(false);

    private void SetSviArtikliIzabrani(bool izabrano)
    {
        if (_updatingChkSviArtikli) return;
        if (LstArtikli.ItemsSource is not List<MaterijalIzbor> izbori) return;

        foreach (var izbor in izbori) izbor.IsSelected = izabrano;
        UpdateBtnStampajKarticuState();
    }

    private void LstArtikli_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var red = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (red?.Item is MaterijalIzbor izbor)
        {
            LstArtikli.SelectedItem = izbor;
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

    private void CtxStampajKarticu_Click(object sender, RoutedEventArgs e) => BtnStampajKarticu_Click(sender, e);

    private void CtxExportExcelKartica_Click(object sender, RoutedEventArgs e) => BtnExportExcelKartica_Click(sender, e);

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
            PrikaziSumeMaterijala();
            UpdateBtnStampajKarticuState();
            return;
        }

        if (LstArtikli.SelectedItem is not MaterijalIzbor izbor)
        {
            TxtNaslovArtikla.Text = "Izaberite magacin i materijal sa leve strane";
            TxtStanjeArtikla.Text = "";
            DgKarticaMaterijala.ItemsSource = null;
            _trenutnaKarticaMaterijala.Clear();
            PrikaziSumeMaterijala();
            UpdateBtnStampajKarticuState();
            return;
        }

        var artikal = izbor.Materijal;
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
            PrikaziSumeMaterijala();

            decimal zadnjeStanje = _trenutnaKarticaMaterijala.LastOrDefault()?.Stanje ?? 0m;
            decimal zadnjiSaldo = _trenutnaKarticaMaterijala.LastOrDefault()?.Saldo ?? 0m;
            decimal prosecnaCena = zadnjeStanje != 0 ? zadnjiSaldo / zadnjeStanje : 0;

            TxtNaslovArtikla.Text = $"{artikal.Naziv} ({artikal.SifraArtikla}) — {magacin.NazivMagacina}";
            TxtStanjeArtikla.Text = $"Trenutno stanje: {zadnjeStanje:N2} {artikal.JedinicaMere} | Prosečna cena: {prosecnaCena:N2} RSD | Stavki prometa: {_trenutnaKarticaMaterijala.Count}";
            UpdateBtnStampajKarticuState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrikaziSumeMaterijala()
    {
        TxtSumaUlazMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Ulaz).ToString("N2");
        TxtSumaIzlazMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Izlaz).ToString("N2");
        TxtSumaDugujeMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Duguje).ToString("N2");
        TxtSumaPotrazujeMaterijal.Text = _trenutnaKarticaMaterijala.Sum(k => k.Potrazuje).ToString("N2");
        TxtSumaSaldoMaterijal.Text = (_trenutnaKarticaMaterijala.Count > 0 ? _trenutnaKarticaMaterijala[^1].Saldo : 0m).ToString("N2");
    }

    private async void BtnStampajKarticu_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin za štampu kartice.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var izbori = LstArtikli.ItemsSource as List<MaterijalIzbor> ?? new();
        var izabraniMaterijali = izbori.Where(i => i.IsSelected).Select(i => i.Materijal).ToList();

        if (izabraniMaterijali.Count == 0 && LstArtikli.SelectedItem is MaterijalIzbor trenutni)
        {
            izabraniMaterijali.Add(trenutni.Materijal);
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };

            byte[] pdfBytes;
            string sifraZaNaziv;

            if (izabraniMaterijali.Count == 0)
            {
                var potvrda = MessageBox.Show("Nijedan materijal nije čekiran. Da li želite da štampate kartice SVIH materijala?", "Štampa svih kartica", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (potvrda != MessageBoxResult.Yes) return;

                var sveSekcije = await service.PrikupiKarticeAsync(JeSviMagacini(magacin) ? null : magacin.SifraMagacina, null);
                if (sveSekcije.Count == 0)
                {
                    MessageBox.Show($"Nema prometa ni na jednoj materijalnoj kartici{(JeSviMagacini(magacin) ? "" : $" u magacinu '{magacin.NazivMagacina}'")}.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiSveMaterijalneKarticePdf(firma, sveSekcije);
                sifraZaNaziv = "SVI_MATERIJALI";
            }
            else if (izabraniMaterijali.Count == 1 && !JeSviMagacini(magacin))
            {
                if (_trenutnaKarticaMaterijala.Count == 0)
                {
                    MessageBox.Show($"Nema prometa na materijalnoj kartici za materijal '{izabraniMaterijali[0].Naziv}' u magacinu '{magacin.NazivMagacina}'.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                pdfBytes = Services.PdfReportService.GenerisiMaterijalnuKarticuPdf(firma, magacin, izabraniMaterijali[0], _trenutnaKarticaMaterijala);
                sifraZaNaziv = izabraniMaterijali[0].SifraArtikla;
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
                sifraZaNaziv = $"{izabraniMaterijali.Count}_materijala";
            }

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
        if (DgUlazi == null) return;

        string search = TxtPretragaUlaz.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniUlazi?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniUlazi?.IsChecked == true;

        DgUlazi.ItemsSource = _sviUlazi.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search)) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();
    }

    private void TxtPretragaUlaz_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterUlazi();

    private void Filter_Ulazi_Changed(object sender, RoutedEventArgs e) => ApplyFilterUlazi();

    private void DgUlazi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgUlazi.SelectedItem is UlazNalog nalog)
        {
            var artikliDict = _sviArtikli.ToDictionary(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            foreach (var st in nalog.Stavke)
            {
                st.NazivArtikla = artikliDict.TryGetValue(st.SifraArtikla, out var art) ? art.Naziv : null;
            }
            DgUlazStavke.ItemsSource = nalog.Stavke;
        }
        else
        {
            DgUlazStavke.ItemsSource = null;
        }
    }

    private void BtnNoviUlaz_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new UlazEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadUlazi();
        }
    }

    private async void BtnIzmeniUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selektovan.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Ulaz #{selektovan.BrojNaloga} je proknjižen i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                "Proknjižen ulaz", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje ulaza dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new UlazService(db);
                await service.RasknjiziUlazAsync(selektovan.UlazNalogId);

                LoadUlazi();

                var osvezen = _sviUlazi.FirstOrDefault(u => u.UlazNalogId == selektovan.UlazNalogId);
                if (osvezen != null)
                {
                    var dijalogR = new UlazEditWindow(osvezen) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadUlazi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            var artikliMap = await db.Materijali.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            var magacin = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == selektovan.SifraMagacina)
                ?? new AccountingData.Models.Magacin { SifraMagacina = selektovan.SifraMagacina, NazivMagacina = selektovan.SifraMagacina };
            var pdfBytes = Services.PdfReportService.GenerisiUlazPdf(firma, selektovan, artikliMap, magacin);

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
        if (DgTrebovanja == null) return;

        string search = TxtPretragaTrebovanje.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniTrebovanja?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniTrebovanja?.IsChecked == true;

        DgTrebovanja.ItemsSource = _svaTrebovanja.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search)) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();
    }

    private void TxtPretragaTrebovanje_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterTrebovanja();

    private void Filter_Trebovanja_Changed(object sender, RoutedEventArgs e) => ApplyFilterTrebovanja();

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

    private async void BtnIzmeniTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selektovano.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Trebovanje #{selektovano.BrojNaloga} je proknjiženo i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                "Proknjiženo trebovanje", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje trebovanja dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new TrebovanjeService(db);
                await service.RasknjiziTrebovanjeAsync(selektovano.TrebovanjeNalogId);

                LoadTrebovanja();

                var osvezeno = _svaTrebovanja.FirstOrDefault(t => t.TrebovanjeNalogId == selektovano.TrebovanjeNalogId);
                if (osvezeno != null)
                {
                    var dijalogR = new TrebovanjeEditWindow(osvezeno) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadTrebovanja();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            var artikliMap = await db.Materijali.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            var magacin = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == selektovano.SifraMagacina)
                ?? new AccountingData.Models.Magacin { SifraMagacina = selektovano.SifraMagacina, NazivMagacina = selektovano.SifraMagacina };
            var pdfBytes = Services.PdfReportService.GenerisiTrebovanjePdf(firma, selektovano, artikliMap, magacin);

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
        if (DgPrimopredaje == null) return;

        string search = TxtPretragaPrimopredaja.Text.Trim().ToLower();
        bool samoProknjizeni = RbProknjizeniPrimopredaje?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeniPrimopredaje?.IsChecked == true;

        DgPrimopredaje.ItemsSource = _svePrimopredaje.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search) || n.SifraMagacinaDaje.ToLower().Contains(search) || n.SifraMagacinaPrima.ToLower().Contains(search)) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();
    }

    private void TxtPretragaPrimopredaja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPrimopredaja();

    private void Filter_Primopredaje_Changed(object sender, RoutedEventArgs e) => ApplyFilterPrimopredaja();

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

    private async void BtnIzmeniPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Primopredaja #{selektovano.BrojNaloga} je proknjižena i ne može se menjati u ovom statusu.\n\nDa li želite da je rasknjižite radi izmene?",
                "Proknjižena primopredaja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje primopredaje dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
                using var db = new AccountingDbContext(options);
                var service = new PrimopredajaService(db);
                await service.RasknjiziPrimopredajuAsync(selektovano.PrimopredajaNalogId);

                LoadPrimopredaje();

                var osvezeno = _svePrimopredaje.FirstOrDefault(p => p.PrimopredajaNalogId == selektovano.PrimopredajaNalogId);
                if (osvezeno != null)
                {
                    var dijalogR = new PrimopredajaEditWindow(osvezeno) { Owner = Window.GetWindow(this) };
                    if (dijalogR.ShowDialog() == true) LoadPrimopredaje();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
            var artikliDict = await db.Materijali.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
            foreach (var st in selektovano.Stavke)
            {
                if (artikliDict.TryGetValue(st.SifraArtikla, out var art))
                {
                    st.NazivArtikla = art.Naziv;
                    st.JedinicaMere = art.JedinicaMere;
                }
            }

            var magDaje = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == selektovano.SifraMagacinaDaje)
                ?? new AccountingData.Models.Magacin { SifraMagacina = selektovano.SifraMagacinaDaje, NazivMagacina = selektovano.SifraMagacinaDaje };
            var magPrima = await db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == selektovano.SifraMagacinaPrima)
                ?? new AccountingData.Models.Magacin { SifraMagacina = selektovano.SifraMagacinaPrima, NazivMagacina = selektovano.SifraMagacinaPrima };

            var pdfBytes = Services.PdfReportService.GenerisiPrimopredajuPdf(firma, selektovano, magDaje, magPrima);

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

            _sviBrutoRedoviMat = await RobniBrutoBilansService.GetMaterijalniBrutoBilansAsync(db, magId, doDatuma, pretraga);

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

    // ===================== EXCEL EXPORT DUGMIĆI =====================

    private void BtnExportExcelMaterijali_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgSifrarnikMaterijala, "Šifrarnik materijala", "Sifrarnik_Materijala");

    private void BtnExportExcelUlazi_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgUlazi, "Ulazi materijala", "Ulazi_Materijala");

    private void BtnExportExcelTrebovanja_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgTrebovanja, "Trebovanja materijala", "Trebovanja_Materijala");

    private void BtnExportExcelPrimopredaje_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgPrimopredaje, "Primopredaje materijala", "Primopredaje_Materijala");

    private void BtnExportExcelKartica_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgKarticaMaterijala, TxtNaslovArtikla.Text, "Materijalna_Kartica");

    private void BtnExportExcelBrutoMat_Click(object sender, RoutedEventArgs e)
        => Services.ExcelExportService.ExportDataGridToExcel(DgBrutoBilansMat, "Bruto bilans materijalnog knjigovodstva", "Bruto_Bilans_Materijalnog_Knjigovodstva");
}

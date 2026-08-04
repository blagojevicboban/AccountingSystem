using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeApp.Views.Pomoc;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Trgovina;

public partial class PrimopredajaEditWindow : Window
{
    // Šifarnik artikala ume da ima nekoliko hiljada stavki, pa se u padajuću listu nikad
    // ne ubacuje cela lista — samo prvih toliko pogodaka filtera (isti obrazac kao ColKonto
    // u NalogEditWindow).
    private const int MaxPrikazanihArtikala = 100;

    private readonly PrimopredajaNalog? _existingNalog;
    private readonly string _vrstaZaNovu;
    private List<ERPiFinansijeData.Models.Magacin> _magacini = new();
    private List<Artikal> _artikli = new();
    public ObservableCollection<PrimopredajaStavkaModel> StavkeModels { get; set; } = new();

    private ComboBox? _aktivniArtikalCombo;
    private PrimopredajaStavkaModel? _aktivnaArtikalStavka;
    private string _artikalPreIzmene = string.Empty;
    private bool _bezReakcijeNaArtikal;
    private bool _internoZatvaranjeArtikalListe;
    private string _poslednjiOtkucaniArtikalUpit = string.Empty;

    public PrimopredajaEditWindow(PrimopredajaNalog? existingNalog = null, string vrstaZaNovu = "Primopredaja")
    {
        InitializeComponent();
        _existingNalog = existingNalog;
        _vrstaZaNovu = vrstaZaNovu;
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _magacini = await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync();
            _artikli = await db.Artikli.OrderBy(a => a.SifraArtikla).ToListAsync();

            CmbMagacinDaje.ItemsSource = _magacini;
            CmbMagacinPrima.ItemsSource = _magacini;

            var artikliDict = _artikli.ToDictionary(a => a.SifraArtikla, a => a.Naziv, StringComparer.OrdinalIgnoreCase);

            if (_existingNalog != null)
            {
                TxtNaslov.Text = $"✏️ Izmena — {_existingNalog.VrstaDokumenta} #{_existingNalog.BrojNaloga}";
                TxtBrojNaloga.Text = _existingNalog.BrojNaloga.ToString();
                TxtBrojNaloga.IsReadOnly = true;
                DpDatum.SelectedDate = _existingNalog.Datum;

                CmbMagacinDaje.SelectedItem = _magacini.FirstOrDefault(m => m.SifraMagacina == _existingNalog.SifraMagacinaDaje);
                CmbMagacinPrima.SelectedItem = _magacini.FirstOrDefault(m => m.SifraMagacina == _existingNalog.SifraMagacinaPrima);
                CmbStopaPdv.Text = _existingNalog.StopaPdv.ToString(System.Globalization.CultureInfo.InvariantCulture);

                int rbr = 1;
                foreach (var st in _existingNalog.Stavke)
                {
                    artikliDict.TryGetValue(st.SifraArtikla, out var nazivArtikla);
                    StavkeModels.Add(new PrimopredajaStavkaModel
                    {
                        RedniBroj = rbr++,
                        SifraArtikla = st.SifraArtikla,
                        NazivArtikla = nazivArtikla,
                        Kolicina = st.Kolicina,
                        Cena = st.Cena,
                        Iznos = st.Iznos
                    });
                }
            }
            else
            {
                TxtNaslov.Text = $"➕ Novi nalog — {_vrstaZaNovu}";
                DpDatum.SelectedDate = DateTime.Now;

                // Generiši sledeći broj naloga — nezavisan brojač po vrsti dokumenta, analogno
                // odvojenim legacy DBF fajlovima (ZADUZ.DBF / RAZDUZ.DBF / MAT_NAL.DBF).
                int maxBr = (await db.PrimopredajaNalozi
                    .Where(n => n.VrstaDokumenta == _vrstaZaNovu)
                    .MaxAsync(n => (int?)n.BrojNaloga) ?? 0) + 1;
                TxtBrojNaloga.Text = maxBr.ToString("D5");

                if (_magacini.Count > 0) CmbMagacinDaje.SelectedIndex = 0;
                if (_magacini.Count > 1) CmbMagacinPrima.SelectedIndex = 1;
                else if (_magacini.Count > 0) CmbMagacinPrima.SelectedIndex = 0;

                CmbStopaPdv.Text = "20";

                StavkeModels.Add(new PrimopredajaStavkaModel { RedniBroj = 1 });
            }

            DgStavke.ItemsSource = StavkeModels;
            AzurirajUpozorenjePrelaza();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju podataka: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Prikazuje polje za stopu PDV i napomenu samo kad magacin koji daje i magacin koji prima
    /// nisu iste vrste (Veleprodaja/Maloprodaja) — samo tada <see cref="PrimopredajaService"/>
    /// pravi nalog u Glavnoj knjizi.
    /// </summary>
    private void AzurirajUpozorenjePrelaza()
    {
        bool prelaziVpMp = CmbMagacinDaje.SelectedItem is ERPiFinansijeData.Models.Magacin md
            && CmbMagacinPrima.SelectedItem is ERPiFinansijeData.Models.Magacin mp
            && md.VrstaMagacina != mp.VrstaMagacina;

        var vidljivost = prelaziVpMp ? Visibility.Visible : Visibility.Collapsed;
        TxtLabelStopaPdv.Visibility = vidljivost;
        CmbStopaPdv.Visibility = vidljivost;
        TxtInfoPrelaz.Visibility = vidljivost;
    }

    private void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e) => AzurirajUpozorenjePrelaza();

    // ===================== Pretraga artikla u ćeliji (šifra + naziv, strelice, Enter/Tab/klik) =====================
    // Isti obrazac kao ColKonto u NalogEditWindow: template kolona umesto DataGridComboBoxColumn,
    // jer se filtrirana lista menja dok se kuca, a izbor se upisuje u model ručno (kod, ne kroz
    // SelectedValueBinding koji bi izgubio vrednost čim otkucani tekst ispadne iz filtrirane liste).

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column == ColArtikal && e.EditAction == DataGridEditAction.Commit)
        {
            PrihvatiUnetiArtikal();
        }
    }

    private void ArtikalCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox cb) return;

        _aktivniArtikalCombo = cb;
        _aktivnaArtikalStavka = cb.DataContext as PrimopredajaStavkaModel;
        _artikalPreIzmene = _aktivnaArtikalStavka?.SifraArtikla ?? string.Empty;
        _poslednjiOtkucaniArtikalUpit = _artikalPreIzmene;

        _bezReakcijeNaArtikal = true;
        cb.ItemsSource = FiltrirajArtikle(_artikalPreIzmene);
        cb.Text = _artikalPreIzmene;
        _bezReakcijeNaArtikal = false;

        // Klik mišem na stavku liste hvatamo direktno (a ne preko SelectionChanged /
        // DropDownClosed) da bismo znali da je izbor zaista potvrđen mišem, a ne samo
        // označen strelicama.
        cb.RemoveHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(ArtikalCombo_PreviewMouseLeftButtonUp));
        cb.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(ArtikalCombo_PreviewMouseLeftButtonUp), true);

        if (cb.Template.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.TextChanged -= ComboArtikal_TextChanged;
            tb.TextChanged += ComboArtikal_TextChanged;
            tb.PreviewKeyDown -= ComboArtikal_PreviewKeyDown;
            tb.PreviewKeyDown += ComboArtikal_PreviewKeyDown;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.SelectAll();
                tb.Focus();
                PostaviPadajucuListuArtikal(cb, cb.Items.Count > 0);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void ArtikalCombo_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox cb) return;

        cb.RemoveHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(ArtikalCombo_PreviewMouseLeftButtonUp));
        if (cb.Template?.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.TextChanged -= ComboArtikal_TextChanged;
            tb.PreviewKeyDown -= ComboArtikal_PreviewKeyDown;
        }

        if (ReferenceEquals(_aktivniArtikalCombo, cb))
        {
            _aktivniArtikalCombo = null;
            _aktivnaArtikalStavka = null;
        }
    }

    private void ComboArtikal_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;

        if (e.Key == Key.Down)
        {
            if (!cb.IsDropDownOpen)
            {
                PostaviPadajucuListuArtikal(cb, true);
                e.Handled = true;
                return;
            }

            if (cb.Items.Count > 0)
            {
                int nextIndex = cb.SelectedIndex + 1;
                if (nextIndex < cb.Items.Count)
                {
                    cb.SelectedIndex = nextIndex;
                    if (cb.SelectedItem is Artikal izabran)
                    {
                        SkrolujDoStavkeArtikal(cb, izabran);
                        UpisiArtikal(izabran);
                    }
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up)
        {
            if (cb.IsDropDownOpen && cb.Items.Count > 0)
            {
                if (cb.SelectedIndex > 0)
                {
                    int prevIndex = cb.SelectedIndex - 1;
                    cb.SelectedIndex = prevIndex;
                    if (cb.SelectedItem is Artikal izabran)
                    {
                        SkrolujDoStavkeArtikal(cb, izabran);
                        UpisiArtikal(izabran);
                    }
                }
                else if (cb.SelectedIndex == 0)
                {
                    cb.SelectedIndex = -1;
                    if (!string.IsNullOrEmpty(_poslednjiOtkucaniArtikalUpit))
                    {
                        _bezReakcijeNaArtikal = true;
                        tb.Text = _poslednjiOtkucaniArtikalUpit;
                        tb.CaretIndex = _poslednjiOtkucaniArtikalUpit.Length;
                        _bezReakcijeNaArtikal = false;
                    }
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.PageDown)
        {
            if (cb.IsDropDownOpen && cb.Items.Count > 0)
            {
                int nextIndex = Math.Min(cb.Items.Count - 1, Math.Max(0, cb.SelectedIndex + 5));
                cb.SelectedIndex = nextIndex;
                if (cb.SelectedItem is Artikal izabran)
                {
                    SkrolujDoStavkeArtikal(cb, izabran);
                    UpisiArtikal(izabran);
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.PageUp)
        {
            if (cb.IsDropDownOpen && cb.Items.Count > 0)
            {
                int prevIndex = Math.Max(0, cb.SelectedIndex - 5);
                cb.SelectedIndex = prevIndex;
                if (cb.SelectedItem is Artikal izabran)
                {
                    SkrolujDoStavkeArtikal(cb, izabran);
                    UpisiArtikal(izabran);
                }
                e.Handled = true;
            }
        }
    }

    private void ComboArtikal_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_bezReakcijeNaArtikal) return;
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;

        // Tekst se promenio zato što je stavka izabrana u listi (ComboBox upisuje
        // "šifra - naziv"), a ne zato što korisnik kuca — tada se ne filtrira.
        if (cb.SelectedItem is Artikal izabran && tb.Text == izabran.Prikaz) return;

        string tekst = tb.Text;
        int kursor = tb.CaretIndex;

        if (cb.SelectedItem == null)
        {
            _poslednjiOtkucaniArtikalUpit = tekst;
        }

        var filtrirano = FiltrirajArtikle(tekst);

        _bezReakcijeNaArtikal = true;
        cb.ItemsSource = filtrirano;
        // Zamena ItemsSource poništava selekciju, a ComboBox tada ume da obriše i
        // otkucani tekst — vraćamo ga zajedno sa pozicijom kursora.
        if (tb.Text != tekst)
        {
            tb.Text = tekst;
            tb.CaretIndex = Math.Min(kursor, tekst.Length);
        }
        _bezReakcijeNaArtikal = false;

        PostaviPadajucuListuArtikal(cb, filtrirano.Count > 0);
    }

    private List<Artikal> FiltrirajArtikle(string upit)
    {
        string q = upit.Trim().ToLowerInvariant();
        var rezultat = new List<Artikal>(MaxPrikazanihArtikala);

        if (q.Length == 0)
        {
            for (int i = 0; i < _artikli.Count && rezultat.Count < MaxPrikazanihArtikala; i++)
            {
                rezultat.Add(_artikli[i]);
            }
            return rezultat;
        }

        // Korisnik podjednako često traži po šifri i po nazivu — pogoci koji počinju
        // upitom (bilo koje polje) idu prvi, pa tek onda oni koji ga samo sadrže.
        foreach (var a in _artikli)
        {
            if (a.SifraArtikla.StartsWith(q, StringComparison.OrdinalIgnoreCase) || a.Naziv.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            {
                rezultat.Add(a);
                if (rezultat.Count >= MaxPrikazanihArtikala) return rezultat;
            }
        }

        foreach (var a in _artikli)
        {
            if (rezultat.Contains(a)) continue;
            if (a.SifraArtikla.Contains(q, StringComparison.OrdinalIgnoreCase) || a.Naziv.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                rezultat.Add(a);
                if (rezultat.Count >= MaxPrikazanihArtikala) return rezultat;
            }
        }

        return rezultat;
    }

    private void PostaviPadajucuListuArtikal(ComboBox cb, bool otvorena)
    {
        if (cb.IsDropDownOpen == otvorena) return;

        _internoZatvaranjeArtikalListe = true;
        cb.IsDropDownOpen = otvorena;
        _internoZatvaranjeArtikalListe = false;
    }

    private void ArtikalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_bezReakcijeNaArtikal || _internoZatvaranjeArtikalListe) return;
        if (sender is not ComboBox cb || cb.SelectedItem is not Artikal izabran) return;

        // Pokriva i izbor strelicama u otvorenoj listi; potvrda (Enter/Tab/klik)
        // samo zatvara ćeliju.
        UpisiArtikal(izabran);
        SkrolujDoStavkeArtikal(cb, izabran);
    }

    private static void SkrolujDoStavkeArtikal(ComboBox cb, object item)
    {
        if (cb == null || item == null) return;
        cb.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (cb.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement element)
            {
                element.BringIntoView();
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void ArtikalCombo_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject izvor) return;

        var stavkaListe = ItemsControl.ContainerFromElement(sender as ComboBox, izvor) as ComboBoxItem;
        if (stavkaListe?.DataContext is not Artikal izabran) return;

        UpisiArtikal(izabran);

        // ComboBox tek posle ovog događaja završava sopstvenu obradu klika, pa se
        // zatvaranje ćelije odlaže da mu se ne bi izmakao editor ispod ruke.
        var stavka = _aktivnaArtikalStavka;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ZavrsiUnosArtikla();
            PredjiNaKolonuPosleArtikla(stavka);
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void UpisiArtikal(Artikal artikal)
    {
        if (_aktivnaArtikalStavka == null) return;
        _aktivnaArtikalStavka.SifraArtikla = artikal.SifraArtikla;
        _aktivnaArtikalStavka.NazivArtikla = artikal.Naziv;
    }

    /// <summary>
    /// Prihvata ono što je korisnik uneo u ćeliju artikla: izabranu stavku iz liste,
    /// a ako izbora nema — otkucani tekst se razrešava u šifarniku artikala.
    /// </summary>
    private void PrihvatiUnetiArtikal()
    {
        var cb = _aktivniArtikalCombo;
        if (cb == null) return;

        if (cb.SelectedItem is Artikal izabran)
        {
            UpisiArtikal(izabran);
            return;
        }

        string uneto = TekstUnetogArtikla();
        if (uneto.Length == 0) return;

        var poklapanje = NadjiArtikal(uneto);
        if (poklapanje != null) UpisiArtikal(poklapanje);
    }

    private string TekstUnetogArtikla()
    {
        var cb = _aktivniArtikalCombo;
        if (cb == null) return string.Empty;

        return ((cb.Template?.FindName("PART_EditableTextBox", cb) as TextBox)?.Text ?? cb.Text ?? string.Empty).Trim();
    }

    // Ako je u polju ostao prikaz oblika "A1 - NAZIV ARTIKLA (kom)", upotrebljiva je
    // samo šifra ispred crte.
    private static string SamoSifraArtikla(string tekst)
    {
        int crta = tekst.IndexOf(" - ", StringComparison.Ordinal);
        return crta > 0 ? tekst[..crta].Trim() : tekst.Trim();
    }

    private Artikal? NadjiArtikal(string uneto)
    {
        string sifra = SamoSifraArtikla(uneto);

        var pogodak = _artikli.FirstOrDefault(a => a.SifraArtikla.Equals(sifra, StringComparison.OrdinalIgnoreCase));
        if (pogodak != null) return pogodak;

        var kandidati = FiltrirajArtikle(uneto);
        return kandidati.FirstOrDefault();
    }

    private void ZavrsiUnosArtikla()
    {
        var cb = _aktivniArtikalCombo;
        if (cb != null) PostaviPadajucuListuArtikal(cb, false);

        DgStavke.CommitEdit(DataGridEditingUnit.Cell, true);
        _aktivniArtikalCombo = null;
        _aktivnaArtikalStavka = null;
    }

    private void PredjiNaKolonuPosleArtikla(PrimopredajaStavkaModel? stavka)
    {
        stavka ??= DgStavke.SelectedItem as PrimopredajaStavkaModel;
        if (stavka == null) return;

        int indeks = DgStavke.Columns.IndexOf(ColArtikal);
        if (indeks < 0 || indeks + 1 >= DgStavke.Columns.Count) return;

        DgStavke.CurrentCell = new DataGridCellInfo(stavka, DgStavke.Columns[indeks + 1]);
        DgStavke.BeginEdit();
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        StavkeModels.Add(new PrimopredajaStavkaModel { RedniBroj = StavkeModels.Count + 1 });
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PrimopredajaStavkaModel model)
        {
            StavkeModels.Remove(model);
            int rbr = 1;
            foreach (var s in StavkeModels) s.RedniBroj = rbr++;
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brNaloga))
        {
            MessageBox.Show("Molimo unesite ispravan broj naloga.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbMagacinDaje.SelectedItem is not ERPiFinansijeData.Models.Magacin magDaje ||
            CmbMagacinPrima.SelectedItem is not ERPiFinansijeData.Models.Magacin magPrima)
        {
            MessageBox.Show("Izaberite magacin koji daje i magacin koji prima robnog prometa.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (magDaje.SifraMagacina == magPrima.SifraMagacina)
        {
            MessageBox.Show("Magacin koji daje i magacin koji prima moraju biti različiti!", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool prelaziVpMp = magDaje.VrstaMagacina != magPrima.VrstaMagacina;
        decimal stopaPdv = 20m;
        if (prelaziVpMp && !decimal.TryParse(CmbStopaPdv.Text.Trim(), out stopaPdv))
        {
            MessageBox.Show("Unesite ispravnu stopu PDV (%) — prelaz veleprodaja↔maloprodaja pravi nalog u Glavnoj knjizi i mora znati po kojoj stopi da obračuna porez.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var validneStavke = StavkeModels.Where(s => !string.IsNullOrWhiteSpace(s.SifraArtikla) && s.Kolicina > 0).ToList();
        if (validneStavke.Count == 0)
        {
            MessageBox.Show("Unesite bar jednu validnu stavku robe sa količinom većom od 0.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            var nalog = _existingNalog ?? new PrimopredajaNalog { VrstaDokumenta = _vrstaZaNovu };
            nalog.BrojNaloga = brNaloga;
            nalog.Datum = DpDatum.SelectedDate ?? DateTime.Now;
            nalog.SifraMagacinaDaje = magDaje.SifraMagacina;
            nalog.SifraMagacinaPrima = magPrima.SifraMagacina;
            nalog.StopaPdv = stopaPdv;
            nalog.Stavke = validneStavke.Select((s, idx) => new PrimopredajaStavka
            {
                RedniBroj = idx + 1,
                SifraArtikla = s.SifraArtikla,
                Kolicina = s.Kolicina,
                Cena = s.Cena,
                Iznos = s.Iznos > 0 ? s.Iznos : s.Kolicina * s.Cena
            }).ToList();

            await service.SavePrimopredajuAsync(nalog);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju naloga primopredaje:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
            "🔄 Pomoć — Primopredaja robe",
            "Interni prenos robe između magacina.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
                ("➕ Dodaj stavku", "Dodaje artikal u nalog za primopredaju."),
                ("🗑️", "Uklanja stavku iz reda u tabeli."),
            },
            "Obavezno izabrati različite magacine 'daje' i 'prima'. Primopredaja se knjiži kao izlaz iz jednog i ulaz u drugi magacin po istoj vrednosti. Kod prelaska veleprodaja↔maloprodaja se dodatno traži stopa PDV i pravi se nalog u Glavnoj knjizi (1320/1340 + ukalkulisani PDV)."
        ) { Owner = this }.ShowDialog();
    }
}

public class PrimopredajaStavkaModel
{
    public int RedniBroj { get; set; }
    public string SifraArtikla { get; set; } = string.Empty;
    public string? NazivArtikla { get; set; }
    public decimal Kolicina { get; set; }
    public decimal Cena { get; set; }
    public decimal Iznos { get; set; }

    /// <summary>"šifra - naziv" za prikaz u ćeliji van režima izmene.</summary>
    public string PrikazArtikla => string.IsNullOrWhiteSpace(SifraArtikla)
        ? string.Empty
        : string.IsNullOrWhiteSpace(NazivArtikla) ? SifraArtikla : $"{SifraArtikla} - {NazivArtikla}";
}

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ERPiFinansijeApp.Views.Konta;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Nalozi;

public partial class NalogEditWindow : Window
{
    // Kontni plan ume da ima i preko 3.000 konta, pa se u padajuću listu nikad
    // ne ubacuje cela lista — samo prvih toliko pogodaka filtera.
    private const int MaxPrikazanihKonta = 100;

    private readonly ObservableCollection<StavkaNaloga> _stavke = new();
    private readonly int _existingNalogId;
    private List<KontoOption> _svaKonta = new();
    private ComboBox? _aktivniKontoCombo;
    private StavkaNaloga? _aktivnaKontoStavka;
    private string _kontoPreIzmene = string.Empty;
    private bool _bezReakcijeNaKonto;
    private bool _internoZatvaranjeListe;
    private string _poslednjiOtkucaniUpit = string.Empty;

    public class KontoOption
    {
        public string BrojKonta { get; set; } = string.Empty;
        public string NazivKonta { get; set; } = string.Empty;
        public string Prikaz => $"{BrojKonta} - {NazivKonta}";

        // Filter se izvršava na svaki pritisak tastera preko celog kontnog plana,
        // pa se mala slova računaju jednom pri učitavanju umesto svaki put.
        public string BrojMala { get; set; } = string.Empty;
        public string NazivMala { get; set; } = string.Empty;
    }

    /// <param name="fokusStavkaNalogaId">
    /// Kad se nalog otvara iz kartice konta (ili kartice partnera), prosleđuje se ID stavke
    /// na koju je korisnik kliknuo da bi se grid odmah pozicionirao baš na taj red.
    /// </param>
    public NalogEditWindow(Nalog? existingNalog = null, bool isReadOnly = false, int? fokusStavkaNalogaId = null)
    {
        InitializeComponent();
        DataContext = this;
        DgStavke.ItemsSource = _stavke;
        _stavke.CollectionChanged += (s, e) => PrikaziSaldo();

        if (existingNalog != null)
        {
            _existingNalogId = existingNalog.NalogId;
            TxtBrojNaloga.Text = existingNalog.BrojNaloga.ToString();
            DpDatum.SelectedDate = existingNalog.DatumNaloga;
            TxtOpisNaloga.Text = existingNalog.Opis;

            Dictionary<int, string> promeneDict = new();
            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;
                using var db = new AccountingDbContext(options);
                promeneDict = db.Promene.ToDictionary(p => p.Sifra, p => p.Opis);
            }
            catch { }

            foreach (var s in existingNalog.Stavke.OrderBy(s => s.RedniBroj))
            {
                string? opisStavke = s.Opis;
                if (s.PromenaKod.HasValue && promeneDict.TryGetValue(s.PromenaKod.Value, out var textIzPromena) && !string.IsNullOrWhiteSpace(textIzPromena))
                {
                    if (string.IsNullOrWhiteSpace(opisStavke) || opisStavke == s.BrojDokumenta)
                    {
                        opisStavke = textIzPromena;
                    }
                }

                _stavke.Add(new StavkaNaloga
                {
                    StavkaNalogaId = s.StavkaNalogaId,
                    RedniBroj = s.RedniBroj,
                    BrojKonta = s.BrojKonta,
                    BrojDokumenta = s.BrojDokumenta,
                    Opis = opisStavke,
                    Duguje = s.Duguje,
                    Potrazuje = s.Potrazuje,
                    PartnerId = s.PartnerId,
                    PromenaKod = s.PromenaKod
                });
            }
            Title = isReadOnly ? $"📖 Pregled proknjiženog naloga #{existingNalog.BrojNaloga} (Samo za čitanje)" : $"Izmena naloga #{existingNalog.BrojNaloga}";
        }
        else
        {
            DpDatum.SelectedDate = DateTime.Now;
            Title = "Novi nalog za knjiženje";
            _ = PredloziSledeciBrojAsync();
        }

        if (isReadOnly)
        {
            TxtBrojNaloga.IsReadOnly = true;
            DpDatum.IsEnabled = false;
            TxtOpisNaloga.IsReadOnly = true;
            DgStavke.IsReadOnly = true;
            BtnDodajStavku.Visibility = Visibility.Collapsed;
            BtnObrisiStavku.Visibility = Visibility.Collapsed;
            BtnSnimi.Visibility = Visibility.Collapsed;
            BtnRasknjizi.Visibility = Visibility.Visible;
            BtnOtkazi.Content = "Zatvori (Esc)";
        }

        PozicionirajNaStavku(fokusStavkaNalogaId);

        _ = UcitajKontaAsync();
        _ = UcitajPartnereAsync();
        _ = UcitajPromeneAsync();
        PrikaziSaldo();
    }

    /// <summary>
    /// Selektuje i skroluje na stavku iz koje je nalog otvoren (klik u kartici konta/partnera).
    /// Redovi DataGrid-a postoje tek posle prvog layout prolaza, pa se pozicioniranje radi
    /// na Loaded, sa niskim prioritetom da ne bi bilo pregaženo inicijalnom selekcijom grida.
    /// </summary>
    private void PozicionirajNaStavku(int? stavkaNalogaId)
    {
        if (stavkaNalogaId is not int id || id <= 0) return;

        var trazena = _stavke.FirstOrDefault(s => s.StavkaNalogaId == id);
        if (trazena == null) return;

        Loaded += (_, _) =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DgStavke.SelectedItem = trazena;
                DgStavke.ScrollIntoView(trazena);
                DgStavke.UpdateLayout();

                if (DgStavke.Columns.Count > 1)
                {
                    DgStavke.CurrentCell = new DataGridCellInfo(trazena, DgStavke.Columns[1]);
                }

                if (DgStavke.ItemContainerGenerator.ContainerFromItem(trazena) is DataGridRow red)
                {
                    red.Focus();
                }
                else
                {
                    DgStavke.Focus();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        };
    }

    public ObservableCollection<string> OpisiPromenaOptions { get; } = new();

    private async Task UcitajPromeneAsync()
    {
        var defaultPromene = new[]
        {
            "Pocetno stanje", "izvod", "isplate", "uplate", "glavna blagajna",
            "cekovi gradjana", "racuni", "putni troskovi", "avans", "cesija",
            "kompenzacija", "licni dohodak", "terenski dodatak", "topli obrok",
            "UGOVOR O PREUZIMANJU DUGA", "ulazi", "trebovanja"
        };

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var promene = await db.Promene.OrderBy(p => p.Sifra).Select(p => p.Opis).ToListAsync();
            
            OpisiPromenaOptions.Clear();
            if (promene != null && promene.Count > 0)
            {
                foreach (var p in promene)
                {
                    if (!string.IsNullOrWhiteSpace(p) && !OpisiPromenaOptions.Contains(p))
                    {
                        OpisiPromenaOptions.Add(p);
                    }
                }
            }

            // Ako u bazi nema unetih promena, ubacujemo podrazumevani šifarnik iz legacy PROMENE.DBF
            if (OpisiPromenaOptions.Count == 0)
            {
                foreach (var p in defaultPromene) OpisiPromenaOptions.Add(p);
            }
        }
        catch
        {
            OpisiPromenaOptions.Clear();
            foreach (var p in defaultPromene) OpisiPromenaOptions.Add(p);
        }
    }

    private async Task UcitajKontaAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new KontaService(db);
            var konta = await service.GetKontaAsync();

            var opcije = konta.Select(k => new KontoOption
            {
                BrojKonta = k.BrojKonta,
                NazivKonta = k.NazivKonta ?? string.Empty,
                BrojMala = k.BrojKonta.ToLowerInvariant(),
                NazivMala = (k.NazivKonta ?? string.Empty).ToLowerInvariant()
            }).ToList();

            _svaKonta = opcije;

            KontoPrikazConverter.Nazivi.Clear();
            foreach (var o in opcije) KontoPrikazConverter.Nazivi[o.BrojKonta] = o.NazivKonta;

            // Konta stižu asinhrono, nakon što je grid već iscrtan — osvežavamo prikaz
            // da bi se uz broj konta video i naziv.
            if (!DgStavke.IsKeyboardFocusWithin) DgStavke.Items.Refresh();
        }
        catch
        {
            // Izbor konta iz padajuće liste je pogodnost
        }
    }

    private async Task UcitajPartnereAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);

            var partneri = new List<Partner> { new() { PartnerId = 0, Naziv = "(bez partnera)" } };
            partneri.AddRange(await service.GetPartneriAsync());
            ColPartner.ItemsSource = partneri;
        }
        catch
        {
            // Izbor partnera je pogodnost — ako ne uspe, stavke se i dalje mogu snimiti bez partnera.
        }
    }

    private async Task PredloziSledeciBrojAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            int max = await db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0;
            TxtBrojNaloga.Text = (max + 1).ToString();
        }
        catch
        {
            // Predlog broja je samo pogodnost — ako ne uspe, korisnik unosi ručno.
        }
    }

    private void BtnPriloziDms_Click(object sender, RoutedEventArgs e)
    {
        if (_existingNalogId == 0)
        {
            MessageBox.Show("Molimo sačuvajte ili knjižite nalog pre prilaganja dokumenta u DMS.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var win = new Views.Dms.DmsWindow(nalogId: _existingNalogId) { Owner = this };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju DMS priloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        DodajNovuStavku();
    }

    private void DodajNovuStavku()
    {
        var novaStavka = new StavkaNaloga
        {
            RedniBroj = _stavke.Count + 1
        };

        // Automatsko prepisivanje Dokumenta, Opisa i Partnera iz prethodnog reda (ako postoji)
        var poslednja = _stavke.LastOrDefault();
        if (poslednja != null)
        {
            novaStavka.BrojDokumenta = poslednja.BrojDokumenta;
            novaStavka.Opis = poslednja.Opis;
            novaStavka.PartnerId = poslednja.PartnerId;
        }

        // Smart auto-balans: Proračun neizbalansirane razlike i predlog na suprotnoj strani
        decimal duguje = _stavke.Sum(s => s.Duguje);
        decimal potrazuje = _stavke.Sum(s => s.Potrazuje);
        decimal razlika = duguje - potrazuje;

        if (razlika > 0)
        {
            novaStavka.Potrazuje = razlika;
        }
        else if (razlika < 0)
        {
            novaStavka.Duguje = Math.Abs(razlika);
        }

        _stavke.Add(novaStavka);
        DgStavke.SelectedItem = novaStavka;
        DgStavke.ScrollIntoView(novaStavka);
        PrikaziSaldo();

        // Fokusiramo ćeliju Konto nove stavke radi trenutnog unosa
        Dispatcher.BeginInvoke(new Action(() =>
        {
            DgStavke.Focus();
            if (DgStavke.Columns.Count > 1)
            {
                var cell = new DataGridCellInfo(novaStavka, DgStavke.Columns[1]);
                DgStavke.CurrentCell = cell;
                DgStavke.BeginEdit();
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void DgStavke_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Insert)
        {
            DodajNovuStavku();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control)
        {
            BtnObrisiStavku_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && UKontoCeliji)
        {
            // Bez veze podataka na ćeliji konta DataGrid nema šta da vrati, pa staru
            // vrednost vraćamo sami.
            if (_aktivnaKontoStavka != null) _aktivnaKontoStavka.BrojKonta = _kontoPreIzmene;
            OtkaziUnosKonta();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            // Must run here (DataGrid-level PreviewKeyDown), not on the ComboBox
            // itself: DataGridCell swallows Enter/Tab internally (BeginEdit/CommitEdit
            // bookkeeping) before the tunnel ever reaches a cell's editing element, so
            // a handler attached directly to the ComboBox never sees these keys.
            if (UKontoCeliji)
            {
                PrihvatiUnetiKonto();

                if (e.Key == Key.Enter)
                {
                    // Enter u ćeliji konta radi isto što i Tab: potvrđuje izabrani
                    // konto i prelazi na sledeću kolonu.
                    var stavka = _aktivnaKontoStavka;
                    ZavrsiUnosKonta();
                    PredjiNaKolonuPosleKonta(stavka);
                    e.Handled = true;
                    return;
                }
            }

            if (DgStavke.CurrentCell.Column != null && DgStavke.SelectedItem is StavkaNaloga currentStavka)
            {
                var columnIndex = DgStavke.Columns.IndexOf(DgStavke.CurrentCell.Column);
                bool isLastColumn = columnIndex >= DgStavke.Columns.Count - 2; // Potražuje (5) or Partner (6)
                bool isLastRow = _stavke.IndexOf(currentStavka) == _stavke.Count - 1;

                if (isLastColumn && isLastRow)
                {
                    DgStavke.CommitEdit();
                    DodajNovuStavku();
                    e.Handled = true;
                }
            }
        }
    }

    private void BtnPretragaKonta_Click(object sender, RoutedEventArgs e)
    {
        OtvoriPretraguKonta();
    }

    private async void BtnSifarnikOpisa_Click(object sender, RoutedEventArgs e)
    {
        var win = new PromeneWindow { Owner = this };
        win.ShowDialog();
        await UcitajPromeneAsync();
    }

    private void BtnPomoc_Click(object sender, RoutedEventArgs e)
    {
        OtvoriPomoc();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            OtvoriPomoc();
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            OtvoriPretraguKonta();
            e.Handled = true;
        }
        else if (e.Key == Key.Insert)
        {
            DodajNovuStavku();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OtvoriPomoc()
    {
        var pomoc = new NalogHelpWindow { Owner = this };
        pomoc.ShowDialog();
    }

    private void OtvoriPretraguKonta()
    {
        // Pretraga se najčešće poziva iz ćelije konta koja je u režimu izmene: ono što
        // je do tada otkucano prenosi se u pretragu, a grid mora izaći iz transakcije
        // izmene da bi kasniji Items.Refresh() prošao.
        string initialSearch = SamoBrojKonta(TekstUnetogKonta());
        if (_aktivniKontoCombo != null) OtkaziUnosKonta();
        DgStavke.CommitEdit(DataGridEditingUnit.Row, true);

        if (initialSearch.Length == 0 && DgStavke.SelectedItem is StavkaNaloga selektovana && !string.IsNullOrWhiteSpace(selektovana.BrojKonta))
        {
            initialSearch = selektovana.BrojKonta;
        }

        var picker = new KontoPickerWindow(initialSearch) { Owner = this };
        if (picker.ShowDialog() == true && picker.IzabraniKonto != null)
        {
            var izabran = picker.IzabraniKonto;
            StavkaNaloga targetStavka;

            if (DgStavke.SelectedItem is StavkaNaloga current)
            {
                targetStavka = current;
            }
            else
            {
                targetStavka = new StavkaNaloga { RedniBroj = _stavke.Count + 1 };
                _stavke.Add(targetStavka);
                DgStavke.SelectedItem = targetStavka;
            }

            targetStavka.BrojKonta = izabran.BrojKonta;
            DgStavke.Items.Refresh();
            PrikaziSaldo();
        }
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is StavkaNaloga selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            DgStavke.Items.Refresh();
            PrikaziSaldo();
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        // Izlazak iz ćelije konta mišem (klik na drugu ćeliju) takođe je potvrda unosa.
        if (e.Column == ColKonto && e.EditAction == DataGridEditAction.Commit)
        {
            PrihvatiUnetiKonto();
        }

        Dispatcher.BeginInvoke(new Action(PrikaziSaldo));
    }

    private bool UKontoCeliji => _aktivniKontoCombo != null && DgStavke.CurrentCell.Column == ColKonto;

    private void KontoCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox cb) return;

        _aktivniKontoCombo = cb;
        _aktivnaKontoStavka = cb.DataContext as StavkaNaloga;
        _kontoPreIzmene = _aktivnaKontoStavka?.BrojKonta ?? string.Empty;
        _poslednjiOtkucaniUpit = _kontoPreIzmene;

        _bezReakcijeNaKonto = true;
        cb.ItemsSource = FiltrirajKonta(_kontoPreIzmene);
        cb.Text = _kontoPreIzmene;
        _bezReakcijeNaKonto = false;

        // Klik mišem na stavku liste hvatamo direktno (a ne preko SelectionChanged /
        // DropDownClosed) da bismo znali da je izbor zaista potvrđen mišem, a ne samo
        // označen strelicama.
        cb.RemoveHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(KontoCombo_PreviewMouseLeftButtonUp));
        cb.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(KontoCombo_PreviewMouseLeftButtonUp), true);

        if (cb.Template.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.TextChanged -= ComboKonto_TextChanged;
            tb.TextChanged += ComboKonto_TextChanged;
            tb.PreviewKeyDown -= ComboKonto_PreviewKeyDown;
            tb.PreviewKeyDown += ComboKonto_PreviewKeyDown;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.SelectAll();
                tb.Focus();
                PostaviPadajucuListu(cb, cb.Items.Count > 0);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void KontoCombo_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox cb) return;

        cb.RemoveHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(KontoCombo_PreviewMouseLeftButtonUp));
        if (cb.Template?.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.TextChanged -= ComboKonto_TextChanged;
            tb.PreviewKeyDown -= ComboKonto_PreviewKeyDown;
        }

        if (ReferenceEquals(_aktivniKontoCombo, cb))
        {
            _aktivniKontoCombo = null;
            _aktivnaKontoStavka = null;
        }
    }

    private void ComboKonto_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;

        if (e.Key == Key.Down)
        {
            if (!cb.IsDropDownOpen)
            {
                PostaviPadajucuListu(cb, true);
                e.Handled = true;
                return;
            }

            if (cb.Items.Count > 0)
            {
                int nextIndex = cb.SelectedIndex + 1;
                if (nextIndex < cb.Items.Count)
                {
                    cb.SelectedIndex = nextIndex;
                    if (cb.SelectedItem is KontoOption izabran)
                    {
                        SkrolujDoStavke(cb, izabran);
                        UpisiKonto(izabran.BrojKonta);
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
                    if (cb.SelectedItem is KontoOption izabran)
                    {
                        SkrolujDoStavke(cb, izabran);
                        UpisiKonto(izabran.BrojKonta);
                    }
                }
                else if (cb.SelectedIndex == 0)
                {
                    cb.SelectedIndex = -1;
                    if (!string.IsNullOrEmpty(_poslednjiOtkucaniUpit))
                    {
                        _bezReakcijeNaKonto = true;
                        tb.Text = _poslednjiOtkucaniUpit;
                        tb.CaretIndex = _poslednjiOtkucaniUpit.Length;
                        _bezReakcijeNaKonto = false;
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
                if (cb.SelectedItem is KontoOption izabran)
                {
                    SkrolujDoStavke(cb, izabran);
                    UpisiKonto(izabran.BrojKonta);
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
                if (cb.SelectedItem is KontoOption izabran)
                {
                    SkrolujDoStavke(cb, izabran);
                    UpisiKonto(izabran.BrojKonta);
                }
                e.Handled = true;
            }
        }
    }

    private void ComboKonto_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_bezReakcijeNaKonto) return;
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;

        // Tekst se promenio zato što je stavka izabrana u listi (ComboBox upisuje
        // "broj - naziv"), a ne zato što korisnik kuca — tada se ne filtrira.
        if (cb.SelectedItem is KontoOption izabran && tb.Text == izabran.Prikaz) return;

        string tekst = tb.Text;
        int kursor = tb.CaretIndex;

        // Pamti ono što je korisnik kucao pre izbora strelicom
        if (cb.SelectedItem == null)
        {
            _poslednjiOtkucaniUpit = tekst;
        }

        var filtrirano = FiltrirajKonta(tekst);

        _bezReakcijeNaKonto = true;
        cb.ItemsSource = filtrirano;
        // Zamena ItemsSource poništava selekciju, a ComboBox tada ume da obriše i
        // otkucani tekst — vraćamo ga zajedno sa pozicijom kursora.
        if (tb.Text != tekst)
        {
            tb.Text = tekst;
            tb.CaretIndex = Math.Min(kursor, tekst.Length);
        }
        _bezReakcijeNaKonto = false;

        PostaviPadajucuListu(cb, filtrirano.Count > 0);
    }

    private List<KontoOption> FiltrirajKonta(string upit)
    {
        string q = upit.Trim().ToLowerInvariant();
        var rezultat = new List<KontoOption>(MaxPrikazanihKonta);

        if (q.Length == 0)
        {
            for (int i = 0; i < _svaKonta.Count && rezultat.Count < MaxPrikazanihKonta; i++)
            {
                rezultat.Add(_svaKonta[i]);
            }
            return rezultat;
        }

        // Unos obično počinje brojem konta, pa konta koja počinju upitom idu prva.
        foreach (var k in _svaKonta)
        {
            if (k.BrojMala.StartsWith(q, StringComparison.Ordinal))
            {
                rezultat.Add(k);
                if (rezultat.Count >= MaxPrikazanihKonta) return rezultat;
            }
        }

        foreach (var k in _svaKonta)
        {
            if (k.BrojMala.StartsWith(q, StringComparison.Ordinal)) continue;
            if (k.BrojMala.Contains(q, StringComparison.Ordinal) || k.NazivMala.Contains(q, StringComparison.Ordinal))
            {
                rezultat.Add(k);
                if (rezultat.Count >= MaxPrikazanihKonta) return rezultat;
            }
        }

        return rezultat;
    }

    private void PostaviPadajucuListu(ComboBox cb, bool otvorena)
    {
        if (cb.IsDropDownOpen == otvorena) return;

        _internoZatvaranjeListe = true;
        cb.IsDropDownOpen = otvorena;
        _internoZatvaranjeListe = false;
    }

    private void KontoCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_bezReakcijeNaKonto || _internoZatvaranjeListe) return;
        if (sender is not ComboBox cb || cb.SelectedItem is not KontoOption izabran) return;

        // Pokriva i izbor strelicama u otvorenoj listi; potvrda (Enter/Tab/klik)
        // samo zatvara ćeliju.
        UpisiKonto(izabran.BrojKonta);
        SkrolujDoStavke(cb, izabran);
    }

    private static void SkrolujDoStavke(ComboBox cb, object item)
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

    private void KontoCombo_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject izvor) return;

        var stavkaListe = ItemsControl.ContainerFromElement(sender as ComboBox, izvor) as ComboBoxItem;
        if (stavkaListe?.DataContext is not KontoOption izabran) return;

        UpisiKonto(izabran.BrojKonta);

        // ComboBox tek posle ovog događaja završava sopstvenu obradu klika, pa se
        // zatvaranje ćelije odlaže da mu se ne bi izmakao editor ispod ruke.
        var stavka = _aktivnaKontoStavka;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ZavrsiUnosKonta();
            PredjiNaKolonuPosleKonta(stavka);
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void UpisiKonto(string brojKonta)
    {
        if (_aktivnaKontoStavka != null) _aktivnaKontoStavka.BrojKonta = brojKonta;
    }

    /// <summary>
    /// Prihvata ono što je korisnik uneo u ćeliju konta: izabranu stavku iz liste,
    /// a ako izbora nema — otkucani tekst se razrešava u kontnom planu.
    /// </summary>
    private void PrihvatiUnetiKonto()
    {
        var cb = _aktivniKontoCombo;
        if (cb == null) return;

        if (cb.SelectedItem is KontoOption izabran)
        {
            UpisiKonto(izabran.BrojKonta);
            return;
        }

        string uneto = TekstUnetogKonta();
        if (uneto.Length == 0) return;

        var poklapanje = NadjiKonto(uneto);
        if (poklapanje != null) UpisiKonto(poklapanje.BrojKonta);
    }

    private string TekstUnetogKonta()
    {
        var cb = _aktivniKontoCombo;
        if (cb == null) return string.Empty;

        return ((cb.Template?.FindName("PART_EditableTextBox", cb) as TextBox)?.Text ?? cb.Text ?? string.Empty).Trim();
    }

    // Ako je u polju ostao prikaz oblika "2413/1 - POSEBAN TEKUCI RACUN",
    // upotrebljiv je samo broj konta ispred crte.
    private static string SamoBrojKonta(string tekst)
    {
        int crta = tekst.IndexOf(" - ", StringComparison.Ordinal);
        return crta > 0 ? tekst[..crta].Trim() : tekst.Trim();
    }

    private KontoOption? NadjiKonto(string uneto)
    {
        string broj = SamoBrojKonta(uneto);

        var poGodak = _svaKonta.FirstOrDefault(k => k.BrojKonta.Equals(broj, StringComparison.OrdinalIgnoreCase));
        if (poGodak != null) return poGodak;

        var kandidati = FiltrirajKonta(uneto);
        return kandidati.FirstOrDefault();
    }

    private void ZavrsiUnosKonta()
    {
        var cb = _aktivniKontoCombo;
        if (cb != null) PostaviPadajucuListu(cb, false);

        DgStavke.CommitEdit(DataGridEditingUnit.Cell, true);
        _aktivniKontoCombo = null;
        _aktivnaKontoStavka = null;
    }

    private void OtkaziUnosKonta()
    {
        var cb = _aktivniKontoCombo;
        if (cb != null) PostaviPadajucuListu(cb, false);

        DgStavke.CancelEdit(DataGridEditingUnit.Cell);
        _aktivniKontoCombo = null;
        _aktivnaKontoStavka = null;
    }

    private void PredjiNaKolonuPosleKonta(StavkaNaloga? stavka)
    {
        stavka ??= DgStavke.SelectedItem as StavkaNaloga;
        if (stavka == null) return;

        int indeks = DgStavke.Columns.IndexOf(ColKonto);
        if (indeks < 0 || indeks + 1 >= DgStavke.Columns.Count) return;

        DgStavke.CurrentCell = new DataGridCellInfo(stavka, DgStavke.Columns[indeks + 1]);
        DgStavke.BeginEdit();
    }

    private void PrikaziSaldo()
    {
        decimal duguje = _stavke.Sum(s => s.Duguje);
        decimal potrazuje = _stavke.Sum(s => s.Potrazuje);

        TxtZbirDuguje.Text = duguje.ToString("N2");
        TxtZbirPotrazuje.Text = potrazuje.ToString("N2");

        if (_stavke.Count == 0)
        {
            TxtBalansStatus.Text = "⚠️ Nema stavki";
            BorderBalans.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7));
            return;
        }

        bool balans = Math.Abs(duguje - potrazuje) < 0.01m;
        if (balans)
        {
            TxtBalansStatus.Text = "✅ Nalog je u ravnoteži";
            BorderBalans.Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xFD, 0xF5));
        }
        else
        {
            TxtBalansStatus.Text = $"⚠️ Razlika: {(duguje - potrazuje):N2}";
            BorderBalans.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        // Klik na dugme ne mora da zatvori ćeliju koja je u režimu izmene, pa se
        // poslednji unos (najčešće konto) potvrđuje pre provere i snimanja.
        if (UKontoCeliji) PrihvatiUnetiKonto();
        DgStavke.CommitEdit(DataGridEditingUnit.Row, true);

        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga))
        {
            MessageBox.Show("Unesite ispravan broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_stavke.Count == 0)
        {
            MessageBox.Show("Dodajte bar jednu stavku naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var s in _stavke)
        {
            if (string.IsNullOrWhiteSpace(s.BrojKonta))
            {
                MessageBox.Show("Svaka stavka mora imati unet konto.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            Nalog nalog;
            if (_existingNalogId != 0)
            {
                nalog = await db.Nalozi.Include(n => n.Stavke).FirstAsync(n => n.NalogId == _existingNalogId);
                db.StavkeNaloga.RemoveRange(nalog.Stavke);
                nalog.Stavke.Clear();
            }
            else
            {
                nalog = new Nalog();
            }

            nalog.BrojNaloga = brojNaloga;
            nalog.DatumNaloga = DpDatum.SelectedDate ?? DateTime.Now;
            nalog.Opis = TxtOpisNaloga.Text.Trim();

            int red = 1;
            foreach (var s in _stavke)
            {
                nalog.Stavke.Add(new StavkaNaloga
                {
                    RedniBroj = red++,
                    BrojKonta = s.BrojKonta.Trim(),
                    BrojDokumenta = s.BrojDokumenta,
                    Opis = s.Opis,
                    Duguje = s.Duguje,
                    Potrazuje = s.Potrazuje,
                    PartnerId = s.PartnerId is null or 0 ? null : s.PartnerId
                });
            }

            await service.SaveNalogAsync(nalog);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (_existingNalogId <= 0) return;

        var res = MessageBox.Show(
            $"Da li ste sigurni da želite da rasknjižite nalog #{TxtBrojNaloga.Text} radi izmene?",
            "Potvrda rasknjižavanja",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;

        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);
            await service.RasknjiziNalogAsync(_existingNalogId);

            PrebaciURezimIzmene();
            MessageBox.Show($"Nalog #{TxtBrojNaloga.Text} je uspešno rasknjižen i omogućen za izmenu.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrebaciURezimIzmene()
    {
        Title = $"Izmena naloga #{TxtBrojNaloga.Text}";
        TxtBrojNaloga.IsReadOnly = false;
        DpDatum.IsEnabled = true;
        TxtOpisNaloga.IsReadOnly = false;
        DgStavke.IsReadOnly = false;
        BtnDodajStavku.Visibility = Visibility.Visible;
        BtnObrisiStavku.Visibility = Visibility.Visible;
        BtnSnimi.Visibility = Visibility.Visible;
        BtnRasknjizi.Visibility = Visibility.Collapsed;
        BtnOtkazi.Content = "Otkaži (Esc)";
    }
}

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
    private readonly ObservableCollection<StavkaNaloga> _stavke = new();
    private readonly int _existingNalogId;
    private List<KontoOption> _svaKonta = new();
    private ComboBox? _aktivniKontoCombo;

    public class KontoOption
    {
        public string BrojKonta { get; set; } = string.Empty;
        public string NazivKonta { get; set; } = string.Empty;
        public string Prikaz => $"{BrojKonta} - {NazivKonta}";
    }

    public NalogEditWindow(Nalog? existingNalog = null)
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
            Title = $"Izmena naloga #{existingNalog.BrojNaloga}";
        }
        else
        {
            DpDatum.SelectedDate = DateTime.Now;
            Title = "Novi nalog za knjiženje";
            _ = PredloziSledeciBrojAsync();
        }

        _ = UcitajKontaAsync();
        _ = UcitajPartnereAsync();
        _ = UcitajPromeneAsync();
        PrikaziSaldo();
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
                NazivKonta = k.NazivKonta
            }).ToList();

            _svaKonta = opcije;
            ColKonto.ItemsSource = opcije;
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
        else if (e.Key == Key.Enter || e.Key == Key.Tab)
        {
            // Must run here (DataGrid-level PreviewKeyDown), not on the ComboBox
            // itself: DataGridCell swallows Enter/Tab internally (BeginEdit/CommitEdit
            // bookkeeping) before the tunnel ever reaches a cell's editing element, so
            // a handler attached directly to the ComboBox never sees these keys.
            RazresiUnetiKontoPriPotvrdi();

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
        string initialSearch = "";
        if (DgStavke.SelectedItem is StavkaNaloga selektovana && !string.IsNullOrWhiteSpace(selektovana.BrojKonta))
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
        Dispatcher.BeginInvoke(new Action(PrikaziSaldo));
    }

    private void DgStavke_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.Column != ColKonto || e.EditingElement is not ComboBox cb) return;

        cb.IsEditable = true;
        cb.IsTextSearchEnabled = false;
        cb.StaysOpenOnEdit = true;
        cb.ItemsSource = _svaKonta;
        _aktivniKontoCombo = cb;

        cb.ApplyTemplate();
        if (cb.Template.FindName("PART_EditableTextBox", cb) is TextBox tb)
        {
            tb.TextChanged += ComboKonto_TextChanged;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                tb.SelectAll();
                tb.Focus();
                cb.IsDropDownOpen = true;
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void ComboKonto_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb || tb.TemplatedParent is not ComboBox cb) return;

        string query = tb.Text.Trim().ToLower();
        var filtrirano = string.IsNullOrEmpty(query)
            ? _svaKonta
            : _svaKonta.Where(k => k.BrojKonta.ToLower().Contains(query) || k.NazivKonta.ToLower().Contains(query)).ToList();

        cb.ItemsSource = filtrirano;
        cb.IsDropDownOpen = filtrirano.Count > 0;
    }

    // Editable ComboBox's own Text/SelectedItem sync becomes unreliable once
    // ItemsSource is swapped on every keystroke (WPF quirk), so on commit we
    // resolve the typed text against _svaKonta ourselves instead of trusting cb.Text/SelectedItem.
    private void RazresiUnetiKontoPriPotvrdi()
    {
        var cb = _aktivniKontoCombo;
        if (cb == null || DgStavke.CurrentCell.Column != ColKonto) return;
        if (cb.SelectedItem is KontoOption) return;

        string typed = (cb.Template.FindName("PART_EditableTextBox", cb) as TextBox)?.Text?.Trim() ?? string.Empty;
        if (typed.Length == 0) return;

        var poklapanje = _svaKonta.FirstOrDefault(k => k.BrojKonta.Equals(typed, StringComparison.OrdinalIgnoreCase))
            ?? _svaKonta.FirstOrDefault(k => k.BrojKonta.ToLower().Contains(typed.ToLower()) || k.NazivKonta.ToLower().Contains(typed.ToLower()));

        // Not cb.SelectedItem = poklapanje: the ComboBox's ItemsSource was just
        // swapped by the last keystroke's filter, and WPF's ItemContainerGenerator
        // for the popup's (separate visual root) content hasn't caught up yet —
        // assigning SelectedItem right after silently no-ops (confirmed via logging:
        // SelectedItem read back as null immediately after the assignment). Go
        // straight to the row's model instead of fighting the ComboBox's selection
        // machinery; nothing else writes BrojKonta here since we never touch
        // SelectedValue, so there's no competing binding push to race against.
        if (poklapanje != null && DgStavke.SelectedItem is StavkaNaloga trenutna)
        {
            trenutna.BrojKonta = poklapanje.BrojKonta;
        }
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
}

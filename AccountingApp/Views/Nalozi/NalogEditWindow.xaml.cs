using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AccountingApp.Views.Konta;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Nalozi;

public partial class NalogEditWindow : Window
{
    private readonly ObservableCollection<StavkaNaloga> _stavke = new();
    private readonly int _existingNalogId;

    public class KontoOption
    {
        public string BrojKonta { get; set; } = string.Empty;
        public string NazivKonta { get; set; } = string.Empty;
        public string Prikaz => $"{BrojKonta} - {NazivKonta}";
    }

    public NalogEditWindow(Nalog? existingNalog = null)
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        _stavke.CollectionChanged += (s, e) => PrikaziSaldo();

        if (existingNalog != null)
        {
            _existingNalogId = existingNalog.NalogId;
            TxtBrojNaloga.Text = existingNalog.BrojNaloga;
            DpDatum.SelectedDate = existingNalog.DatumNaloga;
            TxtOpisNaloga.Text = existingNalog.Opis;
            foreach (var s in existingNalog.Stavke.OrderBy(s => s.RedniBroj))
            {
                _stavke.Add(new StavkaNaloga
                {
                    RedniBroj = s.RedniBroj,
                    BrojKonta = s.BrojKonta,
                    BrojDokumenta = s.BrojDokumenta,
                    Opis = s.Opis,
                    Duguje = s.Duguje,
                    Potrazuje = s.Potrazuje,
                    PartnerId = s.PartnerId
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
        PrikaziSaldo();
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

            var brojevi = await db.Nalozi.Select(n => n.BrojNaloga).ToListAsync();
            int max = 0;
            foreach (var b in brojevi)
            {
                if (int.TryParse(b, out var v) && v > max) max = v;
            }
            TxtBrojNaloga.Text = (max + 1).ToString();
        }
        catch
        {
            // Predlog broja je samo pogodnost — ako ne uspe, korisnik unosi ručno.
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
        if (string.IsNullOrWhiteSpace(TxtBrojNaloga.Text))
        {
            MessageBox.Show("Unesite broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            nalog.BrojNaloga = TxtBrojNaloga.Text.Trim();
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

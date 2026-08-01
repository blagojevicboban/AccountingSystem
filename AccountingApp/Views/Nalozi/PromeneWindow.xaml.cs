using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingApp.Views.Pomoc;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Nalozi;

public partial class PromeneWindow : Window
{
    private List<Promena> _allPromene = new();
    private Promena? _selectedPromena;

    public PromeneWindow()
    {
        InitializeComponent();
        Loaded += PromeneWindow_Loaded;
    }

    private async void PromeneWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await UcitajPromeneAsync();
    }

    private async Task UcitajPromeneAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            _allPromene = await db.Promene.OrderBy(p => p.Sifra).ToListAsync();
            
            // Ako baza nema promene, napuni podrazumevani šifarnik
            if (_allPromene.Count == 0)
            {
                var defaultPromene = new[]
                {
                    "Pocetno stanje", "izvod", "isplate", "uplate", "glavna blagajna",
                    "cekovi gradjana", "racuni", "putni troskovi", "avans", "cesija",
                    "kompenzacija", "licni dohodak", "terenski dodatak", "topli obrok",
                    "UGOVOR O PREUZIMANJU DUGA", "ulazi", "trebovanja"
                };

                int sifra = 1;
                foreach (var p in defaultPromene)
                {
                    db.Promene.Add(new Promena { Sifra = sifra++, Opis = p });
                }
                await db.SaveChangesAsync();
                _allPromene = await db.Promene.OrderBy(p => p.Sifra).ToListAsync();
            }

            PrimeniFilter();
            OčistiFormu();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju šifarnika promena: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrimeniFilter()
    {
        var query = TxtPretraga.Text?.Trim().ToLower() ?? "";
        var filtrirane = _allPromene.Where(p =>
            string.IsNullOrWhiteSpace(query) ||
            p.Sifra.ToString().Contains(query) ||
            p.Opis.ToLower().Contains(query)
        ).ToList();

        DgPromene.ItemsSource = filtrirane;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        PrimeniFilter();
    }

    private void DgPromene_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgPromene.SelectedItem is Promena selektovana)
        {
            _selectedPromena = selektovana;
            TxtSifra.Text = selektovana.Sifra.ToString();
            TxtOpis.Text = selektovana.Opis;
            BtnObrisi.IsEnabled = true;
        }
        else
        {
            OčistiFormu();
        }
    }

    private void OčistiFormu()
    {
        _selectedPromena = null;
        int maxSifra = _allPromene.Count > 0 ? _allPromene.Max(p => p.Sifra) + 1 : 1;
        TxtSifra.Text = maxSifra.ToString();
        TxtOpis.Text = "";
        BtnObrisi.IsEnabled = false;
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtSifra.Text.Trim(), out var sifra) || sifra <= 0)
        {
            MessageBox.Show("Molimo unesite ispravnu numeričku šifru.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string opis = TxtOpis.Text.Trim();
        if (string.IsNullOrWhiteSpace(opis))
        {
            MessageBox.Show("Molimo unesite opis promene.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            if (_selectedPromena != null)
            {
                var p = await db.Promene.FindAsync(_selectedPromena.PromenaId);
                if (p != null)
                {
                    p.Sifra = sifra;
                    p.Opis = opis;
                }
            }
            else
            {
                db.Promene.Add(new Promena { Sifra = sifra, Opis = opis });
            }

            await db.SaveChangesAsync();
            await UcitajPromeneAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju opisa promene: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPromena == null) return;

        if (MessageBox.Show($"Da li ste sigurni da želite obrisati opis '{_selectedPromena.Opis}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var p = await db.Promene.FindAsync(_selectedPromena.PromenaId);
            if (p != null)
            {
                db.Promene.Remove(p);
                await db.SaveChangesAsync();
            }

            await UcitajPromeneAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju opisa promene: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = true;
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
            "📝 Pomoć — Šifarnik opisa promena",
            "Standardni opisi knjižnih promena za brži unos stavki naloga.",
            new (string, string)[]
            {
                ("Esc", "Zatvara prozor."),
            },
            "Šifra i opis definisani ovde dostupni su kao brzi izbor (F2) prilikom unosa opisa stavke u Glavnoj knjizi."
        ) { Owner = this }.ShowDialog();
    }
}

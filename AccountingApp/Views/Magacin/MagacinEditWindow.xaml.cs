using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingApp.Views.Pomoc;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class MagacinEditWindow : Window
{
    private readonly AccountingData.Models.Magacin? _existingMagacin;

    public MagacinEditWindow(AccountingData.Models.Magacin? existingMagacin = null)
    {
        InitializeComponent();
        ContextHelpFix.UkloniDugmeZaPomoc(this);
        _existingMagacin = existingMagacin;

        if (_existingMagacin != null)
        {
            TxtNaslov.Text = "✏️ Izmena računopolagača";
            TxtSifra.Text = _existingMagacin.SifraMagacina;
            TxtSifra.IsReadOnly = true; // Šifra se ne menja u izmeni
            TxtNaziv.Text = _existingMagacin.NazivMagacina;
            TxtOdgovornoLice.Text = _existingMagacin.OdgovornoLice ?? "";

            foreach (ComboBoxItem item in CmbVrsta.Items)
            {
                if (string.Equals(item.Content.ToString(), _existingMagacin.VrstaMagacina, StringComparison.OrdinalIgnoreCase))
                {
                    CmbVrsta.SelectedItem = item;
                    break;
                }
            }
        }
        else
        {
            TxtNaslov.Text = "➕ Novi računopolagač / magacin";
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string sifra = TxtSifra.Text.Trim();
        string naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Molimo unesite šifru magacina.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSifra.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Molimo unesite naziv računopolagača / magacina.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNaziv.Focus();
            return;
        }

        string odgovornoLice = TxtOdgovornoLice.Text.Trim();
        string vrsta = (CmbVrsta.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Veleprodaja";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);

            if (_existingMagacin == null)
            {
                // Unos novog magacina
                bool vecPostoji = await db.Magacini.AnyAsync(m => m.SifraMagacina == sifra);
                if (vecPostoji)
                {
                    MessageBox.Show($"Magacin sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtSifra.Focus();
                    return;
                }

                db.Magacini.Add(new AccountingData.Models.Magacin
                {
                    SifraMagacina = sifra,
                    NazivMagacina = naziv,
                    OdgovornoLice = string.IsNullOrWhiteSpace(odgovornoLice) ? null : odgovornoLice,
                    VrstaMagacina = vrsta
                });
            }
            else
            {
                // Izmena postojećeg magacina
                var m = await db.Magacini.FirstOrDefaultAsync(x => x.MagacinId == _existingMagacin.MagacinId);
                if (m != null)
                {
                    m.NazivMagacina = naziv;
                    m.OdgovornoLice = string.IsNullOrWhiteSpace(odgovornoLice) ? null : odgovornoLice;
                    m.VrstaMagacina = vrsta;
                }
            }

            await db.SaveChangesAsync();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju magacina:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
            "🏢 Pomoć — Računopolagač / Magacin",
            "Šifarnik magacina (skladišta) i računopolagača korišćen u svim robnim i materijalnim dokumentima.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
            },
            "Vrsta magacina (Veleprodaja/Maloprodaja/Proizvodnja/Tranzit) utiče na to koje kalkulacije i dokumenti su dostupni za taj magacin."
        ) { Owner = this }.ShowDialog();
    }
}

using System.Windows;
using System.Windows.Input;
using AccountingApp.Views.Pomoc;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class MaterijalEditWindow : Window
{
    private readonly Materijal? _existingMaterijal;

    public MaterijalEditWindow(Materijal? existingMaterijal = null)
    {
        InitializeComponent();
        _existingMaterijal = existingMaterijal;
        LoadData();
    }

    private void LoadData()
    {
        if (_existingMaterijal != null)
        {
            TxtNaslov.Text = "✏️ Izmena materijala";
            TxtSifra.Text = _existingMaterijal.SifraArtikla;
            TxtSifra.IsReadOnly = true; // Šifra se ne menja
            TxtNaziv.Text = _existingMaterijal.Naziv;
            CmbJedinicaMere.Text = _existingMaterijal.JedinicaMere ?? "kom";
            TxtPakovanje.Text = _existingMaterijal.Pakovanje ?? "";
        }
        else
        {
            TxtNaslov.Text = "➕ Novi materijal";
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string sifra = TxtSifra.Text.Trim();
        string naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Molimo unesite šifru materijala.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSifra.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Molimo unesite naziv materijala.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNaziv.Focus();
            return;
        }

        string jm = CmbJedinicaMere.Text.Trim();
        if (string.IsNullOrWhiteSpace(jm)) jm = "kom";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);

            if (_existingMaterijal == null)
            {
                bool vecPostoji = await db.Materijali.AnyAsync(a => a.SifraArtikla == sifra);
                if (vecPostoji)
                {
                    MessageBox.Show($"Materijal sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtSifra.Focus();
                    return;
                }

                db.Materijali.Add(new Materijal
                {
                    SifraArtikla = sifra,
                    Naziv = naziv,
                    JedinicaMere = jm,
                    Pakovanje = string.IsNullOrWhiteSpace(TxtPakovanje.Text) ? null : TxtPakovanje.Text.Trim()
                });
            }
            else
            {
                var a = await db.Materijali.FirstOrDefaultAsync(x => x.MaterijalId == _existingMaterijal.MaterijalId);
                if (a != null)
                {
                    a.Naziv = naziv;
                    a.JedinicaMere = jm;
                    a.Pakovanje = string.IsNullOrWhiteSpace(TxtPakovanje.Text) ? null : TxtPakovanje.Text.Trim();
                }
            }

            await db.SaveChangesAsync();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju materijala:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
            "🧱 Pomoć — Materijal",
            "Šifarnik materijala korišćen u ulazima, trebovanjima i primopredajama.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
            },
            "Šifra materijala mora biti jedinstvena. Jedinica mere se koristi u svim kasnijim dokumentima (ulaz, trebovanje, primopredaja, popis) vezanim za ovaj materijal."
        ) { Owner = this }.ShowDialog();
    }
}

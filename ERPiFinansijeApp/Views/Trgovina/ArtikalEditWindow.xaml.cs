using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeApp.Views.Pomoc;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Trgovina;

public partial class ArtikalEditWindow : Window
{
    private readonly Artikal? _existingArtikal;

    public ArtikalEditWindow(Artikal? existingArtikal = null)
    {
        InitializeComponent();
        ContextHelpFix.UkloniDugmeZaPomoc(this);
        _existingArtikal = existingArtikal;
        LoadData();
    }

    private async void LoadData()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite($"Data Source={AppConfig.DbPath}")
            .Options;
        using var db = new AccountingDbContext(options);

        var tarife = await db.PoreskeTarife.ToListAsync();
        tarife = tarife.OrderBy(t => int.Parse(t.TarifniBroj)).ToList();
        tarife.Insert(0, new PoreskaTarifa { TarifniBroj = "" });
        CmbTarifniBroj.ItemsSource = tarife;

        if (_existingArtikal != null)
        {
            TxtNaslov.Text = "✏️ Izmena artikla";
            TxtSifra.Text = _existingArtikal.SifraArtikla;
            TxtSifra.IsReadOnly = true; // Šifra se ne menja
            TxtNaziv.Text = _existingArtikal.Naziv;
            CmbJedinicaMere.Text = _existingArtikal.JedinicaMere ?? "kom";
            TxtPakovanje.Text = _existingArtikal.Pakovanje ?? "";
            CmbTarifniBroj.SelectedValue = _existingArtikal.TarifniBroj ?? "";
            TxtNabavnaCena.Text = _existingArtikal.NabavnaCena.ToString("N2");
            TxtProdajnaCena.Text = _existingArtikal.ProdajnaCena.ToString("N2");
        }
        else
        {
            TxtNaslov.Text = "➕ Novi artikal / roba";
            TxtNabavnaCena.Text = "0,00";
            TxtProdajnaCena.Text = "0,00";
            CmbTarifniBroj.SelectedIndex = 0;
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string sifra = TxtSifra.Text.Trim();
        string naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Molimo unesite šifru artikla.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSifra.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Molimo unesite naziv artikla.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNaziv.Focus();
            return;
        }

        string jm = CmbJedinicaMere.Text.Trim();
        if (string.IsNullOrWhiteSpace(jm)) jm = "kom";

        decimal.TryParse(TxtNabavnaCena.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nabCena);
        decimal.TryParse(TxtProdajnaCena.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal prodCena);

        string? tarifniBroj = CmbTarifniBroj.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(tarifniBroj)) tarifniBroj = null;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);

            if (_existingArtikal == null)
            {
                bool vecPostoji = await db.Artikli.AnyAsync(a => a.SifraArtikla == sifra);
                if (vecPostoji)
                {
                    MessageBox.Show($"Artikal sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtSifra.Focus();
                    return;
                }

                db.Artikli.Add(new Artikal
                {
                    SifraArtikla = sifra,
                    Naziv = naziv,
                    JedinicaMere = jm,
                    Pakovanje = string.IsNullOrWhiteSpace(TxtPakovanje.Text) ? null : TxtPakovanje.Text.Trim(),
                    TarifniBroj = tarifniBroj,
                    NabavnaCena = nabCena,
                    ProdajnaCena = prodCena
                });
            }
            else
            {
                var a = await db.Artikli.FirstOrDefaultAsync(x => x.ArtikalId == _existingArtikal.ArtikalId);
                if (a != null)
                {
                    a.Naziv = naziv;
                    a.JedinicaMere = jm;
                    a.Pakovanje = string.IsNullOrWhiteSpace(TxtPakovanje.Text) ? null : TxtPakovanje.Text.Trim();
                    a.TarifniBroj = tarifniBroj;
                    a.NabavnaCena = nabCena;
                    a.ProdajnaCena = prodCena;
                }
            }

            await db.SaveChangesAsync();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju artikla:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
            "🛒 Pomoć — Artikal / Roba",
            "Šifarnik artikala korišćen u kalkulacijama, fakturama i nivelacijama.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
            },
            "Šifra artikla mora biti jedinstvena. Poreska tarifa se bira iz šifarnika poreskih tarifa i određuje stopu PDV-a koja se primenjuje pri prodaji ovog artikla."
        ) { Owner = this }.ShowDialog();
    }
}

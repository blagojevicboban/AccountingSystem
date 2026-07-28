using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using Microsoft.Data.Sqlite;

namespace AccountingApp.Views.Firme;

/// <summary>
/// Redak u listi firmi — jedan .db fajl iz AppConfig.BazeDir = jedna firma
/// (analogno SredstvaApp FirmaGridItem/FirmePage).
/// </summary>
public class FirmaGridItem
{
    public string Sifra { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string? Pib { get; set; }
    public string? MaticniBroj { get; set; }
    public string? Adresa { get; set; }
    public string? PttIMesto { get; set; }
    public string? Telefon { get; set; }
    public string? ZiroRacun { get; set; }
    public string DbFilePath { get; set; } = "";
    public bool JeTrenutnoOtvorena => string.Equals(DbFilePath, AppConfig.DbPath, StringComparison.OrdinalIgnoreCase);
}

public partial class FirmeView : UserControl
{
    private List<FirmaGridItem> _sveFirme = new();
    private FirmaGridItem? _selectedItem;
    private bool _isNew;

    public FirmeView()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            UcitajFirme();
            PostaviRezimPregleda();
        };
    }

    private void UcitajFirme()
    {
        var lista = new List<FirmaGridItem>();
        try
        {
            Directory.CreateDirectory(AppConfig.BazeDir);
            var fajlovi = Directory.GetFiles(AppConfig.BazeDir, "*.db");

            foreach (var fajl in fajlovi)
            {
                try
                {
                    using var ctx = AccountingDbContext.Create(fajl);
                    var firma = ctx.Firme.FirstOrDefault();
                    if (firma == null) continue; // prazna/strana baza — preskoči, ne upisuj u tuđ fajl

                    lista.Add(new FirmaGridItem
                    {
                        Sifra = firma.Sifra,
                        Naziv = firma.Naziv,
                        Pib = firma.Pib,
                        MaticniBroj = firma.MaticniBroj,
                        Adresa = firma.Adresa,
                        PttIMesto = firma.PttIMesto,
                        Telefon = firma.Telefon,
                        ZiroRacun = firma.ZiroRacun,
                        DbFilePath = fajl
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Preskačem bazu '{fajl}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju firmi:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        _sveFirme = lista.OrderBy(f => f.Naziv).ToList();
        PrimeniFilter();
    }

    private void PrimeniFilter()
    {
        var upit = TxtPretraga.Text.Trim().ToLower();

        var filtrirane = string.IsNullOrWhiteSpace(upit)
            ? _sveFirme
            : _sveFirme.Where(f =>
                f.Naziv.ToLower().Contains(upit) ||
                f.Sifra.ToLower().Contains(upit) ||
                (f.Pib != null && f.Pib.ToLower().Contains(upit)) ||
                (f.MaticniBroj != null && f.MaticniBroj.ToLower().Contains(upit)) ||
                (f.PttIMesto != null && f.PttIMesto.ToLower().Contains(upit))).ToList();

        DgFirme.ItemsSource = filtrirane;
        TxtUkupno.Text = $"Ukupno: {filtrirane.Count} firmi";

        if (DgFirme.SelectedItem == null && filtrirane.Count > 0)
        {
            var aktivna = filtrirane.FirstOrDefault(f => f.JeTrenutnoOtvorena) ?? filtrirane.FirstOrDefault();
            if (aktivna != null)
            {
                DgFirme.SelectedItem = aktivna;
            }
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        PrimeniFilter();
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajFirme();
    }

    private void DgFirme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgFirme.SelectedItem is FirmaGridItem item)
        {
            _selectedItem = item;
            PopuniPolja(item);
            TxtPanelTitle.Text = $"Detalji firme — {item.Naziv}";
            TxtHint.Text = "Prikaz detalja selektovane firme. Za izmenu podataka kliknite '✏️ Izmeni' u tabeli.";
            TxtHint.Visibility = Visibility.Visible;
        }
        else if (!_isNew)
        {
            PostaviRezimPregleda();
        }
    }

    // ---- Panel: pregled / unos / izmena ----

    private void PostaviRezimPregleda()
    {
        _isNew = false;
        _selectedItem = null;
        TxtPanelTitle.Text = "Detalji firme";
        TxtHint.Text = "Izaberite firmu iz liste ili kliknite 'Nova firma'.";
        TxtHint.Visibility = Visibility.Visible;
        FormFieldsPanel.IsEnabled = false;
        ActionButtonsPanel.Visibility = Visibility.Collapsed;
        DgFirme.IsEnabled = true;
        OcistiPolja();
    }

    private void OcistiPolja()
    {
        TxtSifra.Text = "";
        TxtNaziv.Text = "";
        TxtPib.Text = "";
        TxtMaticniBroj.Text = "";
        TxtAdresa.Text = "";
        TxtPttIMesto.Text = "";
        TxtTelefon.Text = "";
        TxtZiroRacun.Text = "";
        TxtDbPath.Text = "";
    }

    private void PopuniPolja(FirmaGridItem item)
    {
        TxtSifra.Text = item.Sifra;
        TxtNaziv.Text = item.Naziv;
        TxtPib.Text = item.Pib;
        TxtMaticniBroj.Text = item.MaticniBroj;
        TxtAdresa.Text = item.Adresa;
        TxtPttIMesto.Text = item.PttIMesto;
        TxtTelefon.Text = item.Telefon;
        TxtZiroRacun.Text = item.ZiroRacun;
        TxtDbPath.Text = item.DbFilePath;
    }

    private void BtnNovaFirma_Click(object sender, RoutedEventArgs e)
    {
        _isNew = true;
        _selectedItem = null;
        OcistiPolja();
        TxtDbPath.Text = "(kreiraće se pri čuvanju)";
        TxtPanelTitle.Text = "➕ Unos nove firme";
        TxtHint.Visibility = Visibility.Collapsed;
        FormFieldsPanel.IsEnabled = true;
        ActionButtonsPanel.Visibility = Visibility.Visible;
        DgFirme.IsEnabled = false;
        TxtSifra.Focus();
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not FirmaGridItem item) return;

        _isNew = false;
        _selectedItem = item;
        PopuniPolja(item);
        TxtPanelTitle.Text = $"✏️ Izmena firme — {item.Naziv}";
        TxtHint.Visibility = Visibility.Collapsed;
        FormFieldsPanel.IsEnabled = true;
        ActionButtonsPanel.Visibility = Visibility.Visible;
        DgFirme.IsEnabled = false;
        TxtNaziv.Focus();
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        PostaviRezimPregleda();
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        var sifra = TxtSifra.Text.Trim();
        var naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            MessageBox.Show("Molimo unesite šifru firme.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtSifra.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Molimo unesite naziv firme.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtNaziv.Focus();
            return;
        }

        var pib = string.IsNullOrWhiteSpace(TxtPib.Text) ? null : TxtPib.Text.Trim();
        var maticniBroj = string.IsNullOrWhiteSpace(TxtMaticniBroj.Text) ? null : TxtMaticniBroj.Text.Trim();
        var adresa = string.IsNullOrWhiteSpace(TxtAdresa.Text) ? null : TxtAdresa.Text.Trim();
        var pttIMesto = string.IsNullOrWhiteSpace(TxtPttIMesto.Text) ? null : TxtPttIMesto.Text.Trim();
        var telefon = string.IsNullOrWhiteSpace(TxtTelefon.Text) ? null : TxtTelefon.Text.Trim();
        var ziroRacun = string.IsNullOrWhiteSpace(TxtZiroRacun.Text) ? null : TxtZiroRacun.Text.Trim();

        try
        {
            if (_isNew)
            {
                if (_sveFirme.Any(f => string.Equals(f.Sifra, sifra, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show($"Firma sa šifrom '{sifra}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Directory.CreateDirectory(AppConfig.BazeDir);
                var noviPath = Path.Combine(AppConfig.BazeDir,
                    $"firma_{AppConfig.SanitizujZaNazivFajla(sifra)}_{AppConfig.SanitizujZaNazivFajla(naziv)}.db");

                if (File.Exists(noviPath))
                {
                    MessageBox.Show("Fajl baze sa ovim imenom već postoji. Promenite šifru ili naziv.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool nijeBiloNijedneFirme = _sveFirme.Count == 0;

                using (var ctx = AccountingDbContext.Create(noviPath))
                {
                    ctx.Firme.Add(new Firma
                    {
                        Sifra = sifra,
                        Naziv = naziv,
                        Pib = pib,
                        MaticniBroj = maticniBroj,
                        Adresa = adresa,
                        PttIMesto = pttIMesto,
                        Telefon = telefon,
                        ZiroRacun = ziroRacun
                    });
                    ctx.SaveChanges();
                }

                // Ako do sada nije postojala nijedna baza, odmah je postaviti kao aktivnu
                // (nema šta da se desinhronizuje — ništa nije bilo ni otvoreno).
                if (nijeBiloNijedneFirme)
                {
                    AppConfig.DbPath = noviPath;
                }
            }
            else if (_selectedItem != null)
            {
                // Izmena ide direktno u bazu TE firme (ne nužno trenutno aktivnu).
                using var ctx = AccountingDbContext.Create(_selectedItem.DbFilePath);
                var firma = ctx.Firme.FirstOrDefault();
                if (firma == null)
                {
                    MessageBox.Show("Baza firme nije u očekivanom stanju (nema Firma zapisa).", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                firma.Sifra = sifra;
                firma.Naziv = naziv;
                firma.Pib = pib;
                firma.MaticniBroj = maticniBroj;
                firma.Adresa = adresa;
                firma.PttIMesto = pttIMesto;
                firma.Telefon = telefon;
                firma.ZiroRacun = ziroRacun;
                ctx.SaveChanges();

                if (_selectedItem.JeTrenutnoOtvorena)
                {
                    AppSession.TrenutnaFirma = firma;
                }
            }

            UcitajFirme();
            PostaviRezimPregleda();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju podataka o firmi:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnAktiviraj_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not FirmaGridItem item) return;
        if (item.JeTrenutnoOtvorena) return;

        var potvrda = MessageBox.Show(
            $"Firma '{item.Naziv}' će postati aktivna za rad u sistemu.\n\nAplikacija će se sada ponovo pokrenuti radi primene promena.",
            "Postavi kao aktivnu", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.OK) return;

        try
        {
            AppConfig.DbPath = item.DbFilePath;

            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
            }
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri aktiviranju firme:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBrisi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not FirmaGridItem item) return;

        if (_sveFirme.Count <= 1)
        {
            MessageBox.Show("Ne možete obrisati poslednju preostalu firmu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da trajno obrišete firmu '{item.Naziv}'?\n\nOvo trajno briše bazu podataka te firme (konta, naloge, partnere, sve podatke) i ne može se poništiti.",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            bool bilaAktivna = item.JeTrenutnoOtvorena;

            SqliteConnection.ClearAllPools();

            if (bilaAktivna)
            {
                var druga = _sveFirme.First(f => f.DbFilePath != item.DbFilePath);
                AppConfig.DbPath = druga.DbFilePath;
            }

            File.Delete(item.DbFilePath);

            if (bilaAktivna)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                }
                Application.Current.Shutdown();
                return;
            }

            UcitajFirme();
            PostaviRezimPregleda();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju firme:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingApp.Views.Pomoc;
using AccountingData;
using AccountingData.Models;

namespace AccountingApp.Views.Korisnici;

public partial class KorisnikEditWindow : Window
{
    private readonly AccountingDbContext _db;
    public Korisnik Korisnik { get; private set; }
    private readonly bool _isNew;

    public KorisnikEditWindow(AccountingDbContext db, Korisnik? korisnik = null)
    {
        InitializeComponent();
        ContextHelpFix.UkloniDugmeZaPomoc(this);
        _db = db;

        if (korisnik == null)
        {
            _isNew = true;
            Korisnik = new Korisnik { IsActive = true, Uloga = "Knjigovođa" };
            TxtTitle.Text = "👤 Dodavanje korisničkog naloga";
            LblLozinka.Text = "Lozinka *";
        }
        else
        {
            _isNew = false;
            Korisnik = korisnik;
            TxtTitle.Text = "✏️ Izmena korisničkog naloga";
            LblLozinka.Text = "Nova lozinka (opciono)";
            TxtLozinkaHint.Visibility = Visibility.Visible;
            PopuniPolja();
        }
    }

    private void PopuniPolja()
    {
        TxtKorisnickoIme.Text = Korisnik.KorisnickoIme;
        TxtImeIPrezime.Text = Korisnik.ImeIPrezime;
        ChkIsActive.IsChecked = Korisnik.IsActive;

        foreach (ComboBoxItem item in CmbUloga.Items)
        {
            if (item.Content.ToString() == Korisnik.Uloga)
            {
                CmbUloga.SelectedItem = item;
                break;
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            BtnCancel_Click(sender, e);
        }
        else if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "👤 Pomoć — Korisnički nalog",
            "Kreiranje i izmena korisničkih naloga i njihovih uloga (RBAC).",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
            },
            "Pri izmeni postojećeg korisnika, polje lozinke ostavite prazno da zadržite postojeću lozinku — unesite novu samo ako je zaista menjate. Za opis šta svaka uloga (Administrator/Knjigovođa/Gledalac) sme da radi, pogledajte temu '🔐 Prijava, korisnici i bezbednost' u glavnom Help meniju."
        ) { Owner = this }.ShowDialog();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        var username = TxtKorisnickoIme.Text.Trim();
        var name = TxtImeIPrezime.Text.Trim();
        var password = TxtLozinka.Password;

        if (string.IsNullOrWhiteSpace(username))
        {
            MessageBox.Show("Molimo unesite korisničko ime.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtKorisnickoIme.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Molimo unesite ime i prezime korisnika.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtImeIPrezime.Focus();
            return;
        }

        if (_isNew && string.IsNullOrEmpty(password))
        {
            MessageBox.Show("Molimo unesite lozinku za novi nalog.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtLozinka.Focus();
            return;
        }

        // Provera jedinstvenosti korisničkog imena
        var existing = _db.Korisnici.FirstOrDefault(k => k.KorisnickoIme == username && k.KorisnikId != Korisnik.KorisnikId);
        if (existing != null)
        {
            MessageBox.Show($"Korisničko ime '{username}' već postoji u bazi. Izaberite drugo.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtKorisnickoIme.Focus();
            return;
        }

        Korisnik.KorisnickoIme = username;
        Korisnik.ImeIPrezime = name;
        Korisnik.IsActive = ChkIsActive.IsChecked ?? true;

        if (CmbUloga.SelectedItem is ComboBoxItem selectedRole)
        {
            Korisnik.Uloga = selectedRole.Content.ToString() ?? "Knjigovođa";
        }

        if (!string.IsNullOrEmpty(password))
        {
            Korisnik.LozinkaHash = AccountingDbContext.HashPassword(password);
        }

        try
        {
            if (_isNew)
            {
                _db.Korisnici.Add(Korisnik);
            }
            _db.SaveChanges();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju korisničkog naloga:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

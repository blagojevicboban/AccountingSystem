using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;

namespace AccountingApp.Views.Korisnici;

public partial class LoginWindow : Window
{
    private readonly AccountingDbContext _db;

    public LoginWindow(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;

        LoadCompanyInfo();

#if DEBUG
        TxtUsername.Text = "admin";
        TxtPassword.Password = "admin";
#endif
        TxtUsername.Focus();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"ERPi © 2026 Blagojević Boban - v{version?.ToString(3)}";
    }

    private void LoadCompanyInfo()
    {
        var firma = _db.Firme.FirstOrDefault();
        if (firma != null)
        {
            TxtFirma.Text = firma.Naziv;
            AppSession.TrenutnaFirma = firma;
        }
        else
        {
            TxtFirma.Text = "Nije dostupna kompanija";
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DoLogin();
        }
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        DoLogin();
    }

    private void DoLogin()
    {
        TxtError.Visibility = Visibility.Collapsed;
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Unesite korisničko ime i lozinku.");
            return;
        }

        var korisnik = _db.Korisnici.FirstOrDefault(k => k.KorisnickoIme == username);

        if (korisnik == null || !AccountingDbContext.VerifyPassword(password, korisnik.LozinkaHash))
        {
            ShowError("Pogrešno korisničko ime ili lozinka.");
            return;
        }

        if (!korisnik.IsActive)
        {
            ShowError("Vaš nalog je deaktiviran. Obratite se administratoru.");
            return;
        }

        // Podrazumevana lozinka iz inicijalnog seed-a je javno poznata (nalazi se u
        // izvornom kodu i migracijama), pa se mora promeniti pre prvog ulaska u sistem.
        if (AccountingDbContext.VerifyPassword("admin123", korisnik.LozinkaHash) && !ZahtevajPromenuLozinke(korisnik))
        {
            ShowError("Morate postaviti novu lozinku da biste nastavili.");
            TxtPassword.Clear();
            return;
        }

        AppSession.TrenutniKorisnik = korisnik;
        korisnik.PoslednjaPrijava = DateTime.Now;
        _db.SaveChanges();

        var mainWindow = new MainWindow(_db);
        mainWindow.Show();

        Close();
    }

    /// <summary>
    /// Otvara izmenu naloga i ne pušta dalje dok lozinka stvarno nije promenjena
    /// u nešto različito od podrazumevane. Vraća true ako je promena uspela.
    /// </summary>
    private bool ZahtevajPromenuLozinke(Korisnik korisnik)
    {
        MessageBox.Show(
            $"Nalog '{korisnik.KorisnickoIme}' još uvek koristi podrazumevanu lozinku.\n\n" +
            "Ta lozinka je javno poznata i mora se promeniti pre nastavka rada.",
            "Obavezna promena lozinke", MessageBoxButton.OK, MessageBoxImage.Warning);

        var dlg = new KorisnikEditWindow(_db, korisnik) { Owner = this };
        if (dlg.ShowDialog() != true)
        {
            _db.Entry(korisnik).Reload();
            return false;
        }

        if (AccountingDbContext.VerifyPassword("admin123", korisnik.LozinkaHash))
        {
            MessageBox.Show("Nova lozinka ne sme biti ista kao podrazumevana.",
                "Lozinka nije promenjena", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}

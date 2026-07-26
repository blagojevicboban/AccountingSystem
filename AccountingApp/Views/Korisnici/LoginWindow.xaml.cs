using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AccountingData;

namespace AccountingApp.Views.Korisnici;

public partial class LoginWindow : Window
{
    private readonly AccountingDbContext _db;

    public LoginWindow(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;

        LoadCompanyInfo();
        EnsureAdminPasswordUpdated();

#if DEBUG
        TxtUsername.Text = "admin";
        TxtPassword.Password = "admin";
#endif
        TxtUsername.Focus();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"AccountingSystem © 2026 Blagojević Boban - v{version?.ToString(3)}";
    }

    private void EnsureAdminPasswordUpdated()
    {
        try
        {
            var admin = _db.Korisnici.FirstOrDefault(k => k.KorisnickoIme == "admin");
            if (admin != null && (string.IsNullOrEmpty(admin.LozinkaHash) || AccountingDbContext.VerifyPassword("admin123", admin.LozinkaHash)))
            {
                admin.LozinkaHash = "PBKDF2$100000$IxpGjzsTHvV0x7fZq6RdJQ==$6ERduoiJeJ9Iwc5bF56gYD0r3MqcFCWBYyw8XTHQ3u4=";
                _db.SaveChanges();
            }
        }
        catch { }
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

        AppSession.TrenutniKorisnik = korisnik;
        korisnik.PoslednjaPrijava = DateTime.Now;
        _db.SaveChanges();

        var mainWindow = new MainWindow(_db);
        mainWindow.Show();

        Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}

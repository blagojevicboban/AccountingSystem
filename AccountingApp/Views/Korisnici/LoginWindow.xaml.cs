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
        TxtUsername.Focus();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"AccountingSystem © 2026 - v{version?.ToString(3)}";
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

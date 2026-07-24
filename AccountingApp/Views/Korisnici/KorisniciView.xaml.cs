using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Korisnici;

public partial class KorisniciView : UserControl
{
    private readonly AccountingDbContext _db;
    private List<Korisnik> _sviKorisnici = new();

    public KorisniciView(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += async (s, e) => await UcitajKorisnikeAsync();
    }

    private async Task UcitajKorisnikeAsync()
    {
        try
        {
            _sviKorisnici = await _db.Korisnici.OrderBy(k => k.KorisnikId).ToListAsync();
            PrimeniFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju korisnika:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrimeniFilter()
    {
        var upit = TxtPretraga.Text.Trim().ToLower();

        var filtrirani = string.IsNullOrWhiteSpace(upit)
            ? _sviKorisnici
            : _sviKorisnici.Where(k =>
                (k.KorisnickoIme != null && k.KorisnickoIme.ToLower().Contains(upit)) ||
                (k.ImeIPrezime != null && k.ImeIPrezime.ToLower().Contains(upit)) ||
                (k.Uloga != null && k.Uloga.ToLower().Contains(upit))).ToList();

        DgKorisnici.ItemsSource = filtrirani;
        TxtUkupno.Text = $"Ukupno: {filtrirani.Count} korisnika";
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        PrimeniFilter();
    }

    private async void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        await UcitajKorisnikeAsync();
    }

    private async void BtnNoviKorisnik_Click(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Samo korisnici sa ulogom Administrator mogu dodavati nove korisničke naloge.",
                "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new KorisnikEditWindow(_db)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await UcitajKorisnikeAsync();
        }
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Korisnik korisnik)
        {
            if (!AppSession.IsAdministrator && AppSession.TrenutniKorisnik?.KorisnikId != korisnik.KorisnikId)
            {
                MessageBox.Show("Nemate pravo izmene tuđih korisničkih naloga.",
                    "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new KorisnikEditWindow(_db, korisnik)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                await UcitajKorisnikeAsync();
            }
        }
    }

    private async void BtnBrisi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Korisnik korisnik)
        {
            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Samo korisnici sa ulogom Administrator mogu brisati i deaktivirati naloge.",
                    "Pristup odbijen", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (korisnik.KorisnickoIme == "admin")
            {
                MessageBox.Show("Glavni administratorski nalog (admin) ne može biti izbrisan ili deaktiviran.",
                    "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var rez = MessageBox.Show(
                $"Da li ste sigurni da želite da izbrišete ili deaktivirate nalog '{korisnik.KorisnickoIme}'?\n\nKliknite YES za brisanje ili NO za deaktivaciju.",
                "Potvrda brisanja / deaktivacije",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (rez == MessageBoxResult.Yes)
            {
                try
                {
                    _db.Korisnici.Remove(korisnik);
                    await _db.SaveChangesAsync();
                    await UcitajKorisnikeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri brisanju korisničkog naloga:\n{ex.Message}",
                        "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (rez == MessageBoxResult.No)
            {
                korisnik.IsActive = false;
                await _db.SaveChangesAsync();
                await UcitajKorisnikeAsync();
            }
        }
    }
}

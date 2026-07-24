using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Firme;

public partial class FirmeView : UserControl
{
    private readonly AccountingDbContext _db;
    private List<Firma> _sveFirme = new();

    public FirmeView(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += async (s, e) => await UcitajFirmeAsync();
    }

    private async Task UcitajFirmeAsync()
    {
        try
        {
            _sveFirme = await _db.Firme.OrderBy(f => f.Sifra).ToListAsync();
            PrimeniFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju firmi:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrimeniFilter()
    {
        var upit = TxtPretraga.Text.Trim().ToLower();

        var filtrirane = string.IsNullOrWhiteSpace(upit)
            ? _sveFirme
            : _sveFirme.Where(f =>
                (f.Naziv != null && f.Naziv.ToLower().Contains(upit)) ||
                (f.Sifra != null && f.Sifra.ToLower().Contains(upit)) ||
                (f.Pib != null && f.Pib.ToLower().Contains(upit)) ||
                (f.MaticniBroj != null && f.MaticniBroj.ToLower().Contains(upit)) ||
                (f.PttIMesto != null && f.PttIMesto.ToLower().Contains(upit))).ToList();

        DgFirme.ItemsSource = filtrirane;
        TxtUkupno.Text = $"Ukupno: {filtrirane.Count} firmi";
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        PrimeniFilter();
    }

    private async void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        await UcitajFirmeAsync();
    }

    private async void BtnNovaFirma_Click(object sender, RoutedEventArgs e)
    {
        var window = new FirmaEditWindow(_db)
        {
            Owner = Window.GetWindow(this)
        };

        if (window.ShowDialog() == true)
        {
            await UcitajFirmeAsync();

            // Ako do sada nismo imali aktivnu firmu, postavi novokreiranu
            if (AppSession.TrenutnaFirma == null)
            {
                AppSession.TrenutnaFirma = window.Firma;
            }
        }
    }

    private void BtnIzaberi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Firma firma)
        {
            AppSession.TrenutnaFirma = firma;
            MessageBox.Show($"Aktivna firma je uspešno postavljena na:\n\n{firma.Naziv} (Šifra: {firma.Sifra})",
                "Aktivna firma", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Firma firma)
        {
            var window = new FirmaEditWindow(_db, firma)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
            {
                await UcitajFirmeAsync();

                // Ako je izmenjena trenutno aktivna firma, osveži sesiju
                if (AppSession.TrenutnaFirma?.FirmaId == firma.FirmaId)
                {
                    AppSession.TrenutnaFirma = firma;
                }
            }
        }
    }

    private async void BtnBrisi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is Firma firma)
        {
            var rez = MessageBox.Show(
                $"Da li ste sigurni da želite da izbrišete ili deaktivirate firmu '{firma.Naziv}'?\n\nKliknite YES za brisanje ili NO za deaktivaciju.",
                "Potvrda brisanja / deaktivacije",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (rez == MessageBoxResult.Yes)
            {
                try
                {
                    _db.Firme.Remove(firma);
                    await _db.SaveChangesAsync();

                    if (AppSession.TrenutnaFirma?.FirmaId == firma.FirmaId)
                    {
                        AppSession.TrenutnaFirma = _db.Firme.FirstOrDefault();
                    }

                    await UcitajFirmeAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Nije moguće izbrisati firmu jer postoje povezani podaci. Možete je umesto toga deaktivirati.\n\nDetalji: {ex.Message}",
                        "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (rez == MessageBoxResult.No)
            {
                firma.IsActive = false;
                await _db.SaveChangesAsync();
                await UcitajFirmeAsync();
            }
        }
    }

    private void DgFirme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
}

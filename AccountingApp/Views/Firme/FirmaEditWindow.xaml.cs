using System;
using System.Windows;
using AccountingData;
using AccountingData.Models;

namespace AccountingApp.Views.Firme;

public partial class FirmaEditWindow : Window
{
    private readonly AccountingDbContext _db;
    public Firma Firma { get; private set; }
    private readonly bool _isNew;

    public FirmaEditWindow(AccountingDbContext db, Firma? firma = null)
    {
        InitializeComponent();
        _db = db;

        if (firma == null)
        {
            _isNew = true;
            Firma = new Firma { DatumKreiranja = DateTime.Now, IsActive = true };
            TxtTitle.Text = "🏢 Dodavanje nove firme";
        }
        else
        {
            _isNew = false;
            Firma = firma;
            TxtTitle.Text = "✏️ Izmena podataka o firmi";
            PopuniPolja();
        }
    }

    private void PopuniPolja()
    {
        TxtSifra.Text = Firma.Sifra;
        TxtNaziv.Text = Firma.Naziv;
        TxtPib.Text = Firma.Pib;
        TxtMaticniBroj.Text = Firma.MaticniBroj;
        TxtAdresa.Text = Firma.Adresa;
        TxtPttIMesto.Text = Firma.PttIMesto;
        TxtTelefon.Text = Firma.Telefon;
        TxtZiroRacun.Text = Firma.ZiroRacun;
        ChkIsActive.IsChecked = Firma.IsActive;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
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

        Firma.Sifra = sifra;
        Firma.Naziv = naziv;
        Firma.Pib = string.IsNullOrWhiteSpace(TxtPib.Text) ? null : TxtPib.Text.Trim();
        Firma.MaticniBroj = string.IsNullOrWhiteSpace(TxtMaticniBroj.Text) ? null : TxtMaticniBroj.Text.Trim();
        Firma.Adresa = string.IsNullOrWhiteSpace(TxtAdresa.Text) ? null : TxtAdresa.Text.Trim();
        Firma.PttIMesto = string.IsNullOrWhiteSpace(TxtPttIMesto.Text) ? null : TxtPttIMesto.Text.Trim();
        Firma.Telefon = string.IsNullOrWhiteSpace(TxtTelefon.Text) ? null : TxtTelefon.Text.Trim();
        Firma.ZiroRacun = string.IsNullOrWhiteSpace(TxtZiroRacun.Text) ? null : TxtZiroRacun.Text.Trim();
        Firma.IsActive = ChkIsActive.IsChecked ?? true;

        try
        {
            if (_isNew)
            {
                _db.Firme.Add(Firma);
            }
            _db.SaveChanges();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju podataka o firmi:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;

namespace AccountingApp.Views.MestaTroska;

public partial class MestoTroskaEditWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly MestaTroskaService _service;
    private readonly MestoTroska _mt;

    public MestoTroskaEditWindow(MestoTroska mt, AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new MestaTroskaService(_db);
        _mt = mt;

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += MestoTroskaEditWindow_Loaded;
    }

    private void MestoTroskaEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TxtSifra.Text = _mt.Sifra;
        TxtNaziv.Text = _mt.Naziv;
        TxtNapomena.Text = _mt.Napomena;
        ChkAktivno.IsChecked = _mt.IsAktivno;

        CmbTip.SelectedIndex = (int)_mt.Tip;
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string sifra = TxtSifra.Text.Trim();
        string naziv = TxtNaziv.Text.Trim();

        if (string.IsNullOrWhiteSpace(sifra) || string.IsNullOrWhiteSpace(naziv))
        {
            MessageBox.Show("Unesite šifru i naziv mesta troška.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _mt.Sifra = sifra;
        _mt.Naziv = naziv;
        _mt.Tip = (TipMestaTroska)CmbTip.SelectedIndex;
        _mt.Napomena = TxtNapomena.Text.Trim();
        _mt.IsAktivno = ChkAktivno.IsChecked == true;

        try
        {
            await _service.SacuvajMestoTroskaAsync(_mt);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju mesta troška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

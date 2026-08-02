using System;
using System.Windows;
using System.Windows.Input;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;

namespace ERPiFinansijeApp.Views.Blagajna;

public partial class BlagajnickiNalogEditWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly BlagajnaService _service;
    private readonly BlagajnickiNalog _bn;

    public BlagajnickiNalogEditWindow(BlagajnickiNalog bn, AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BlagajnaService(_db);
        _bn = bn;

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += BlagajnickiNalogEditWindow_Loaded;
    }

    private void BlagajnickiNalogEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CmbVrstaBlagajne.SelectedIndex = _bn.VrstaBlagajne == VrstaBlagajne.Devizna ? 1 : 0;
        CmbVrstaNaloga.SelectedIndex = _bn.VrstaNaloga == VrstaBlagajnickogNaloga.Isplata ? 1 : 0;

        DpDatum.SelectedDate = _bn.Datum;
        TxtKontoProtu.Text = string.IsNullOrWhiteSpace(_bn.BrojKontaProtu) ? "2410" : _bn.BrojKontaProtu;
        TxtUplatilacIsplatilac.Text = _bn.UplatilacIsplatilac;
        TxtSvrha.Text = _bn.Svrha;
        TxtIznos.Text = _bn.Iznos > 0 ? _bn.Iznos.ToString("G") : "";
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string uplatilac = TxtUplatilacIsplatilac.Text.Trim();
        string svrha = TxtSvrha.Text.Trim();
        decimal.TryParse(TxtIznos.Text, out decimal iznos);

        if (string.IsNullOrWhiteSpace(uplatilac) || string.IsNullOrWhiteSpace(svrha))
        {
            MessageBox.Show("Unesite ime uplatioca/isplatioca i svrhu nalaog.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (iznos <= 0)
        {
            MessageBox.Show("Unesite ispravan iznos veći od 0.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _bn.VrstaBlagajne = CmbVrstaBlagajne.SelectedIndex == 1 ? VrstaBlagajne.Devizna : VrstaBlagajne.Dinarska;
        _bn.VrstaNaloga = CmbVrstaNaloga.SelectedIndex == 1 ? VrstaBlagajnickogNaloga.Isplata : VrstaBlagajnickogNaloga.Uplata;
        _bn.Datum = DpDatum.SelectedDate ?? DateTime.Today;
        _bn.BrojKontaProtu = TxtKontoProtu.Text.Trim();
        _bn.UplatilacIsplatilac = uplatilac;
        _bn.Svrha = svrha;
        _bn.Iznos = iznos;

        try
        {
            await _service.SacuvajBlagajnickiNalogAsync(_bn);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju nalaog blagajne: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

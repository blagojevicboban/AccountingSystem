using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.Win32;

namespace AccountingApp.Views.Izvodi;

public partial class UvozIzvodaWindow : Window
{
    private readonly AccountingDbContext _db;
    private readonly BankIzvodService _service;
    private BankIzvod? _trenutniIzvod;

    public bool JeProknjizeno { get; private set; }

    public UvozIzvodaWindow(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;
        _service = new BankIzvodService(_db);

        // Prečica Esc za zatvaranje
        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }

    private async void BtnIzaberiFajl_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Bankarski izvodi (*.xml;*.txt;*.sta;*.940)|*.xml;*.txt;*.sta;*.940|Halcom/CAMT XML (*.xml)|*.xml|SWIFT MT940 (*.txt;*.sta;*.940)|*.txt;*.sta;*.940|Svi fajlovi (*.*)|*.*",
            Title = "Izaberite fajl bankarskog izvoda"
        };

        if (dlg.ShowDialog() == true)
        {
            TxtFajlPutanja.Text = dlg.FileName;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _trenutniIzvod = await _service.UcitajIIzanalizirajIzvodAsync(dlg.FileName);
                Mouse.OverrideCursor = null;

                TxtFormatNaziv.Text = $"Format: {_trenutniIzvod.Format}";
                TxtBrojIzvoda.Text = string.IsNullOrWhiteSpace(_trenutniIzvod.BrojIzvoda) ? "1" : _trenutniIzvod.BrojIzvoda;
                TxtDatumIzvoda.Text = _trenutniIzvod.DatumIzvoda.ToString("dd.MM.yyyy");
                TxtPocetnoStanje.Text = $"{_trenutniIzvod.PocetnoStanje:N2}";
                TxtUkupnoUplate.Text = $"+{_trenutniIzvod.UkupnoUplata:N2}";
                TxtUkupnoIsplate.Text = $"-{_trenutniIzvod.UkupnoIsplata:N2}";

                GridSummary.Visibility = Visibility.Visible;
                DgStavke.ItemsSource = _trenutniIzvod.Stavke;

                BtnProknjizi.IsEnabled = _trenutniIzvod.Stavke.Count > 0;
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Greška pri čitanju i analizi izvoda: {ex.Message}", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (_trenutniIzvod == null || _trenutniIzvod.Stavke.Count == 0) return;

        var res = MessageBox.Show(
            $"Da li ste sigurni da želite da proknjižite izvod br. {_trenutniIzvod.BrojIzvoda} " +
            $"sa {_trenutniIzvod.Stavke.Count} stavki u Glavnu knjigu i izvršite automatsko IOS zatvaranje otvorenih računa?",
            "Potvrda knjiženja izvoda",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var korisnik = AppSession.TrenutniKorisnik;
            int korisnikId = korisnik?.KorisnikId ?? 1;
            string korisnickoIme = korisnik?.KorisnickoIme ?? "admin";

            var nalog = await _service.ProknjiziIzvodIZatvoriStavkeAsync(_trenutniIzvod, korisnikId, korisnickoIme);
            Mouse.OverrideCursor = null;

            MessageBox.Show(
                $"Uspešno je kreiran i proknjižen nalog knjiženja br. {nalog.BrojNaloga} (IZV) sa {nalog.Stavke.Count} stavki!\n\n" +
                $"Automatski su zatvorene otvorene stavke kupaca i dobavljača u sistemu otvorenih stavki (IOS).",
                "Knjiženje uspešno",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            JeProknjizeno = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            Mouse.OverrideCursor = null;
            MessageBox.Show($"Greška pri knjiženju izvoda: {ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

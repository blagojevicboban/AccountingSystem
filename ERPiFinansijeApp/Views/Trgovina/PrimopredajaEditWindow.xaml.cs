using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeApp.Views.Pomoc;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Trgovina;

public partial class PrimopredajaEditWindow : Window
{
    private readonly PrimopredajaNalog? _existingNalog;
    private readonly string _vrstaZaNovu;
    private List<ERPiFinansijeData.Models.Magacin> _magacini = new();
    private List<Artikal> _artikli = new();
    public ObservableCollection<PrimopredajaStavkaModel> StavkeModels { get; set; } = new();

    public PrimopredajaEditWindow(PrimopredajaNalog? existingNalog = null, string vrstaZaNovu = "Primopredaja")
    {
        InitializeComponent();
        _existingNalog = existingNalog;
        _vrstaZaNovu = vrstaZaNovu;
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            _magacini = await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync();
            _artikli = await db.Artikli.OrderBy(a => a.SifraArtikla).ToListAsync();

            CmbMagacinDaje.ItemsSource = _magacini;
            CmbMagacinPrima.ItemsSource = _magacini;
            ColArtikal.ItemsSource = _artikli;
            ColArtikal.DisplayMemberPath = "Naziv";
            ColArtikal.SelectedValuePath = "SifraArtikla";

            if (_existingNalog != null)
            {
                TxtNaslov.Text = $"✏️ Izmena — {_existingNalog.VrstaDokumenta} #{_existingNalog.BrojNaloga}";
                TxtBrojNaloga.Text = _existingNalog.BrojNaloga.ToString();
                TxtBrojNaloga.IsReadOnly = true;
                DpDatum.SelectedDate = _existingNalog.Datum;

                CmbMagacinDaje.SelectedItem = _magacini.FirstOrDefault(m => m.SifraMagacina == _existingNalog.SifraMagacinaDaje);
                CmbMagacinPrima.SelectedItem = _magacini.FirstOrDefault(m => m.SifraMagacina == _existingNalog.SifraMagacinaPrima);
                CmbStopaPdv.Text = _existingNalog.StopaPdv.ToString(System.Globalization.CultureInfo.InvariantCulture);

                int rbr = 1;
                foreach (var st in _existingNalog.Stavke)
                {
                    StavkeModels.Add(new PrimopredajaStavkaModel
                    {
                        RedniBroj = rbr++,
                        SifraArtikla = st.SifraArtikla,
                        Kolicina = st.Kolicina,
                        Cena = st.Cena,
                        Iznos = st.Iznos
                    });
                }
            }
            else
            {
                TxtNaslov.Text = $"➕ Novi nalog — {_vrstaZaNovu}";
                DpDatum.SelectedDate = DateTime.Now;

                // Generiši sledeći broj naloga — nezavisan brojač po vrsti dokumenta, analogno
                // odvojenim legacy DBF fajlovima (ZADUZ.DBF / RAZDUZ.DBF / MAT_NAL.DBF).
                int maxBr = (await db.PrimopredajaNalozi
                    .Where(n => n.VrstaDokumenta == _vrstaZaNovu)
                    .MaxAsync(n => (int?)n.BrojNaloga) ?? 0) + 1;
                TxtBrojNaloga.Text = maxBr.ToString("D5");

                if (_magacini.Count > 0) CmbMagacinDaje.SelectedIndex = 0;
                if (_magacini.Count > 1) CmbMagacinPrima.SelectedIndex = 1;
                else if (_magacini.Count > 0) CmbMagacinPrima.SelectedIndex = 0;

                CmbStopaPdv.Text = "20";

                StavkeModels.Add(new PrimopredajaStavkaModel { RedniBroj = 1 });
            }

            DgStavke.ItemsSource = StavkeModels;
            AzurirajUpozorenjePrelaza();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju podataka: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Prikazuje polje za stopu PDV i napomenu samo kad magacin koji daje i magacin koji prima
    /// nisu iste vrste (Veleprodaja/Maloprodaja) — samo tada <see cref="PrimopredajaService"/>
    /// pravi nalog u Glavnoj knjizi.
    /// </summary>
    private void AzurirajUpozorenjePrelaza()
    {
        bool prelaziVpMp = CmbMagacinDaje.SelectedItem is ERPiFinansijeData.Models.Magacin md
            && CmbMagacinPrima.SelectedItem is ERPiFinansijeData.Models.Magacin mp
            && md.VrstaMagacina != mp.VrstaMagacina;

        var vidljivost = prelaziVpMp ? Visibility.Visible : Visibility.Collapsed;
        TxtLabelStopaPdv.Visibility = vidljivost;
        CmbStopaPdv.Visibility = vidljivost;
        TxtInfoPrelaz.Visibility = vidljivost;
    }

    private void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e) => AzurirajUpozorenjePrelaza();

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        StavkeModels.Add(new PrimopredajaStavkaModel { RedniBroj = StavkeModels.Count + 1 });
    }

    private void BtnUkloniStavku_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PrimopredajaStavkaModel model)
        {
            StavkeModels.Remove(model);
            int rbr = 1;
            foreach (var s in StavkeModels) s.RedniBroj = rbr++;
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brNaloga))
        {
            MessageBox.Show("Molimo unesite ispravan broj naloga.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbMagacinDaje.SelectedItem is not ERPiFinansijeData.Models.Magacin magDaje ||
            CmbMagacinPrima.SelectedItem is not ERPiFinansijeData.Models.Magacin magPrima)
        {
            MessageBox.Show("Izaberite magacin koji daje i magacin koji prima robnog prometa.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (magDaje.SifraMagacina == magPrima.SifraMagacina)
        {
            MessageBox.Show("Magacin koji daje i magacin koji prima moraju biti različiti!", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool prelaziVpMp = magDaje.VrstaMagacina != magPrima.VrstaMagacina;
        decimal stopaPdv = 20m;
        if (prelaziVpMp && !decimal.TryParse(CmbStopaPdv.Text.Trim(), out stopaPdv))
        {
            MessageBox.Show("Unesite ispravnu stopu PDV (%) — prelaz veleprodaja↔maloprodaja pravi nalog u Glavnoj knjizi i mora znati po kojoj stopi da obračuna porez.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var validneStavke = StavkeModels.Where(s => !string.IsNullOrWhiteSpace(s.SifraArtikla) && s.Kolicina > 0).ToList();
        if (validneStavke.Count == 0)
        {
            MessageBox.Show("Unesite bar jednu validnu stavku robe sa količinom većom od 0.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            var nalog = _existingNalog ?? new PrimopredajaNalog { VrstaDokumenta = _vrstaZaNovu };
            nalog.BrojNaloga = brNaloga;
            nalog.Datum = DpDatum.SelectedDate ?? DateTime.Now;
            nalog.SifraMagacinaDaje = magDaje.SifraMagacina;
            nalog.SifraMagacinaPrima = magPrima.SifraMagacina;
            nalog.StopaPdv = stopaPdv;
            nalog.Stavke = validneStavke.Select((s, idx) => new PrimopredajaStavka
            {
                RedniBroj = idx + 1,
                SifraArtikla = s.SifraArtikla,
                Kolicina = s.Kolicina,
                Cena = s.Cena,
                Iznos = s.Iznos > 0 ? s.Iznos : s.Kolicina * s.Cena
            }).ToList();

            await service.SavePrimopredajuAsync(nalog);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju naloga primopredaje:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
        else if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "🔄 Pomoć — Primopredaja robe",
            "Interni prenos robe između magacina.",
            new (string, string)[]
            {
                ("Esc", "Odustaje od unosa bez čuvanja."),
                ("➕ Dodaj stavku", "Dodaje artikal u nalog za primopredaju."),
                ("🗑️", "Uklanja stavku iz reda u tabeli."),
            },
            "Obavezno izabrati različite magacine 'daje' i 'prima'. Primopredaja se knjiži kao izlaz iz jednog i ulaz u drugi magacin po istoj vrednosti. Kod prelaska veleprodaja↔maloprodaja se dodatno traži stopa PDV i pravi se nalog u Glavnoj knjizi (1320/1340 + ukalkulisani PDV)."
        ) { Owner = this }.ShowDialog();
    }
}

public class PrimopredajaStavkaModel
{
    public int RedniBroj { get; set; }
    public string SifraArtikla { get; set; } = string.Empty;
    public decimal Kolicina { get; set; }
    public decimal Cena { get; set; }
    public decimal Iznos { get; set; }
}

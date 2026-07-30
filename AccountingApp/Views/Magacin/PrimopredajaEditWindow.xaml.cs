using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class PrimopredajaEditWindow : Window
{
    private readonly ObservableCollection<PrimopredajaStavka> _stavke = new();
    private int _existingId = 0;

    public PrimopredajaEditWindow(PrimopredajaNalog? existing = null)
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;

        if (existing != null)
        {
            _existingId = existing.PrimopredajaNalogId;
            TxtBrojNaloga.Text = existing.BrojNaloga.ToString();
            DpDatum.SelectedDate = existing.Datum;

            foreach (var st in existing.Stavke)
            {
                _stavke.Add(new PrimopredajaStavka
                {
                    RedniBroj = st.RedniBroj,
                    SifraArtikla = st.SifraArtikla,
                    Kolicina = st.Kolicina
                });
            }
            Title = $"Izmena primopredaje #{existing.BrojNaloga}";
        }
        else
        {
            DpDatum.SelectedDate = DateTime.Now;
            Title = "Nova primopredaja materijala";
            _ = PredloziBrojAsync();
        }

        _ = UcitajSifarnikeAsync();
    }

    private async Task PredloziBrojAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            int max = await db.PrimopredajaNalozi.Select(p => p.BrojNaloga).DefaultIfEmpty(0).MaxAsync();
            TxtBrojNaloga.Text = (max + 1).ToString();
        }
        catch { }
    }

    private async Task UcitajSifarnikeAsync()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var magacini = await db.Magacini.OrderBy(m => m.SifraMagacina).ToListAsync();
            CmbMagacinDaje.ItemsSource = magacini;
            CmbMagacinPrima.ItemsSource = magacini;

            if (magacini.Count > 0) CmbMagacinDaje.SelectedIndex = 0;
            if (magacini.Count > 1) CmbMagacinPrima.SelectedIndex = 1;

            var artikli = await db.Materijali.OrderBy(a => a.Naziv).ToListAsync();
            ColArtikli.ItemsSource = artikli;
            ColArtikli.DisplayMemberPath = "Naziv";
            ColArtikli.SelectedValuePath = "SifraArtikla";
        }
        catch { }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new PrimopredajaStavka { RedniBroj = _stavke.Count + 1, Kolicina = 1m });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is PrimopredajaStavka st)
        {
            _stavke.Remove(st);
            int rb = 1;
            foreach (var item in _stavke) item.RedniBroj = rb++;
        }
    }

    private async void BtnSačuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga))
        {
            MessageBox.Show("Molimo unesite ispravan broj naloga primopredaje.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbMagacinDaje.SelectedItem is not AccountingData.Models.Magacin magDaje ||
            CmbMagacinPrima.SelectedItem is not AccountingData.Models.Magacin magPrima)
        {
            MessageBox.Show("Molimo izaberite magacin koji daje i magacin koji prima.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (magDaje.SifraMagacina == magPrima.SifraMagacina)
        {
            MessageBox.Show("Izlazni i ulazni magacin ne mogu biti isti.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_stavke.Count == 0)
        {
            MessageBox.Show("Primopredaja mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nalog = new PrimopredajaNalog
        {
            PrimopredajaNalogId = _existingId,
            BrojNaloga = brojNaloga,
            Datum = DpDatum.SelectedDate ?? DateTime.Now,
            SifraMagacinaDaje = magDaje.SifraMagacina,
            SifraMagacinaPrima = magPrima.SifraMagacina,
            Stavke = _stavke.ToList()
        };

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var service = new PrimopredajaService(db);
            await service.SavePrimopredajuAsync(nalog);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}

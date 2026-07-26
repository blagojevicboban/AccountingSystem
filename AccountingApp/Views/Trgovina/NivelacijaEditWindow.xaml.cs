using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class NivelacijaEditWindow : Window
{
    private readonly ObservableCollection<NivelacijaStavka> _stavke = new();
    private int _existingId = 0;
    private List<Artikal> _sviArtikli = new();

    public NivelacijaEditWindow(NivelacijaCena? existing = null)
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        _stavke.CollectionChanged += (s, e) => Preračunaj();

        if (existing != null)
        {
            _existingId = existing.NivelacijaCenaId;
            TxtBrojNivelacije.Text = existing.BrojNivelacije;
            DpDatum.SelectedDate = existing.DatumNivelacije;
            CmbMagacin.SelectedValue = existing.MagacinId;

            foreach (var st in existing.Stavke)
            {
                _stavke.Add(new NivelacijaStavka
                {
                    RedniBroj = st.RedniBroj,
                    ArtikalId = st.ArtikalId,
                    KolicinaZaliha = st.KolicinaZaliha,
                    StaraCena = st.StaraCena,
                    NovaCena = st.NovaCena,
                    RazlikaPoJedinici = st.RazlikaPoJedinici,
                    UkupnaRazlika = st.UkupnaRazlika
                });
            }
            Title = $"Izmena nivelacije #{existing.BrojNivelacije}";
        }
        else
        {
            DpDatum.SelectedDate = DateTime.Now;
            Title = "Nova nivelacija cena";
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

            var brojevi = await db.NivelacijeCena.Select(n => n.BrojNivelacije).ToListAsync();
            int max = 0;
            foreach (var b in brojevi)
            {
                if (int.TryParse(b, out var num) && num > max) max = num;
            }
            TxtBrojNivelacije.Text = (max + 1).ToString();
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
            CmbMagacin.ItemsSource = magacini;
            if (CmbMagacin.SelectedIndex < 0 && magacini.Count > 0) CmbMagacin.SelectedIndex = 0;

            _sviArtikli = await db.Artikli.OrderBy(a => a.Naziv).ToListAsync();
            ColArtikli.ItemsSource = _sviArtikli;
            ColArtikli.DisplayMemberPath = "Naziv";
            ColArtikli.SelectedValuePath = "ArtikalId";
        }
        catch { }
    }

    private void Preračunaj()
    {
        decimal tot = 0m;
        foreach (var s in _stavke)
        {
            s.RazlikaPoJedinici = s.NovaCena - s.StaraCena;
            s.UkupnaRazlika = s.KolicinaZaliha * s.RazlikaPoJedinici;
            tot += s.UkupnaRazlika;
        }

        TxtUkupnaRazlika.Text = $"{tot:N2} RSD";
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new NivelacijaStavka { RedniBroj = _stavke.Count + 1, KolicinaZaliha = 1m });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is NivelacijaStavka st)
        {
            _stavke.Remove(st);
            int rb = 1;
            foreach (var item in _stavke) item.RedniBroj = rb++;
        }
    }

    private void DgStavke_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(Preračunaj), System.Windows.Threading.DispatcherPriority.Background);
    }

    private async void BtnSačuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtBrojNivelacije.Text))
        {
            MessageBox.Show("Molimo unesite broj nivelacije.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbMagacin.SelectedValue == null)
        {
            MessageBox.Show("Molimo izaberite magacin.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_stavke.Count == 0)
        {
            MessageBox.Show("Nivelacija mora sadržati bar jednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nivelacija = new NivelacijaCena
        {
            NivelacijaCenaId = _existingId,
            BrojNivelacije = TxtBrojNivelacije.Text.Trim(),
            DatumNivelacije = DpDatum.SelectedDate ?? DateTime.Now,
            MagacinId = (int)CmbMagacin.SelectedValue,
            Stavke = _stavke.ToList()
        };

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;
            using var db = new AccountingDbContext(options);

            var service = new NivelacijaService(db);
            await service.SaveNivelacijuAsync(nivelacija);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
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

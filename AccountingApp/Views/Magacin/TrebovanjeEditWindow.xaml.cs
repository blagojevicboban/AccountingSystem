using System.Collections.ObjectModel;
using System.Windows;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class TrebovanjeEditWindow : Window
{
    private readonly ObservableCollection<TrebovanjeStavka> _stavke = new();
    private readonly int _postojeciId;

    public TrebovanjeEditWindow()
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        DpDatum.SelectedDate = DateTime.Now;

        LoadMagacine();
    }

    public TrebovanjeEditWindow(TrebovanjeNalog postojeci)
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        _postojeciId = postojeci.TrebovanjeNalogId;

        if (postojeci.IsKnjizen)
        {
            MessageBox.Show($"Trebovanje br. {postojeci.BrojNaloga} je proknjiženo i nisu dozvoljene nikakve izmene.", "Izmena nije moguća", MessageBoxButton.OK, MessageBoxImage.Warning);
            IsEnabled = false;
        }

        Title = $"Izmena trebovanja br. {postojeci.BrojNaloga}";
        TxtBrojNaloga.Text = postojeci.BrojNaloga;
        TxtBrojNaloga.IsReadOnly = true;
        DpDatum.SelectedDate = postojeci.Datum;

        foreach (var s in postojeci.Stavke.OrderBy(s => s.RedniBroj))
        {
            _stavke.Add(new TrebovanjeStavka { RedniBroj = s.RedniBroj, SifraArtikla = s.SifraArtikla, Kolicina = s.Kolicina, KontoTroska = s.KontoTroska });
        }

        LoadMagacine(postojeci.SifraMagacina);
    }

    private async void LoadMagacine(string? selektujSifru = null)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);

            var magacini = await service.GetMagaciniAsync();
            CmbMagacin.ItemsSource = magacini;
            if (selektujSifru != null)
            {
                var m = magacini.FirstOrDefault(x => x.SifraMagacina == selektujSifru);
                CmbMagacin.SelectedItem = m ?? (magacini.Count > 0 ? magacini[0] : null);
            }
            else if (magacini.Count > 0)
            {
                CmbMagacin.SelectedIndex = 0;
            }

            if (_postojeciId == 0)
            {
                var brojevi = await db.TrebovanjeNalozi.Select(n => n.BrojNaloga).ToListAsync();
                int max = 0;
                foreach (var b in brojevi)
                {
                    if (int.TryParse(b, out var v) && v > max) max = v;
                }
                TxtBrojNaloga.Text = (max + 1).ToString();
            }
        }
        catch
        {
            // Predlog broja/magacina je pogodnost — nije blokirajuće ako ne uspe.
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        _stavke.Add(new TrebovanjeStavka { RedniBroj = _stavke.Count + 1 });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is TrebovanjeStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            DgStavke.Items.Refresh();
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtBrojNaloga.Text))
        {
            MessageBox.Show("Unesite broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_stavke.Count == 0)
        {
            MessageBox.Show("Dodajte bar jednu stavku trebovanja.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        foreach (var s in _stavke)
        {
            if (string.IsNullOrWhiteSpace(s.SifraArtikla))
            {
                MessageBox.Show("Svaka stavka mora imati šifru artikla.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new TrebovanjeService(db);

            var noveStavke = new List<TrebovanjeStavka>();
            int red = 1;
            foreach (var s in _stavke)
            {
                noveStavke.Add(new TrebovanjeStavka
                {
                    RedniBroj = red++,
                    SifraArtikla = s.SifraArtikla.Trim(),
                    Kolicina = s.Kolicina,
                    KontoTroska = s.KontoTroska
                });
            }

            if (_postojeciId == 0)
            {
                var nalog = new TrebovanjeNalog
                {
                    BrojNaloga = TxtBrojNaloga.Text.Trim(),
                    Datum = DpDatum.SelectedDate ?? DateTime.Now,
                    SifraMagacina = magacin.SifraMagacina
                };
                nalog.Stavke.AddRange(noveStavke);
                await service.SaveTrebovanjeAsync(nalog);
            }
            else
            {
                await service.UpdateTrebovanjeAsync(_postojeciId, DpDatum.SelectedDate ?? DateTime.Now, magacin.SifraMagacina, noveStavke);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

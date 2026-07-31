using System.Collections.ObjectModel;
using System.Windows;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class UlazEditWindow : Window
{
    private readonly ObservableCollection<UlazStavka> _stavke = new();
    private readonly int _postojeciId;

    public UlazEditWindow()
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        DpDatum.SelectedDate = DateTime.Now;

        LoadMagacine();
    }

    public UlazEditWindow(UlazNalog postojeci)
    {
        InitializeComponent();
        DgStavke.ItemsSource = _stavke;
        _postojeciId = postojeci.UlazNalogId;

        if (postojeci.IsKnjizen)
        {
            MessageBox.Show($"Ulaz br. {postojeci.BrojNaloga} je proknjižen i nisu dozvoljene nikakve izmene.", "Izmena nije moguća", MessageBoxButton.OK, MessageBoxImage.Warning);
            IsEnabled = false;
        }

        Title = $"Izmena ulaza br. {postojeci.BrojNaloga}";
        TxtBrojNaloga.Text = postojeci.BrojNaloga.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        DpDatum.SelectedDate = postojeci.Datum;
        TxtBrojRacuna.Text = postojeci.BrojRacuna ?? "";

        foreach (var s in postojeci.Stavke.OrderBy(s => s.RedniBroj))
        {
            _stavke.Add(new UlazStavka { RedniBroj = s.RedniBroj, SifraArtikla = s.SifraArtikla, Kolicina = s.Kolicina, Cena = s.Cena, Iznos = s.Iznos });
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
                int max = await db.UlazNalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0;
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
        _stavke.Add(new UlazStavka { RedniBroj = _stavke.Count + 1 });
    }

    private void BtnObrisiStavku_Click(object sender, RoutedEventArgs e)
    {
        if (DgStavke.SelectedItem is UlazStavka selektovana)
        {
            _stavke.Remove(selektovana);
            int i = 1;
            foreach (var s in _stavke) s.RedniBroj = i++;
            DgStavke.Items.Refresh();
        }
    }

    private async void BtnSnimi_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga))
        {
            MessageBox.Show("Unesite ispravan broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin)
        {
            MessageBox.Show("Izaberite magacin.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_stavke.Count == 0)
        {
            MessageBox.Show("Dodajte bar jednu stavku ulaza.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var service = new UlazService(db);

            var noveStavke = new List<UlazStavka>();
            int red = 1;
            foreach (var s in _stavke)
            {
                noveStavke.Add(new UlazStavka
                {
                    RedniBroj = red++,
                    SifraArtikla = s.SifraArtikla.Trim(),
                    Kolicina = s.Kolicina,
                    Cena = s.Cena,
                    Iznos = s.Kolicina * s.Cena
                });
            }

            if (_postojeciId == 0)
            {
                var nalog = new UlazNalog
                {
                    BrojNaloga = brojNaloga,
                    Datum = DpDatum.SelectedDate ?? DateTime.Now,
                    SifraMagacina = magacin.SifraMagacina,
                    BrojRacuna = TxtBrojRacuna.Text.Trim()
                };
                nalog.Stavke.AddRange(noveStavke);
                await service.SaveUlazAsync(nalog);
            }
            else
            {
                await service.UpdateUlazAsync(_postojeciId, DpDatum.SelectedDate ?? DateTime.Now, magacin.SifraMagacina, TxtBrojRacuna.Text.Trim(), noveStavke);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri snimanju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

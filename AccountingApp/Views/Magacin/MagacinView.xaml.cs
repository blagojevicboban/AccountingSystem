using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class MagacinView : UserControl
{
    private List<Artikal> _sviArtikli = new();
    private List<UlazNalog> _sviUlazi = new();
    private List<TrebovanjeNalog> _svaTrebovanja = new();

    public MagacinView()
    {
        InitializeComponent();
        LoadAllData();
    }

    private void LoadAllData()
    {
        LoadMagaciniIArtikli();
        LoadUlazi();
        LoadTrebovanja();
        LoadPrimopredaje();
    }

    private async void LoadMagaciniIArtikli()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);

            CmbMagacin.ItemsSource = await service.GetMagaciniAsync();
            if (CmbMagacin.Items.Count > 0) CmbMagacin.SelectedIndex = 0;

            _sviArtikli = await service.GetArtikliAsync();
            LstArtikli.ItemsSource = _sviArtikli;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju magacina/artikala: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtPretragaArtikla_TextChanged(object sender, TextChangedEventArgs e)
    {
        string search = TxtPretragaArtikla.Text.Trim().ToLower();
        LstArtikli.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviArtikli
            : _sviArtikli.Where(a => a.SifraArtikla.ToLower().Contains(search) || a.Naziv.ToLower().Contains(search)).ToList();
    }

    private void CmbMagacin_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadKarticaMaterijala();
    private void LstArtikli_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadKarticaMaterijala();

    private async void LoadKarticaMaterijala()
    {
        if (CmbMagacin.SelectedItem is not AccountingData.Models.Magacin magacin || LstArtikli.SelectedItem is not Artikal artikal)
        {
            return;
        }

        TxtNaslovArtikla.Text = $"{artikal.Naziv} ({artikal.SifraArtikla}) — {magacin.NazivMagacina}";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new MaterijalnaKarticaService(db);

            var kartica = await service.GetKarticaAsync(magacin.SifraMagacina, artikal.SifraArtikla);
            DgKarticaMaterijala.ItemsSource = kartica;

            var (stanje, saldo) = await service.GetTrenutnoStanjeAsync(magacin.SifraMagacina, artikal.SifraArtikla);
            decimal prosecnaCena = stanje != 0 ? saldo / stanje : 0;
            TxtStanjeArtikla.Text = $"Trenutno stanje: {stanje:N2} {artikal.JedinicaMere} | Prosečna cena: {prosecnaCena:N2} RSD | Vrednost zaliha: {saldo:N2} RSD";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ===================== ULAZI =====================

    private async void LoadUlazi()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new UlazService(db);

            _sviUlazi = await service.GetUlaziAsync();
            ApplyFilterUlazi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterUlazi()
    {
        string search = TxtPretragaUlaz.Text.Trim().ToLower();
        DgUlazi.ItemsSource = string.IsNullOrEmpty(search)
            ? _sviUlazi
            : _sviUlazi.Where(n => n.BrojNaloga.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaUlaz_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterUlazi();

    private void DgUlazi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgUlazStavke.ItemsSource = DgUlazi.SelectedItem is UlazNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNoviUlaz_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new UlazEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadUlazi();
        }
    }

    private async void BtnKnjiziUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (DgUlazi.SelectedItem is not UlazNalog selektovan)
        {
            MessageBox.Show("Izaberite ulaz za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovan.IsKnjizen)
        {
            MessageBox.Show($"Ulaz #{selektovan.BrojNaloga} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new UlazService(db);

            await service.KnjiziUlazAsync(selektovan.UlazNalogId);
            MessageBox.Show($"Ulaz #{selektovan.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadUlazi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== TREBOVANJA =====================

    private async void LoadTrebovanja()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new TrebovanjeService(db);

            _svaTrebovanja = await service.GetTrebovanjaAsync();
            ApplyFilterTrebovanja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilterTrebovanja()
    {
        string search = TxtPretragaTrebovanje.Text.Trim().ToLower();
        DgTrebovanja.ItemsSource = string.IsNullOrEmpty(search)
            ? _svaTrebovanja
            : _svaTrebovanja.Where(n => n.BrojNaloga.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaTrebovanje_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterTrebovanja();

    private void DgTrebovanja_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgTrebovanjeStavke.ItemsSource = DgTrebovanja.SelectedItem is TrebovanjeNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNovoTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new TrebovanjeEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadTrebovanja();
        }
    }

    private async void BtnKnjiziTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (DgTrebovanja.SelectedItem is not TrebovanjeNalog selektovano)
        {
            MessageBox.Show("Izaberite trebovanje za knjiženje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Trebovanje #{selektovano.BrojNaloga} je već proknjiženo!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new TrebovanjeService(db);

            await service.KnjiziTrebovanjeAsync(selektovano.TrebovanjeNalogId);
            MessageBox.Show($"Trebovanje #{selektovano.BrojNaloga} je uspešno proknjiženo!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadTrebovanja();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== PRIMOPREDAJE (M4) =====================

    private List<PrimopredajaNalog> _svePrimopredaje = new();

    private async void LoadPrimopredaje()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            _svePrimopredaje = await service.GetPrimopredajeAsync();
            ApplyFilterPrimopredaja();
        }
        catch { }
    }

    private void ApplyFilterPrimopredaja()
    {
        string search = TxtPretragaPrimopredaja.Text.Trim().ToLower();
        DgPrimopredaje.ItemsSource = string.IsNullOrEmpty(search)
            ? _svePrimopredaje
            : _svePrimopredaje.Where(n => n.BrojNaloga.ToLower().Contains(search) || n.SifraMagacinaDaje.ToLower().Contains(search) || n.SifraMagacinaPrima.ToLower().Contains(search)).ToList();
    }

    private void TxtPretragaPrimopredaja_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterPrimopredaja();

    private void DgPrimopredaje_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DgPrimopredajaStavke.ItemsSource = DgPrimopredaje.SelectedItem is PrimopredajaNalog nalog ? nalog.Stavke : null;
    }

    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new PrimopredajaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadPrimopredaje();
        }
    }

    private async void BtnKnjiziPrimopredaju_Click(object sender, RoutedEventArgs e)
    {
        if (DgPrimopredaje.SelectedItem is not PrimopredajaNalog selektovano)
        {
            MessageBox.Show("Izaberite primopredaju sa liste.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (selektovano.IsKnjizen)
        {
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je već proknjižena!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var service = new PrimopredajaService(db);

            await service.KnjiziPrimopredajuAsync(selektovano.PrimopredajaNalogId);
            MessageBox.Show($"Primopredaja #{selektovano.BrojNaloga} je uspešno proknjižena u materijalnom knjigovodstvu!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPrimopredaje();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri knjiženju primopredaje: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

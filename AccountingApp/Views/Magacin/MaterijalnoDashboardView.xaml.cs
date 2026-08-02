using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public class UlazRedDto
{
    public int UlazNalogId { get; set; }
    public int BrojNaloga { get; set; }
    public DateTime Datum { get; set; }
    public string SifraMagacina { get; set; } = "";
    public string StatusText { get; set; } = "";
}

public class TrebovanjeRedDto
{
    public int TrebovanjeNalogId { get; set; }
    public int BrojNaloga { get; set; }
    public DateTime Datum { get; set; }
    public string SifraMagacina { get; set; } = "";
    public string StatusText { get; set; } = "";
}

public class TopMaterijalRedDto
{
    public string SifraArtikla { get; set; } = "";
    public string NazivArtikla { get; set; } = "";
    public decimal VrednostZaliha { get; set; }
    public decimal Promet { get; set; }
}

public partial class MaterijalnoDashboardView : UserControl
{
    public MaterijalnoDashboardView()
    {
        InitializeComponent();
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            string dbPath = AppConfig.DbPath;
            if (!System.IO.File.Exists(dbPath)) return;

            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={dbPath}").Options;
            using var db = new AccountingDbContext(options);

            // ===== VREDNOST ZALIHA MATERIJALA =====
            var bilansRedovi = await RobniBrutoBilansService.GetMaterijalniBrutoBilansAsync(db);

            decimal vrednostUkupno = bilansRedovi.Sum(r => r.SaldoVrednosni);
            int brojMaterijala = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Sum(r => r.SaldoKolicinski) != 0);
            int negativnaStanja = bilansRedovi.Count(r => r.SaldoKolicinski < 0 || r.Cena < 0);

            TxtVrednostZaliha.Text = $"{vrednostUkupno:N2} RSD";
            TxtBrojMaterijala.Text = $"{brojMaterijala}";
            TxtNegativnaStanja.Text = $"{negativnaStanja}";

            // ===== TOP MATERIJALI PO PROMETU / VREDNOSTI =====
            var topMaterijali = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TopMaterijalRedDto
                {
                    SifraArtikla = g.Key,
                    NazivArtikla = g.First().NazivArtikla,
                    VrednostZaliha = g.Sum(r => r.SaldoVrednosni),
                    Promet = g.Sum(r => r.UlazVrednost + r.IzlazVrednost)
                })
                .OrderByDescending(x => x.VrednostZaliha)
                .Take(10)
                .ToList();
            DgTopMaterijali.ItemsSource = topMaterijali;

            // ===== POSLEDNJI ULAZI =====
            var ulazi = await new UlazService(db).GetUlaziAsync();
            DgPoslednjiUlazi.ItemsSource = ulazi
                .OrderByDescending(u => u.Datum)
                .Take(8)
                .Select(u => new UlazRedDto
                {
                    UlazNalogId = u.UlazNalogId,
                    BrojNaloga = u.BrojNaloga,
                    Datum = u.Datum,
                    SifraMagacina = u.SifraMagacina,
                    StatusText = u.IsKnjizen ? "Proknjižen" : "U pripremi"
                })
                .ToList();

            // ===== POSLEDNJA TREBOVANJA =====
            var trebovanja = await new TrebovanjeService(db).GetTrebovanjaAsync();
            DgPoslednjaTrebovanja.ItemsSource = trebovanja
                .OrderByDescending(t => t.Datum)
                .Take(8)
                .Select(t => new TrebovanjeRedDto
                {
                    TrebovanjeNalogId = t.TrebovanjeNalogId,
                    BrojNaloga = t.BrojNaloga,
                    Datum = t.Datum,
                    SifraMagacina = t.SifraMagacina,
                    StatusText = t.IsKnjizen ? "Proknjiženo" : "U pripremi"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri učitavanju radne table Materijalno");
        }
    }

    // ===== BRZE AKCIJE =====
    private void BtnNoviUlaz_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new UlazEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovoTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new TrebovanjeEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovaPrimopredaja_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new PrimopredajaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private async void BtnOtvoriUlaz_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not UlazRedDto red) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var puna = await db.UlazNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.UlazNalogId == red.UlazNalogId);
            if (puna == null) return;

            var dijalog = new UlazEditWindow(puna) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju ulaza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOtvoriTrebovanje_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not TrebovanjeRedDto red) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var puna = await db.TrebovanjeNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.TrebovanjeNalogId == red.TrebovanjeNalogId);
            if (puna == null) return;

            var dijalog = new TrebovanjeEditWindow(puna) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true) LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju trebovanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

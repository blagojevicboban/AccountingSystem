using System.Windows;
using System.Windows.Controls;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Trgovina;

public class KalkulacijaRedDto
{
    public string Tip { get; set; } = "";
    public int Id { get; set; }
    public int BrojKalkulacije { get; set; }
    public DateTime Datum { get; set; }
    public string StatusText { get; set; } = "";
}

public class TopArtikalRedDto
{
    public string SifraArtikla { get; set; } = "";
    public string NazivArtikla { get; set; } = "";
    public decimal VrednostZaliha { get; set; }
    public decimal Promet { get; set; }
}

public partial class RobnoDashboardView : UserControl
{
    public RobnoDashboardView()
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

            // ===== VREDNOST ZALIHA (VP/MP) =====
            var bilansRedovi = await RobniBrutoBilansService.GetRobniBrutoBilansAsync(db);
            var magaciniVrsta = await db.Magacini.ToDictionaryAsync(m => m.SifraMagacina, m => m.VrstaMagacina, StringComparer.OrdinalIgnoreCase);

            decimal vrednostVp = 0, vrednostMp = 0, vrednostUkupno = 0;
            int brojArtikala = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Sum(r => r.SaldoKolicinski) != 0);

            foreach (var red in bilansRedovi)
            {
                vrednostUkupno += red.SaldoVrednosni;
                string vrsta = magaciniVrsta.TryGetValue(red.SifraMagacina, out var v) ? v : "Veleprodaja";
                if (vrsta == "Maloprodaja") vrednostMp += red.SaldoVrednosni;
                else vrednostVp += red.SaldoVrednosni;
            }

            TxtVrednostZalihaVp.Text = $"{vrednostVp:N2} RSD";
            TxtVrednostZalihaMp.Text = $"{vrednostMp:N2} RSD";
            TxtVrednostZalihaUkupno.Text = $"{vrednostUkupno:N2} RSD";
            TxtBrojArtikala.Text = $"{brojArtikala} artikala na zalihi";

            // ===== TOP ARTIKLI PO PROMETU / VREDNOSTI =====
            var topArtikli = bilansRedovi
                .GroupBy(r => r.SifraArtikla, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TopArtikalRedDto
                {
                    SifraArtikla = g.Key,
                    NazivArtikla = g.First().NazivArtikla,
                    VrednostZaliha = g.Sum(r => r.SaldoVrednosni),
                    Promet = g.Sum(r => r.UlazVrednost + r.IzlazVrednost)
                })
                .OrderByDescending(x => x.VrednostZaliha)
                .Take(10)
                .ToList();
            DgTopArtikli.ItemsSource = topArtikli;

            // ===== POSLEDNJE KALKULACIJE (VP + MP) =====
            var vpKalk = await db.Kalkulacije.OrderByDescending(k => k.Datum).Take(8).ToListAsync();
            var mpKalk = await db.MaloprodajneKalkulacije.OrderByDescending(k => k.Datum).Take(8).ToListAsync();

            var recentKalkulacije = vpKalk.Select(k => new KalkulacijaRedDto
                {
                    Tip = "VP",
                    Id = k.KalkulacijaId,
                    BrojKalkulacije = k.BrojKalkulacije,
                    Datum = k.Datum,
                    StatusText = k.IsKnjizen ? "Proknjižena" : "U pripremi"
                })
                .Concat(mpKalk.Select(k => new KalkulacijaRedDto
                {
                    Tip = "MP",
                    Id = k.MaloprodajnaKalkulacijaId,
                    BrojKalkulacije = k.BrojKalkulacije,
                    Datum = k.Datum,
                    StatusText = k.IsKnjizen ? "Proknjižena" : "U pripremi"
                }))
                .OrderByDescending(k => k.Datum)
                .Take(8)
                .ToList();
            DgPoslednjeKalkulacije.ItemsSource = recentKalkulacije;

            // ===== POSLEDNJE NIVELACIJE =====
            var nivelacije = await NivelacijaService.GetNivelacijeAsync(db);
            DgPoslednjeNivelacije.ItemsSource = nivelacije.Take(8).ToList();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri učitavanju radne table Robno");
        }
    }

    // ===== BRZE AKCIJE =====
    private void BtnNovaKalkulacijaVp_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new KalkulacijaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovaKalkulacijaMp_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new MaloprodajnaKalkulacijaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private void BtnNovaNivelacija_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var dijalog = new NivelacijaEditWindow(db) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true)
            {
                _ = NivelacijaService.SaveNivelacijaAsync(db, dijalog.Nivelacija);
                LoadData();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri kreiranju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnNovaOtpremnica_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new RacunOtpremnicaEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true) LoadData();
    }

    private async void BtnOtvoriKalkulaciju_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not KalkulacijaRedDto red) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            if (red.Tip == "VP")
            {
                var puna = await db.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == red.Id);
                if (puna == null) return;
                var dijalog = new KalkulacijaEditWindow(puna) { Owner = Window.GetWindow(this) };
                if (dijalog.ShowDialog() == true) LoadData();
            }
            else
            {
                var puna = await db.MaloprodajneKalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == red.Id);
                if (puna == null) return;
                var dijalog = new MaloprodajnaKalkulacijaEditWindow(puna) { Owner = Window.GetWindow(this) };
                if (dijalog.ShowDialog() == true) LoadData();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju kalkulacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnOtvoriNivelaciju_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not NivelacijaCena niv) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);

            var puna = await NivelacijaService.GetNivelacijaByIdAsync(db, niv.NivelacijaCenaId);
            if (puna == null) return;

            var dijalog = new NivelacijaEditWindow(db, puna) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true)
            {
                await NivelacijaService.SaveNivelacijaAsync(db, dijalog.Nivelacija);
                LoadData();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju nivelacije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

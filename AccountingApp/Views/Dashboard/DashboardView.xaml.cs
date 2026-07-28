using System;
using System.Linq;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Measure;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace AccountingApp.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
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

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using var db = new AccountingDbContext(options);

            int proknjizenoCount = await db.Nalozi.CountAsync(n => n.IsKnjizen);
            int stavkiCount = await db.StavkeNaloga.CountAsync(s => s.Nalog != null && s.Nalog.IsKnjizen);
            int neproknjizenoCount = await db.Nalozi.CountAsync(n => !n.IsKnjizen);
            int kontaCount = await db.Konta.CountAsync();
            int matCount = await db.Artikli.CountAsync();
            int partneriCount = await db.Partneri.CountAsync();
            int magacinaCount = await db.Magacini.CountAsync();

            TxtUkupnoNaloga.Text = proknjizenoCount.ToString("N0");
            TxtStavkiKnjizenja.Text = $"{stavkiCount:N0} stavki knjiženja";
            TxtUkupnoKonta.Text = kontaCount.ToString("N0");
            TxtUkupnoMaterijala.Text = matCount.ToString("N0");
            TxtBrojMagacina.Text = $"{magacinaCount:N0} magacina";
            TxtUkupnoPartnera.Text = partneriCount.ToString("N0");

            // ===== ROBNO KNJIGOVODSTVO =====
            int nezaknjizenoKalk = await db.Kalkulacije.CountAsync(k => !k.IsKnjizen);
            int nezaknjizenoRacuni = await db.RacuniOtpremnice.CountAsync(r => !r.IsKnjizen);
            int nezaknjizenoPrimopredaje = await db.PrimopredajaNalozi.CountAsync(p => !p.IsKnjizen);
            int nezaknjizenoNivelacije = await db.NivelacijeCena.CountAsync(n => !n.IsKnjizen);
            int ukupnoNezaknjizeno = nezaknjizenoKalk + nezaknjizenoRacuni + nezaknjizenoPrimopredaje + nezaknjizenoNivelacije;

            TxtNezaknjizeniDokumenti.Text = ukupnoNezaknjizeno.ToString("N0");
            TxtNezaknjizeniDetalji.Text = $"Kalk: {nezaknjizenoKalk}, Rač: {nezaknjizenoRacuni}, Prim: {nezaknjizenoPrimopredaje}, Niv: {nezaknjizenoNivelacije}";

            decimal ukupnoFakturisano = await db.RacuniOtpremnice.SumAsync(r => (decimal?)r.UkupanIznos) ?? 0m;
            int brojRacuna = await db.RacuniOtpremnice.CountAsync();
            int brojKalkulacija = await db.Kalkulacije.CountAsync();

            TxtUkupnoFakturisano.Text = $"{ukupnoFakturisano:N0} RSD";
            TxtBrojRacuna.Text = $"{brojRacuna:N0} računa-otpremnica";
            TxtBrojKalkulacija.Text = brojKalkulacija.ToString("N0");

            // ===== MATERIJALNO KNJIGOVODSTVO =====
            var brutoBilans = await RobniBrutoBilansService.GetRobniBrutoBilansAsync(db);
            decimal vrednostZaliha = brutoBilans.Sum(r => r.SaldoVrednosni);
            int negativnaStanja = brutoBilans.Count(r => r.SaldoKolicinski < 0);

            TxtVrednostZaliha.Text = $"{vrednostZaliha:N0} RSD";
            TxtNegativnaStanja.Text = negativnaStanja.ToString("N0");

            var recentNalozi = await db.Nalozi
                .OrderByDescending(n => n.NalogId)
                .Take(15)
                .ToListAsync();

            DgRecentNalozi.ItemsSource = recentNalozi;

            // Status naloga (Donut)
            PieStatusNaloga.Series = new ISeries[]
            {
                new PieSeries<int> { Values = new[] { proknjizenoCount }, Name = "Proknjiženi", InnerRadius = 40 },
                new PieSeries<int> { Values = new[] { neproknjizenoCount }, Name = "Neproknjiženi", InnerRadius = 40 }
            };

            // Promet po kontu — Top 10 (horizontalni bar, najveći na vrhu)
            var brutoBilansService = new BrutoBilansService(db);
            var bilansPoKontu = await brutoBilansService.GetBrutoBilansAsync();
            var top10Konta = bilansPoKontu
                .OrderByDescending(r => r.Duguje + r.Potrazuje)
                .Take(10)
                .ToList();
            top10Konta.Reverse();

            BarPrometKonta.Series = new ISeries[]
            {
                new RowSeries<double>
                {
                    Values = top10Konta.Select(k => (double)(k.Duguje + k.Potrazuje)).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                    DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#334155")),
                    DataLabelsPosition = DataLabelsPosition.End,
                    DataLabelsFormatter = point => point.Model.ToString("N0"),
                    XToolTipLabelFormatter = point => point.Model.ToString("N2")
                }
            };
            BarPrometKonta.YAxes = new Axis[]
            {
                new Axis
                {
                    Labels = top10Konta.Select(k => $"{k.BrojKonta} {k.NazivKonta}").ToArray(),
                    TextSize = 11
                }
            };

            // Top 5 partnera po prometu (Bar)
            var otvoreneStavkeService = new OtvoreneStavkeService(db);
            var bilansAnalitike = await otvoreneStavkeService.GetBrutoBilansAnalitikeAsync();
            var top5Partnera = bilansAnalitike
                .OrderByDescending(r => r.Duguje + r.Potrazuje)
                .Take(5)
                .ToList();

            if (top5Partnera.Count == 0)
            {
                BarTopPartneri.Visibility = System.Windows.Visibility.Collapsed;
                TxtNemaPartnera.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                BarTopPartneri.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = top5Partnera.Select(p => (double)(p.Duguje + p.Potrazuje)).ToArray(),
                        Name = "Promet",
                        Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                        DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#334155")),
                        DataLabelsPosition = DataLabelsPosition.Top,
                        DataLabelsFormatter = point => point.Model.ToString("N0"),
                        YToolTipLabelFormatter = point => $"{point.Model:N2}"
                    }
                };
                BarTopPartneri.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = top5Partnera.Select(p => p.NazivPartnera).ToArray(),
                        LabelsRotation = 15,
                        TextSize = 12
                    }
                };
            }
        }
        catch
        {
        }
    }
}

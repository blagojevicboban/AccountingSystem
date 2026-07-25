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

            TxtUkupnoNaloga.Text = proknjizenoCount.ToString("N0");
            TxtStavkiKnjizenja.Text = $"{stavkiCount:N0} stavki knjiženja";
            TxtUkupnoKonta.Text = kontaCount.ToString("N0");
            TxtUkupnoMaterijala.Text = matCount.ToString("N0");
            TxtUkupnoPartnera.Text = partneriCount.ToString("N0");

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

            // Promet po kontu — Top 10 (Pie)
            var brutoBilansService = new BrutoBilansService(db);
            var bilansPoKontu = await brutoBilansService.GetBrutoBilansAsync();
            var top10Konta = bilansPoKontu
                .OrderByDescending(r => r.Duguje + r.Potrazuje)
                .Take(10)
                .ToList();

            PiePrometKonta.Series = top10Konta.Select(k => (ISeries)new PieSeries<double>
            {
                Values = new[] { (double)(k.Duguje + k.Potrazuje) },
                Name = $"{k.BrojKonta} {k.NazivKonta}",
                ToolTipLabelFormatter = point => $"{point.Context.Series.Name}: {point.Model:N2}"
            }).ToArray();

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

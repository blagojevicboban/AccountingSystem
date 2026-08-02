using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AccountingApp.Views.Izvestaji;
using AccountingData;
using AccountingData.Models;
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

            // ===== OSNOVNE STATISTIKE =====
            int proknjizenoCount = await db.Nalozi.CountAsync(n => n.IsKnjizen);
            int stavkiCount = await db.StavkeNaloga.CountAsync(s => s.Nalog != null && s.Nalog.IsKnjizen);
            int neproknjizenoCount = await db.Nalozi.CountAsync(n => !n.IsKnjizen);
            int kontaCount = await db.Konta.CountAsync();
            int partneriCount = await db.Partneri.CountAsync();

            TxtUkupnoNaloga.Text = proknjizenoCount.ToString("N0");
            TxtStavkiKnjizenja.Text = $"{stavkiCount:N0}";
            TxtNeproknjizenoNaloga.Text = neproknjizenoCount.ToString("N0");
            TxtUkupnoKonta.Text = kontaCount.ToString("N0");
            TxtUkupnoPartnera.Text = partneriCount.ToString("N0");

            var firma = AppSession.TrenutnaFirma ?? await db.Firme.FirstOrDefaultAsync();
            if (firma != null)
            {
                TxtFirmaNaziv.Text = firma.Naziv;
                TxtFirmaAdresa.Text = string.IsNullOrWhiteSpace(firma.Adresa) ? "—" : $"{firma.Adresa}, {firma.PttIMesto}";
                TxtFirmaZiro.Text = string.IsNullOrWhiteSpace(firma.ZiroRacun) ? "—" : firma.ZiroRacun;
            }

            // ===== OTVORENE STAVKE (IOS & LIKVIDNOST) =====
            var iosService = new OtvoreneStavkeService(db);

            // Kupci (prefix 204...)
            var kupciIos = await iosService.GetIosIzvestajAsync("204", "2049999", null, null, true);
            decimal ukupnoKupciDug = kupciIos.Where(k => k.Saldo > 0).Sum(k => k.Saldo);
            int brojKupacaDug = kupciIos.Count(k => k.Saldo > 0);

            TxtUkupnoKupciDug.Text = $"{ukupnoKupciDug:N2} RSD";
            TxtBrojKupacaDug.Text = $"{brojKupacaDug} kupca ima otvoreno dugovanje";

            var topKupci = kupciIos
                .Where(k => k.Saldo > 0)
                .OrderByDescending(k => k.Saldo)
                .Take(5)
                .ToList();
            DgTopKupci.ItemsSource = topKupci;

            // Dobavljači (prefix 435...)
            var dobavljaciIos = await iosService.GetIosIzvestajAsync("435", "4359999", null, null, true);
            decimal ukupnoDobavljaciDug = dobavljaciIos.Where(d => d.Saldo < 0 || d.UkupnoPotrazuje > d.UkupnoDuguje).Sum(d => Math.Abs(d.Saldo));
            int brojDobavljaciDug = dobavljaciIos.Count(d => d.Saldo < 0 || d.UkupnoPotrazuje > d.UkupnoDuguje);

            TxtUkupnoDobavljaciDug.Text = $"{ukupnoDobavljaciDug:N2} RSD";
            TxtBrojDobavljaciDug.Text = $"{brojDobavljaciDug} dobavljača sa obavezama";

            var topDobavljaci = dobavljaciIos
                .Where(d => d.UkupnoPotrazuje > d.UkupnoDuguje || d.Saldo < 0)
                .OrderByDescending(d => Math.Abs(d.Saldo))
                .Take(5)
                .ToList();
            DgTopDobavljaci.ItemsSource = topDobavljaci;

            // Neto Saldo Likvidnosti (Kupci - Dobavljači)
            decimal netoLikvidnost = ukupnoKupciDug - ukupnoDobavljaciDug;
            TxtNetoSaldoLikvidnost.Text = $"{netoLikvidnost:N2} RSD";

            if (netoLikvidnost >= 0)
            {
                TxtNetoSaldoLikvidnost.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
                TxtNetoOpis.Text = "Pozitivan neto priliv (Potraživanja > Obaveze)";
                TxtNetoOpis.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            }
            else
            {
                TxtNetoSaldoLikvidnost.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                TxtNetoOpis.Text = "Upozorenje: Obaveze veće od potraživanja";
                TxtNetoOpis.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }

            // ===== RECENT NALOZI =====
            var recentNalozi = await db.Nalozi
                .OrderByDescending(n => n.NalogId)
                .Take(10)
                .ToListAsync();

            DgRecentNalozi.ItemsSource = recentNalozi;

            // ===== GRAFIKONI =====
            PieStatusNaloga.Series = new ISeries[]
            {
                new PieSeries<int> { Values = new[] { proknjizenoCount }, Name = "Proknjiženi", InnerRadius = 40, Fill = new SolidColorPaint(SKColor.Parse("#2563EB")) },
                new PieSeries<int> { Values = new[] { neproknjizenoCount }, Name = "Neproknjiženi", InnerRadius = 40, Fill = new SolidColorPaint(SKColor.Parse("#F59E0B")) }
            };

            // Promet po kontu Top 10
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
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri učitavanju radne table");
        }
    }

    // ===== BRZE AKCIJE =====
    private async void BtnOpenIos_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dbPath = AppConfig.DbPath;
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new OtvoreneStavkeService(db);
            var grupe = await service.GetIosIzvestajAsync(null, null, null, null, true);
            var firma = AppSession.TrenutnaFirma ?? await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Kompanija" };

            var iosWin = new IosPreviewWindow(grupe, firma);
            iosWin.Owner = Window.GetWindow(this);
            iosWin.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju ekrana otvorenih stavki: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOpenBrutoBilans_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = Window.GetWindow(this) as MainWindow;
        mainWin?.NavIzvestaji_Click(sender, e);
    }

    private void BtnNewNalog_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = Window.GetWindow(this) as MainWindow;
        mainWin?.NavNalozi_Click(sender, e);
    }

    private void BtnDosImport_Click(object sender, RoutedEventArgs e)
    {
        var mainWin = Window.GetWindow(this) as MainWindow;
        mainWin?.NavPodesavanja_Click(sender, e);
    }

    private async void BtnPrintPartnerIos_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is IosPartnerGrupa grupa)
        {
            try
            {
                string dbPath = AppConfig.DbPath;
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var singleList = new List<IosPartnerGrupa> { grupa };
                var firma = AppSession.TrenutnaFirma ?? await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Kompanija" };

                var previewWin = new IosPreviewWindow(singleList, firma);
                previewWin.Owner = Window.GetWindow(this);
                previewWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri prikazu IOS za partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

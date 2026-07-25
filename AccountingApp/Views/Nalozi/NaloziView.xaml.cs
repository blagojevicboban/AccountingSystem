using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Nalozi;

public partial class NaloziView : UserControl
{
    private List<Nalog> _allNalozi = new();

    public NaloziView()
    {
        InitializeComponent();
        RbProknjizeni.IsChecked = true;
        LoadNalozi();
    }

    private async void LoadNalozi()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            _allNalozi = await service.GetNaloziAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        if (DgNalozi == null) return;

        string search = TxtPretraga.Text.Trim().ToLower();

        bool samoProknjizeni = RbProknjizeni?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeni?.IsChecked == true;

        var filtered = _allNalozi.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToLower().Contains(search) || (n.Opis != null && n.Opis.ToLower().Contains(search))) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen)
        ).ToList();

        DgNalozi.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgNalozi.SelectedIndex = 0;
        }
        else
        {
            DgStavke.ItemsSource = null;
        }

        AzurirajDugmad();
    }

    /// <summary>
    /// Izmeni/Proknjiži/Rasknjiži/Štampa zahtevaju izabran nalog u gridu.
    /// </summary>
    private void AzurirajDugmad()
    {
        bool imaSelekciju = DgNalozi.SelectedItem is Nalog;
        BtnIzmeniNalog.IsEnabled = imaSelekciju;
        BtnProknjizi.IsEnabled = imaSelekciju;
        BtnRasknjizi.IsEnabled = imaSelekciju;
        BtnStampa.IsEnabled = imaSelekciju;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void DgNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selectedNalog)
        {
            TxtDetailHeader.Text = $"📋 Stavke naloga #{selectedNalog.BrojNaloga} ({selectedNalog.Opis})";
            DgStavke.ItemsSource = selectedNalog.Stavke;
        }

        AzurirajDugmad();
    }

    private void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new NalogEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }

    private void BtnIzmeniNalog_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog selectedNalog)
        {
            MessageBox.Show("Izaberite nalog za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selectedNalog.IsKnjizen)
        {
            MessageBox.Show("Proknjižen nalog se ne može menjati.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new NalogEditWindow(selectedNalog) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selectedNalog)
        {
            if (selectedNalog.IsKnjizen)
            {
                MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new NaloziService(db);

                await service.KnjiziNalogAsync(selectedNalog.NalogId);
                MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadNalozi();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog selectedNalog)
        {
            MessageBox.Show("Izaberite nalog za rasknjižavanje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selectedNalog.IsKnjizen)
        {
            MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} nije proknjižen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da rasknjižite nalog #{selectedNalog.BrojNaloga}?\n\nNalog će se vratiti u status nacrta i moći će ponovo da se izmeni.",
            "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            await service.RasknjiziNalogAsync(selectedNalog.NalogId);
            MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} je rasknjižen.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadNalozi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnNovaGodina_Click(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Prenos početnog stanja u novu godinu dozvoljen je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var proknjizeniNalozi = _allNalozi.Where(n => n.IsKnjizen).ToList();
        if (proknjizeniNalozi.Count == 0)
        {
            MessageBox.Show("Nema proknjiženih naloga — nema šta da se prenese u novu godinu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int izvornaGodina = proknjizeniNalozi.Max(n => n.DatumNaloga.Year);
        int novaGodina = izvornaGodina + 1;

        var potvrda = MessageBox.Show(
            $"Da li želite da prenesete zaključni saldo konta iz {izvornaGodina}. u {novaGodina}. godinu?\n\n" +
            $"Biće kreiran nalog za početno stanje datiran 01.01.{novaGodina}. sa saldom svakog konta koji ima promet.",
            "Potvrda prenosa u novu godinu", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NovaGodinaService(db);

            var nalog = await service.PrenesiUNovuGoduAsync(izvornaGodina);
            MessageBox.Show(
                $"Preneseno početno stanje u {novaGodina}. godinu — nalog #{nalog.BrojNaloga} sa {nalog.Stavke.Count} stavki (Duguje={nalog.UkupnoDuguje:N2}, Potražuje={nalog.UkupnoPotrazuje:N2}).",
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadNalozi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prenosu u novu godinu: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        var selektovaniNalozi = DgNalozi.SelectedItems.OfType<Nalog>().ToList();

        if (!selektovaniNalozi.Any() && DgNalozi.SelectedItem is Nalog singleNalog)
        {
            selektovaniNalozi.Add(singleNalog);
        }

        if (!selektovaniNalozi.Any())
        {
            MessageBox.Show("Molimo izaberite jedan ili više naloga za štampu.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? AppSession.TrenutnaFirma ?? new Firma { Naziv = "Firma" };

            var nalogIds = selektovaniNalozi.Select(n => n.NalogId).ToList();
            var naloziSaStavkama = await db.Nalozi
                .Include(n => n.Stavke)
                .Where(n => nalogIds.Contains(n.NalogId))
                .ToListAsync();

            var nalogeForPdf = selektovaniNalozi
                .Select(s => naloziSaStavkama.FirstOrDefault(n => n.NalogId == s.NalogId) ?? s)
                .ToList();

            byte[] pdfBytes = PdfReportService.GenerisiNalogePdf(firma, nalogeForPdf);

            string fileName = nalogeForPdf.Count == 1 
                ? $"Nalog_{nalogeForPdf[0].BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                : $"Nalozi_Vise_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            string pdfPath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF štampe: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnProknjiziSve_Click(object sender, RoutedEventArgs e)
    {
        var unpostedCount = _allNalozi.Count(n => !n.IsKnjizen);
        if (unpostedCount == 0)
        {
            MessageBox.Show("Nema neproknjiženih naloga u bazi.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li želite da proknjižite sve neproknjižene naloge (ukupno {unpostedCount} naloga)?",
            "Potvrda masovnog knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            var (proknjizenoCount, neuravnotezeni) = await service.KnjiziSveNalogeAsync();

            string poruka = $"Uspešno je proknjiženo {proknjizenoCount} naloga!";
            if (neuravnotezeni.Count > 0)
            {
                poruka += $"\n\nSledeći nalozi nisu u ravnoteži i ostali su neproknjiženi: {string.Join(", ", neuravnotezeni)}";
            }

            MessageBox.Show(poruka, "Masovno knjiženje", MessageBoxButton.OK, 
                neuravnotezeni.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            LoadNalozi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri masovnom knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPreknjizavanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new PreknjizavanjeWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }
}

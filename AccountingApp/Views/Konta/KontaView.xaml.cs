using System;
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

namespace AccountingApp.Views.Konta;

public partial class KontaView : UserControl
{
    private List<Konto> _allKonta = new();

    public KontaView()
    {
        InitializeComponent();
        LoadKonta();
    }

    private async void LoadKonta()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KontaService(db);

            _allKonta = await service.GetKontaAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        if (DgKonta == null) return;

        string search = TxtPretraga.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(search)
            ? _allKonta
            : _allKonta.Where(k => k.BrojKonta.ToLower().Contains(search) || k.NazivKonta.ToLower().Contains(search)).ToList();

        DgKonta.ItemsSource = filtered;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnNoviKonto_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new KontoEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadKonta();
        }
    }

    private void BtnIzmeniKonto_Click(object sender, RoutedEventArgs e)
    {
        if (DgKonta.SelectedItem is not Konto selectedKonto)
        {
            MessageBox.Show("Izaberite konto za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        OtvoriIzmenuKonta(selectedKonto);
    }

    private void DgKonta_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) == null) return;
        if (DgKonta.SelectedItem is not Konto selectedKonto) return;

        OtvoriIzmenuKonta(selectedKonto);
    }

    private void OtvoriIzmenuKonta(Konto konto)
    {
        var dijalog = new KontoEditWindow(konto) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadKonta();
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match) return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private async void BtnObrisiKonto_Click(object sender, RoutedEventArgs e)
    {
        if (DgKonta.SelectedItem is not Konto selectedKonto)
        {
            MessageBox.Show("Izaberite konto za brisanje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da obrišete konto {selectedKonto.BrojKonta} ({selectedKonto.NazivKonta})?",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KontaService(db);

            await service.DeleteKontoAsync(selectedKonto.KontoId);
            MessageBox.Show($"Konto {selectedKonto.BrojKonta} je uspešno obrisan.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadKonta();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri brisanju konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnStampaj_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? AppSession.TrenutnaFirma ?? new Firma { Naziv = "Firma" };

            byte[] pdfBytes = PdfReportService.GenerisiKontniPlanPdf(firma, _allKonta);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"KontniPlan_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeApp.Services;
using ERPiFinansijeApp.Views.Nalozi;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Kartice;

public class KontoIzbor : INotifyPropertyChanged
{
    public Konto Konto { get; }
    public KontoIzbor(Konto konto) => Konto = konto;

    public string BrojKonta => Konto.BrojKonta;
    public string NazivKonta => Konto.NazivKonta;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class KarticeView : UserControl
{
    private List<Konto> _svaKonta = new();

    public KarticeView()
    {
        InitializeComponent();
        DpKarticaOd.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpKarticaDo.SelectedDate = DateTime.Today;
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
            var service = new KarticaService(db);

            bool samoSaPrometom = ChkSamoSaPrometom?.IsChecked ?? true;
            _svaKonta = await service.GetKontaAsync(samoSaPrometom);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        LoadKonta();
    }

    private void ApplyFilter()
    {
        if (LstKonta == null) return;

        string search = TxtPretragaKonta?.Text.Trim().ToLower() ?? "";
        var filtered = string.IsNullOrEmpty(search)
            ? _svaKonta
            : _svaKonta.Where(k => k.BrojKonta.ToLower().Contains(search) || k.NazivKonta.ToLower().Contains(search)).ToList();

        var izbori = filtered.Select(k => new KontoIzbor(k)).ToList();
        foreach (var izbor in izbori) izbor.PropertyChanged += KontoIzbor_PropertyChanged;
        LstKonta.ItemsSource = izbori;
        if (filtered.Any())
        {
            LstKonta.SelectedIndex = 0;
        }
        else
        {
            DgKartica.ItemsSource = null;
            TxtNaslovKonta.Text = "Nema konta za prikaz";
            TxtPodnaslovKonta.Text = "";
            TxtSumaDuguje.Text = "0,00";
            TxtSumaPotrazuje.Text = "0,00";
            TxtSumaSaldo.Text = "0,00";
        }

        UpdateBtnStampajIzabraneState();
    }

    private void KontoIzbor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(KontoIzbor.IsSelected))
        {
            UpdateBtnStampajIzabraneState();
        }
    }

    private bool _updatingChkSveKonta;

    private void UpdateBtnStampajIzabraneState()
    {
        var izbori = LstKonta.ItemsSource as List<KontoIzbor>;
        bool imaCekiranih = izbori?.Any(k => k.IsSelected) ?? false;
        bool imaPrikazanuKarticu = LstKonta.SelectedItem is KontoIzbor && (DgKartica.ItemsSource as List<KarticaRed>)?.Count > 0;
        BtnStampajKartice.IsEnabled = imaCekiranih || imaPrikazanuKarticu;

        if (ChkSveKonta == null) return;

        _updatingChkSveKonta = true;
        if (izbori == null || izbori.Count == 0)
            ChkSveKonta.IsChecked = false;
        else if (izbori.All(k => k.IsSelected))
            ChkSveKonta.IsChecked = true;
        else if (izbori.All(k => !k.IsSelected))
            ChkSveKonta.IsChecked = false;
        else
            ChkSveKonta.IsChecked = null;
        _updatingChkSveKonta = false;
    }

    private void ChkSveKonta_Checked(object sender, RoutedEventArgs e) => SetSvaKontaIzabrana(true);

    private void ChkSveKonta_Unchecked(object sender, RoutedEventArgs e) => SetSvaKontaIzabrana(false);

    private void SetSvaKontaIzabrana(bool izabrano)
    {
        if (_updatingChkSveKonta) return;
        if (LstKonta.ItemsSource is not List<KontoIzbor> izbori) return;

        foreach (var izbor in izbori) izbor.IsSelected = izabrano;
        UpdateBtnStampajIzabraneState();
    }

    private void LstKonta_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var red = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (red?.Item is KontoIzbor izbor)
        {
            LstKonta.SelectedItem = izbor;
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

    private void CtxStampajKarticu_Click(object sender, RoutedEventArgs e)
    {
        BtnStampajKartice_Click(sender, e);
    }

    private void CtxExportExcelKartica_Click(object sender, RoutedEventArgs e)
    {
        BtnExportExcelKartica_Click(sender, e);
    }

    private void TxtPretragaKonta_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnOznaciOpseg_Click(object sender, RoutedEventArgs e)
    {
        if (LstKonta.ItemsSource is not List<KontoIzbor> izbori) return;

        string od = TxtOpsegOd.Text.Trim();
        string doKonta = TxtOpsegDo.Text.Trim();

        if (string.IsNullOrEmpty(od) && string.IsNullOrEmpty(doKonta))
        {
            MessageBox.Show("Unesite konto ili opseg konta (npr. celu klasu 5, ili opseg od-do).", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool celaKlasa = !string.IsNullOrEmpty(od) && string.IsNullOrEmpty(doKonta);

        foreach (var izbor in izbori)
        {
            bool uOpsegu = celaKlasa
                ? izbor.BrojKonta.StartsWith(od, StringComparison.OrdinalIgnoreCase)
                : string.Compare(izbor.BrojKonta, od, StringComparison.OrdinalIgnoreCase) >= 0
                  && string.Compare(izbor.BrojKonta, doKonta, StringComparison.OrdinalIgnoreCase) <= 0;

            izbor.IsSelected = uOpsegu;
        }
    }

    private async void LstKonta_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstKonta.SelectedItem is not KontoIzbor izbor)
        {
            return;
        }

        var konto = izbor.Konto;
        TxtNaslovKonta.Text = $"{konto.BrojKonta} — {konto.NazivKonta}";
        TxtPodnaslovKonta.Text = konto.IsSintetika ? "Sintetički konto" : "Analitički konto";

        await UcitajKarticu();
    }

    private async void Period_Changed(object sender, SelectionChangedEventArgs e)
    {
        await UcitajKarticu();
    }

    private void DgKarticaRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var red = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (red?.Item is KarticaRed kr)
        {
            DgKartica.SelectedItem = kr;
        }
    }

    private async void CtxPregledajNalog_Click(object sender, RoutedEventArgs e)
    {
        await OtvariNalogZaIzabranuStavku(samoPregled: true);
    }

    private async void CtxIzmeniNalog_Click(object sender, RoutedEventArgs e)
    {
        await OtvariNalogZaIzabranuStavku(samoPregled: false);
    }

    private async void DgKartica_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        await OtvariNalogZaIzabranuStavku(samoPregled: true);
    }

    private async Task OtvariNalogZaIzabranuStavku(bool samoPregled = true)
    {
        if (DgKartica.SelectedItem is not KarticaRed red)
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
            var nalog = await service.GetNalogByIdAsync(red.NalogId);
            if (nalog == null)
            {
                MessageBox.Show("Nalog nije pronađen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool rasknjizen = false;
            if (nalog.IsKnjizen && !samoPregled)
            {
                var odgovor = MessageBox.Show(
                    $"Nalog #{nalog.BrojNaloga} je proknjižen i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                    "Proknjižen nalog", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (odgovor != MessageBoxResult.Yes) return;

                if (!AppSession.IsAdministrator)
                {
                    MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await service.RasknjiziNalogAsync(nalog.NalogId);
                rasknjizen = true;
                nalog = await service.GetNalogByIdAsync(red.NalogId);
                if (nalog == null)
                {
                    await UcitajKarticu();
                    return;
                }
            }

            bool isReadOnly = nalog.IsKnjizen && samoPregled;
            var dijalog = new NalogEditWindow(nalog, isReadOnly, red.StavkaNalogaId) { Owner = Window.GetWindow(this) };
            if (dijalog.ShowDialog() == true || rasknjizen)
            {
                await UcitajKarticu();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task UcitajKarticu()
    {
        if (LstKonta.SelectedItem is not KontoIzbor izbor)
        {
            return;
        }

        var konto = izbor.Konto;
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);

            var kartica = await service.GetKarticaKontaAsync(konto.BrojKonta, DpKarticaOd.SelectedDate, DpKarticaDo.SelectedDate);
            DgKartica.ItemsSource = kartica;
            TxtSumaDuguje.Text = kartica.Sum(r => r.Duguje).ToString("N2");
            TxtSumaPotrazuje.Text = kartica.Sum(r => r.Potrazuje).ToString("N2");
            TxtSumaSaldo.Text = (kartica.Count > 0 ? kartica[^1].Saldo : 0m).ToString("N2");
            UpdateBtnStampajIzabraneState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kartice konta: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajKartice_Click(object sender, RoutedEventArgs e)
    {
        var izabrani = (LstKonta.ItemsSource as List<KontoIzbor> ?? new()).Where(k => k.IsSelected).ToList();
        if (!izabrani.Any())
        {
            if (LstKonta.SelectedItem is KontoIzbor trenutni)
            {
                izabrani.Add(trenutni);
            }
            else
            {
                MessageBox.Show("Izaberite konto za štampu kartice.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KarticaService(db);
            var odDatuma = DpKarticaOd.SelectedDate;
            var doDatuma = DpKarticaDo.SelectedDate;
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "ARHIBEL - 2026" };

            if (izabrani.Count == 1)
            {
                var konto = izabrani[0].Konto;
                var stavke = await service.GetKarticaKontaAsync(konto.BrojKonta, odDatuma, doDatuma);
                byte[] jedinaPdfBytes = PdfReportService.GenerisiKarticuPdf(firma, konto, stavke, odDatuma, doDatuma);

                string sigurnaSifra = string.Join("_", konto.BrojKonta.Split(Path.GetInvalidFileNameChars()));
                string jedinaPdfPath = Path.Combine(Path.GetTempPath(), $"KarticaKonta_{sigurnaSifra}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                await File.WriteAllBytesAsync(jedinaPdfPath, jedinaPdfBytes);

                Process.Start(new ProcessStartInfo { FileName = jedinaPdfPath, UseShellExecute = true });
                return;
            }

            var kartice = new List<(Konto Konto, List<KarticaRed> Stavke)>();
            foreach (var izbor in izabrani)
            {
                var stavke = await service.GetKarticaKontaAsync(izbor.Konto.BrojKonta, odDatuma, doDatuma);
                kartice.Add((izbor.Konto, stavke));
            }

            byte[] pdfBytes = PdfReportService.GenerisiViseKarticaPdf(firma, kartice, odDatuma, doDatuma);

            string pdfPath = Path.Combine(Path.GetTempPath(), $"KarticeKonta_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelKartica_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgKartica, TxtNaslovKonta.Text, "Kartica_Konta");
}

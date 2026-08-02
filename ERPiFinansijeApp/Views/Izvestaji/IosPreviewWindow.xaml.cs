using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;

namespace ERPiFinansijeApp.Views.Izvestaji;

public partial class IosPreviewWindow : Window
{
    private readonly List<IosPartnerGrupa> _sveGrupe;
    private readonly Firma _firma;
    private readonly string? _odKonta;
    private readonly string? _doKonta;
    private readonly DateTime? _odDatuma;
    private readonly DateTime? _doDatuma;

    public IosPreviewWindow(
        List<IosPartnerGrupa> grupe,
        Firma firma,
        string? odKonta = null,
        string? doKonta = null,
        DateTime? odDatuma = null,
        DateTime? doDatuma = null)
    {
        InitializeComponent();

        _sveGrupe = grupe ?? new List<IosPartnerGrupa>();
        _firma = firma;
        _odKonta = odKonta;
        _doKonta = doKonta;
        _odDatuma = odDatuma;
        _doDatuma = doDatuma;

        foreach (var g in _sveGrupe)
        {
            g.PropertyChanged -= PartnerGrupa_PropertyChanged;
            g.PropertyChanged += PartnerGrupa_PropertyChanged;
        }

        string podnaslovKonto = string.IsNullOrWhiteSpace(odKonta) && string.IsNullOrWhiteSpace(doKonta)
            ? "Sva konta"
            : $"Konto: {odKonta ?? "---"} do {doKonta ?? "---"}";

        string podnaslovDatum = odDatuma.HasValue || doDatuma.HasValue
            ? $" | Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}"
            : "";

        TxtPodnaslov.Text = $"{podnaslovKonto}{podnaslovDatum}";

        ApplyFilter();

        if (LstPartneri.Items.Count > 0)
        {
            LstPartneri.SelectedIndex = 0;
        }
    }

    private void PartnerGrupa_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IosPartnerGrupa.IsSelected))
        {
            UpdateBtnStampajIzabraneState();
        }
    }

    private void ApplyFilter()
    {
        if (_sveGrupe == null || LstPartneri == null || TxtPretraga == null || ChkSamoSaSaldom == null)
            return;

        string term = TxtPretraga.Text.Trim().ToLower();
        bool samoSaSaldom = ChkSamoSaSaldom.IsChecked == true;

        var filtrirano = _sveGrupe.Where(g =>
        {
            if (samoSaSaldom && g.Saldo == 0m)
                return false;

            if (string.IsNullOrWhiteSpace(term))
                return true;

            return g.NazivPartnera.ToLower().Contains(term) ||
                   g.SifraPartnera.ToLower().Contains(term) ||
                   g.Konto.ToLower().Contains(term);
        }).ToList();

        PrikaziListuPartnera(filtrirano);
    }

    private void PrikaziListuPartnera(List<IosPartnerGrupa> grupe)
    {
        if (LstPartneri == null || TxtBrojPartnera == null || TxtUkupanSaldoSvi == null)
            return;

        LstPartneri.ItemsSource = grupe;

        decimal ukupanSaldo = grupe.Sum(g => g.Saldo);
        TxtBrojPartnera.Text = $"Partnera: {grupe.Count}";
        TxtUkupanSaldoSvi.Text = $"Ukupno: {ukupanSaldo:N2} RSD";

        UpdateBtnStampajIzabraneState();
    }

    private void UpdateBtnStampajIzabraneState()
    {
        if (_sveGrupe == null || TxtBrojIzabranih == null || BtnStampajIzabraneIos == null)
            return;

        int count = _sveGrupe.Count(g => g.IsSelected);
        TxtBrojIzabranih.Text = $"Izabrano: {count}";
        BtnStampajIzabraneIos.IsEnabled = count > 0;
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri == null || ChkSelectAll == null)
            return;

        if (LstPartneri.ItemsSource is List<IosPartnerGrupa> vidljivi)
        {
            bool check = ChkSelectAll.IsChecked == true;
            foreach (var g in vidljivi)
            {
                g.IsSelected = check;
            }
            UpdateBtnStampajIzabraneState();
        }
    }

    private void LstPartneri_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not IosPartnerGrupa grupa)
        {
            OcistiPrikazPartnera();
            return;
        }

        if (BtnStampajPrikazanuIos != null)
        {
            BtnStampajPrikazanuIos.IsEnabled = true;
        }

        TxtSelectedPartnerNaziv.Text = $"{grupa.SifraPartnera} — {grupa.NazivPartnera}";
        TxtSelectedPartnerInfo.Text = $"Konto: {grupa.Konto}" +
            (string.IsNullOrWhiteSpace(grupa.Pib) ? "" : $" | PIB: {grupa.Pib}") +
            (string.IsNullOrWhiteSpace(grupa.Adresa) ? "" : $" | {grupa.Adresa}, {grupa.PttIMesto}");

        TxtPartnerSaldo.Text = $"{grupa.Saldo:N2} RSD";

        DgOtvoreneStavke.ItemsSource = grupa.Stavke;

        TxtUkupnoDuguje.Text = grupa.UkupnoDuguje.ToString("N2");
        TxtUkupnoPotrazuje.Text = grupa.UkupnoPotrazuje.ToString("N2");
        TxtSaldoSum.Text = grupa.Saldo.ToString("N2");
    }

    private void OcistiPrikazPartnera()
    {
        if (BtnStampajPrikazanuIos != null)
        {
            BtnStampajPrikazanuIos.IsEnabled = false;
        }

        TxtSelectedPartnerNaziv.Text = "Nema izabranog partnera";
        TxtSelectedPartnerInfo.Text = "";
        TxtPartnerSaldo.Text = "0,00 RSD";
        DgOtvoreneStavke.ItemsSource = null;
        TxtUkupnoDuguje.Text = "0,00";
        TxtUkupnoPotrazuje.Text = "0,00";
        TxtSaldoSum.Text = "0,00";
    }

    private async void BtnStampajPrikazanuIos_Click(object sender, RoutedEventArgs e)
    {
        if (LstPartneri.SelectedItem is not IosPartnerGrupa grupa)
        {
            MessageBox.Show("Izaberite partnera sa liste za štampu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            byte[] pdfBytes = PdfReportService.GenerisiZbirniIOSPdf(_firma, new List<IosPartnerGrupa> { grupa }, _odKonta, _doKonta, _odDatuma, _doDatuma);
            string pdfPath = Path.Combine(Path.GetTempPath(), $"IOS_{grupa.SifraPartnera}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a za prikazanog partnera: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajIzabraneIos_Click(object sender, RoutedEventArgs e)
    {
        var izabrane = _sveGrupe.Where(g => g.IsSelected).ToList();
        if (izabrane.Count == 0)
        {
            MessageBox.Show("Nije izabran nijedan partner za štampu. Označite štrikliranjem partnere u listi sa leve strane.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            byte[] pdfBytes = PdfReportService.GenerisiZbirniIOSPdf(_firma, izabrane, _odKonta, _doKonta, _odDatuma, _doDatuma);
            string pdfPath = Path.Combine(Path.GetTempPath(), $"IOS_Izabrani_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a za izabrane partnere: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnExportPdfIos_Click(object sender, RoutedEventArgs e)
    {
        var vidljivi = LstPartneri.ItemsSource as List<IosPartnerGrupa> ?? _sveGrupe;
        try
        {
            byte[] pdfBytes = PdfReportService.GenerisiZbirniIOSPdf(_firma, vidljivi, _odKonta, _doKonta, _odDatuma, _doDatuma);
            string pdfPath = Path.Combine(Path.GetTempPath(), $"IOS_Zbirni_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelIos_Click(object sender, RoutedEventArgs e)
    {
        if (DgOtvoreneStavke.ItemsSource != null)
        {
            ExcelExportService.ExportDataGridToExcel(DgOtvoreneStavke, TxtSelectedPartnerNaziv.Text, "IOS_Otvorene_Stavke");
        }
        else
        {
            MessageBox.Show("Nema podataka za izvoz u Excel.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

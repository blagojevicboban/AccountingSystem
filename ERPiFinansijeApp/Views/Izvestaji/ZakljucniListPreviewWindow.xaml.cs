using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData.Services;

namespace ERPiFinansijeApp.Views.Izvestaji;

public partial class ZakljucniListPreviewWindow : Window
{
    public ZakljucniListPreviewWindow(string naslov, List<ZakljucniListRed> redovi, DateTime? odDatuma, DateTime? doDatuma)
    {
        InitializeComponent();

        TxtNaslov.Text = naslov;
        TxtPodnaslov.Text = odDatuma.HasValue || doDatuma.HasValue
            ? $"Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}"
            : "";

        DgZakljucniList.ItemsSource = redovi;

        var detalji = redovi.Where(r => r.Tip == BrutoBilansRedTip.Detalj).ToList();
        TxtPocDug.Text = detalji.Sum(r => r.PocetnoDuguje).ToString("N2");
        TxtPocPot.Text = detalji.Sum(r => r.PocetnoPotrazuje).ToString("N2");

        TxtPromDug.Text = detalji.Sum(r => r.PrometDuguje).ToString("N2");
        TxtPromPot.Text = detalji.Sum(r => r.PrometPotrazuje).ToString("N2");

        TxtUkDug.Text = detalji.Sum(r => r.UkupnoDuguje).ToString("N2");
        TxtUkPot.Text = detalji.Sum(r => r.UkupnoPotrazuje).ToString("N2");

        TxtSalDug.Text = detalji.Sum(r => r.SaldoDuguje).ToString("N2");
        TxtSalPot.Text = detalji.Sum(r => r.SaldoPotrazuje).ToString("N2");
    }

    private void BtnExportExcelZakljucniList_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(
            DgZakljucniList,
            TxtNaslov.Text,
            "Zakljucni_List",
            jeStavkaZaZbir: item => item is ZakljucniListRed red && red.Tip == BrutoBilansRedTip.Detalj,
            rowStyler: item => item is ZakljucniListRed red
                ? red.Tip switch
                {
                    BrutoBilansRedTip.KlasaTotal => ("#E2E8F0", true),
                    _                             => (null, false)
                }
                : (null, false));
}

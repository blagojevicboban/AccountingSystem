using System.Windows;
using AccountingData.Services;

namespace AccountingApp.Views.Izvestaji;

public partial class BrutoBilansPreviewWindow : Window
{
    public BrutoBilansPreviewWindow(string naslov, List<BrutoBilansRed> redovi, DateTime? odDatuma, DateTime? doDatuma)
    {
        InitializeComponent();

        TxtNaslov.Text = naslov;
        TxtPodnaslov.Text = odDatuma.HasValue || doDatuma.HasValue
            ? $"Period: {odDatuma?.ToString("dd.MM.yyyy") ?? "---"} - {doDatuma?.ToString("dd.MM.yyyy") ?? "---"}"
            : "";

        DgBrutoBilans.ItemsSource = redovi;

        var detalji = redovi.Where(r => r.Tip == BrutoBilansRedTip.Detalj).ToList();
        TxtUkupnoDuguje.Text = detalji.Sum(r => r.Duguje).ToString("N2");
        TxtUkupnoPotrazuje.Text = detalji.Sum(r => r.Potrazuje).ToString("N2");
        TxtUkupnoSaldoDuguje.Text = detalji.Sum(r => r.SaldoDuguje).ToString("N2");
        TxtUkupnoSaldoPotrazuje.Text = detalji.Sum(r => r.SaldoPotrazuje).ToString("N2");
    }
}

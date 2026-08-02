using System.Windows;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeApp.Views.Izvestaji;

public partial class DnevnikPreviewWindow : Window
{
    public DnevnikPreviewWindow(List<Nalog> nalozi)
    {
        InitializeComponent();

        TxtPodnaslov.Text = $"Broj proknjiženih naloga: {nalozi.Count}";

        var redovi = nalozi
            .SelectMany(n => n.Stavke.Select(st => new DnevnikRed
            {
                BrojNaloga = n.BrojNaloga,
                Datum = n.DatumNaloga,
                DokumentOpis = !string.IsNullOrWhiteSpace(st.BrojDokumenta) ? st.BrojDokumenta! : (st.Opis ?? n.Opis ?? ""),
                BrojKonta = st.BrojKonta,
                Duguje = st.Duguje,
                Potrazuje = st.Potrazuje
            }))
            .ToList();

        DgDnevnik.ItemsSource = redovi;

        TxtUkupnoDuguje.Text = redovi.Sum(r => r.Duguje).ToString("N2");
        TxtUkupnoPotrazuje.Text = redovi.Sum(r => r.Potrazuje).ToString("N2");
    }

    private void BtnExportExcelDnevnik_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgDnevnik, TxtNaslov.Text, "Dnevnik_Glavne_Knjige");
}

public class DnevnikRed
{
    public int BrojNaloga { get; set; }
    public DateTime Datum { get; set; }
    public string DokumentOpis { get; set; } = "";
    public string BrojKonta { get; set; } = "";
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
}

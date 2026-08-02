using System.Windows;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData.Services;

namespace ERPiFinansijeApp.Views.Izvestaji;

public partial class VrednovanjeZalihaPreviewWindow : Window
{
    public VrednovanjeZalihaPreviewWindow(List<RobniBrutoBilansRed> stavke)
    {
        InitializeComponent();

        var redovi = stavke
            .Where(s => s.SaldoKolicinski != 0)
            .OrderBy(s => s.SifraMagacina).ThenBy(s => s.SifraArtikla)
            .Select(s => new VrednovanjeZalihaRed
            {
                NazivMagacina = $"{s.SifraMagacina} — {s.NazivMagacina}",
                SifraArtikla = s.SifraArtikla,
                NazivArtikla = s.NazivArtikla,
                JedinicaMere = s.JedinicaMere,
                Kolicina = s.SaldoKolicinski,
                JedinicnaCena = s.SaldoKolicinski != 0 ? s.SaldoVrednosni / s.SaldoKolicinski : 0m,
                Vrednost = s.SaldoVrednosni
            })
            .ToList();

        TxtPodnaslov.Text = $"Na dan: {DateTime.Now:dd.MM.yyyy} | Stavki: {redovi.Count}";

        DgZalihe.ItemsSource = redovi;
        TxtUkupnaVrednost.Text = redovi.Sum(r => r.Vrednost).ToString("N2");
    }

    private void BtnExportExcelZalihe_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgZalihe, TxtNaslov.Text, "Vrednovanje_Zaliha");
}

public class VrednovanjeZalihaRed
{
    public string NazivMagacina { get; set; } = "";
    public string SifraArtikla { get; set; } = "";
    public string NazivArtikla { get; set; } = "";
    public string JedinicaMere { get; set; } = "";
    public decimal Kolicina { get; set; }
    public decimal JedinicnaCena { get; set; }
    public decimal Vrednost { get; set; }
}

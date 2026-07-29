using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Pdv;

public partial class PdvEvidencijaView : UserControl
{
    private List<PdvZapis> _kirZapisi = new();
    private List<PdvZapis> _kprZapisi = new();
    private PdvObracunResult _pdvObracun = new();

    public PdvEvidencijaView()
    {
        InitializeComponent();
        DpOdDatuma.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DpDoDatuma.SelectedDate = DateTime.Today;
        Loaded += PdvEvidencijaView_Loaded;
    }

    private void PdvEvidencijaView_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajPdvEvidenciju();
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajPdvEvidenciju();
    }

    private async void UcitajPdvEvidenciju()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new PdvService(db);

            DateTime? odDatuma = DpOdDatuma.SelectedDate;
            DateTime? doDatuma = DpDoDatuma.SelectedDate;

            _kirZapisi = await service.GetKirZapisiAsync(odDatuma, doDatuma);
            _kprZapisi = await service.GetKprZapisiAsync(odDatuma, doDatuma);
            _pdvObracun = await service.GetPdvObracunAsync(odDatuma, doDatuma);

            DgKir.ItemsSource = _kirZapisi;
            DgKpr.ItemsSource = _kprZapisi;

            TxtKirUkupno.Text = $"Ukupno KIR: {_kirZapisi.Sum(x => x.UkupnaNaknadaSaPdv):N2} RSD | Izlazni PDV: {_pdvObracun.KirUkupanPdv:N2} RSD";
            TxtKprUkupno.Text = $"Ukupno KPR: {_kprZapisi.Sum(x => x.UkupnaNaknadaSaPdv):N2} RSD | Prethodni PDV: {_pdvObracun.KprUkupanPdv:N2} RSD";

            TxtObracunKirPdv.Text = $"{_pdvObracun.KirUkupanPdv:N2} RSD";
            TxtObracunKprPdv.Text = $"{_pdvObracun.KprUkupanPdv:N2} RSD";

            decimal razlika = _pdvObracun.PdvRazlika;
            var bc = new System.Windows.Media.BrushConverter();

            if (razlika > 0)
            {
                TxtObracunKonačni.Text = $"{razlika:N2} RSD (OBAVEZA ZA UPLATU)";
                TxtObracunKonačni.Foreground = System.Windows.Media.Brushes.DarkRed;
                TxtStatusPdvPoruka.Text = $"⚠️ Za izabrani period postoji obaveza za uplatu PDV-a u iznosu od {razlika:N2} RSD.";
                PnlStatusPdv.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#FEE2E2")!;
                PnlStatusPdv.BorderBrush = (System.Windows.Media.Brush)bc.ConvertFrom("#FCA5A5")!;
            }
            else if (razlika < 0)
            {
                decimal povracaj = Math.Abs(razlika);
                TxtObracunKonačni.Text = $"{povracaj:N2} RSD (PRAVO NA POVRAĆAJ / PREPLATU)";
                TxtObracunKonačni.Foreground = System.Windows.Media.Brushes.DarkGreen;
                TxtStatusPdvPoruka.Text = $"✅ Za izabrani period postoji preplata / pravo na povraćaj PDV-a u iznosu od {povracaj:N2} RSD.";
                PnlStatusPdv.Background = (System.Windows.Media.Brush)bc.ConvertFrom("#DCFCE7")!;
                PnlStatusPdv.BorderBrush = (System.Windows.Media.Brush)bc.ConvertFrom("#86EFAC")!;
            }
            else
            {
                TxtObracunKonačni.Text = "0.00 RSD";
                TxtObracunKonačni.Foreground = System.Windows.Media.Brushes.Blue;
                TxtStatusPdvPoruka.Text = "⚖️ Obaveza za PDV i prethodni PDV su izjednačeni (0.00 RSD).";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju PDV evidencije: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajKirPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Moja Firma D.O.O." };

            byte[] pdf = PdfReportService.GenerisiKirPdf(firma, _kirZapisi, DpOdDatuma.SelectedDate, DpDoDatuma.SelectedDate);

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stampe");
            Directory.CreateDirectory(folder);
            string putanja = Path.Combine(folder, $"KIR_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            await File.WriteAllBytesAsync(putanja, pdf);
            Process.Start(new ProcessStartInfo(putanja) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF KIR-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajKprPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Moja Firma D.O.O." };

            byte[] pdf = PdfReportService.GenerisiKprPdf(firma, _kprZapisi, DpOdDatuma.SelectedDate, DpDoDatuma.SelectedDate);

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stampe");
            Directory.CreateDirectory(folder);
            string putanja = Path.Combine(folder, $"KPR_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            await File.WriteAllBytesAsync(putanja, pdf);
            Process.Start(new ProcessStartInfo(putanja) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF KPR-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportExcelKir_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgKir, "KIR - Knjiga izdatih računa", "KIR_Knjiga_Izdatih_Racuna");

    private void BtnExportExcelKpr_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgKpr, "KPR - Knjiga primljenih računa", "KPR_Knjiga_Primljenih_Racuna");
}

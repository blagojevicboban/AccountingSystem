using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Bilansi;

public partial class BilansiView : UserControl
{
    private List<BilansPozicija> _bilansStanjaPozicije = new();
    private List<BilansPozicija> _bilansUspehaPozicije = new();

    public BilansiView()
    {
        InitializeComponent();
        DpDatumStanja.SelectedDate = DateTime.Today;
        DpOdDatuma.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
        DpDoDatuma.SelectedDate = DateTime.Today;
        Loaded += BilansiView_Loaded;
    }

    private void BilansiView_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajBilanse();
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajBilanse();
    }

    private async void UcitajBilanse()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new BilansService(db);

            // 1. Bilans Stanja
            DateTime doDatumaStanja = DpDatumStanja.SelectedDate ?? DateTime.Today;
            _bilansStanjaPozicije = await service.GetBilansStanjaAsync(doDatumaStanja);
            DgBilansStanja.ItemsSource = _bilansStanjaPozicije;

            var ukAktiva = _bilansStanjaPozicije.FirstOrDefault(p => p.AopCode == "0010")?.IznosTekucaGodina ?? 0m;
            var ukPasiva = _bilansStanjaPozicije.FirstOrDefault(p => p.AopCode == "0410")?.IznosTekucaGodina ?? 0m;

            if (ukAktiva == ukPasiva)
            {
                TxtStatusRavnoteze.Text = $"✅ Bilans Stanja je U RAVNOTEŽI! (Aktiva = Pasiva = {ukAktiva:N2} RSD)";
                TxtStatusRavnoteze.Foreground = System.Windows.Media.Brushes.DarkGreen;
            }
            else
            {
                decimal razlika = ukAktiva - ukPasiva;
                TxtStatusRavnoteze.Text = $"⚠️ Postoji razlika u Bilansu Stanja: Aktiva ({ukAktiva:N2}) - Pasiva ({ukPasiva:N2}) = Razlika {razlika:N2} RSD";
                TxtStatusRavnoteze.Foreground = System.Windows.Media.Brushes.Red;
            }

            // 2. Bilans Uspeha
            DateTime? odDatuma = DpOdDatuma.SelectedDate;
            DateTime? doDatuma = DpDoDatuma.SelectedDate;
            _bilansUspehaPozicije = await service.GetBilansUspehaAsync(odDatuma, doDatuma);
            DgBilansUspeha.ItemsSource = _bilansUspehaPozicije;

            var netoDobitak = _bilansUspehaPozicije.FirstOrDefault(p => p.AopCode == "1030")?.IznosTekucaGodina ?? 0m;
            var netoGubitak = _bilansUspehaPozicije.FirstOrDefault(p => p.AopCode == "1031")?.IznosTekucaGodina ?? 0m;

            if (netoDobitak > 0)
            {
                TxtNetoRezultat.Text = $"🎉 OSTVAREN JE NETO DOBITAK PERIODA: {netoDobitak:N2} RSD";
                TxtNetoRezultat.Foreground = System.Windows.Media.Brushes.DarkGreen;
            }
            else if (netoGubitak > 0)
            {
                TxtNetoRezultat.Text = $"🔻 OSTVAREN JE NETO GUBITAK PERIODA: {netoGubitak:N2} RSD";
                TxtNetoRezultat.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                TxtNetoRezultat.Text = "⚖️ Rezultat poslovanja je 0.00 RSD";
                TxtNetoRezultat.Foreground = System.Windows.Media.Brushes.Blue;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri obračunu bilansa: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajStanjePdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Moja Firma D.O.O." };

            DateTime datum = DpDatumStanja.SelectedDate ?? DateTime.Today;
            byte[] pdf = PdfReportService.GenerisiBilansStanjaPdf(firma, _bilansStanjaPozicije, datum);

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stampe");
            Directory.CreateDirectory(folder);
            string putanja = Path.Combine(folder, $"BilansStanja_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            await File.WriteAllBytesAsync(putanja, pdf);
            Process.Start(new ProcessStartInfo(putanja) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF bilansa stanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnStampajUspehPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Moja Firma D.O.O." };

            byte[] pdf = PdfReportService.GenerisiBilansUspehaPdf(firma, _bilansUspehaPozicije, DpOdDatuma.SelectedDate, DpDoDatuma.SelectedDate);

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stampe");
            Directory.CreateDirectory(folder);
            string putanja = Path.Combine(folder, $"BilansUspeha_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            await File.WriteAllBytesAsync(putanja, pdf);
            Process.Start(new ProcessStartInfo(putanja) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF bilansa uspeha: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

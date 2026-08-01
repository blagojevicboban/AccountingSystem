using System.Windows;
using System.Windows.Input;
using AccountingApp.Views.Pomoc;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Magacin;

public partial class ProveraKarticaWindow : Window
{
    private readonly List<MaterijalnaKartica> _redovi;

    public ProveraKarticaWindow(List<MaterijalnaKartica> redovi)
    {
        InitializeComponent();
        _redovi = redovi;
        DgProvera.ItemsSource = _redovi;
    }

    private async void BtnStampaj_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>().UseSqlite($"Data Source={AppConfig.DbPath}").Options;
            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Preduzeće" };

            var pdfBytes = Services.PdfReportService.GenerisiProveruMaterijalnihKarticaPdf(firma, _redovi);

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Provera_Materijalnih_Kartica_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await System.IO.File.WriteAllBytesAsync(tempFile, pdfBytes);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempFile,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri štampi: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "⚠️ Pomoć — Provera materijalnih kartica",
            "Alat za proveru integriteta podataka materijalnog knjigovodstva.",
            new (string, string)[]
            {
                ("Esc", "Zatvara prozor."),
                ("🖨️", "Izvozi prikazanu listu u PDF."),
            },
            "Lista prikazuje stavke gde je izračunato stanje ili cena na materijalnoj kartici negativno — što obično ukazuje na grešku u redosledu unosa dokumenata (npr. trebovanje pre odgovarajućeg ulaza)."
        ) { Owner = this }.ShowDialog();
    }
}

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace AccountingApp.Views.Podesavanja;

public partial class PodesavanjaView : UserControl
{
    public PodesavanjaView()
    {
        InitializeComponent();
        UcitajPodesavanja();
    }

    private async void UcitajPodesavanja()
    {
        var settings = UserSettings.Instance;

        TxtDbPath.Text = AppConfig.DbPath;
        ChkStartMaximized.IsChecked = settings.StartMaximized;

        TxtNazivServisa.Text = settings.NazivServisa ?? "";
        TxtOvlascenoLice.Text = settings.OvlascenoLice ?? "";

        ChkPotvrdaZaRasknjizavanje.IsChecked = settings.PotvrdaZaRasknjizavanje;
        ChkPotvrdaZaBrisanje.IsChecked = settings.PotvrdaZaBrisanje;

        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        TxtAppVersion.Text = $"Aplikacija: AccountingSystem v{ver?.Major}.{ver?.Minor}.{ver?.Build} (.NET 8.0 WPF)";

        // Učitavanje SEF podešavanja iz baze
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync();
            if (firma != null)
            {
                TxtSefApiKey.Text = firma.SefApiKey ?? "";
                TxtJbkjsBroj.Text = firma.JbkjsBroj ?? "";
                TxtFirmaEmail.Text = firma.Email ?? "";
                CmbSefEnvironment.SelectedIndex = (firma.SefEnvironment ?? "Demo").Equals("Production", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

                TxtPfrUrl.Text = firma.PfrUrl ?? "http://localhost:8443";
                TxtPfrPacKod.Text = firma.PfrPacKod ?? "123456";
                TxtPfrKasirName.Text = firma.PfrKasirName ?? "Glavni Kasir";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri učitavanju SEF/PFR podešavanja: {ex.Message}");
        }

        OsveziStatusWebServera();
    }

    private void OsveziStatusWebServera()
    {
        if (AccountingWebServer.IsRunning)
        {
            TxtWebServerStatus.Text = $"🟢 Server je aktivan na http://localhost:{AccountingWebServer.Port}";
            TxtWebServerStatus.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            TxtWebServerStatus.Text = "🔴 Server je zaustavljen";
            TxtWebServerStatus.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void BtnPokreniWebServer_Click(object sender, RoutedEventArgs e)
    {
        int port = 5050;
        int.TryParse(TxtApiPort.Text.Trim(), out port);
        if (port <= 0) port = 5050;

        AccountingWebServer.Start(AppConfig.DbPath, port);
        OsveziStatusWebServera();
        MessageBox.Show($"🌐 Web Server & Cloud REST API je uspešno pokrenut na portu {port}!\n\nWeb Dashboard je dostupan na:\nhttp://localhost:{port}", "Web Server Pokrenut", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnZaustaviWebServer_Click(object sender, RoutedEventArgs e)
    {
        AccountingWebServer.Stop();
        OsveziStatusWebServera();
        MessageBox.Show("⏹️ Web Server je zaustavljen.", "Web Server Zaustavljen", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnOtvoriWebDashboard_Click(object sender, RoutedEventArgs e)
    {
        if (!AccountingWebServer.IsRunning)
        {
            BtnPokreniWebServer_Click(sender, e);
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"http://localhost:{AccountingWebServer.Port}") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju pretraživača: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPromeniBazu_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Izaberite SQLite bazu podataka",
            Filter = "SQLite Baza (*.db)|*.db|Svi fajlovi (*.*)|*.*",
            FileName = AppConfig.DbPath
        };

        if (openDialog.ShowDialog() == true)
        {
            UserSettings.Instance.ActiveDbPath = openDialog.FileName;
            UserSettings.Instance.Save();
            TxtDbPath.Text = openDialog.FileName;

            MessageBox.Show("Putanja baze podataka je promenjena!\n\nPromene će u potpunosti stupiti na snagu pri sledećem pokretanju aplikacije.",
                "Putanja baze sačuvana", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = UserSettings.Instance;

            settings.StartMaximized = ChkStartMaximized.IsChecked ?? true;

            settings.NazivServisa = string.IsNullOrWhiteSpace(TxtNazivServisa.Text) ? null : TxtNazivServisa.Text.Trim();
            settings.OvlascenoLice = string.IsNullOrWhiteSpace(TxtOvlascenoLice.Text) ? null : TxtOvlascenoLice.Text.Trim();

            settings.PotvrdaZaRasknjizavanje = ChkPotvrdaZaRasknjizavanje.IsChecked ?? true;
            settings.PotvrdaZaBrisanje = ChkPotvrdaZaBrisanje.IsChecked ?? true;

            settings.Save();

            // Čuvanje SEF i PFR podešavanja u bazi
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync();
            if (firma != null)
            {
                firma.SefApiKey = string.IsNullOrWhiteSpace(TxtSefApiKey.Text) ? null : TxtSefApiKey.Text.Trim();
                firma.JbkjsBroj = string.IsNullOrWhiteSpace(TxtJbkjsBroj.Text) ? null : TxtJbkjsBroj.Text.Trim();
                firma.Email = string.IsNullOrWhiteSpace(TxtFirmaEmail.Text) ? null : TxtFirmaEmail.Text.Trim();
                firma.SefEnvironment = CmbSefEnvironment.SelectedIndex == 1 ? "Production" : "Demo";

                firma.PfrUrl = string.IsNullOrWhiteSpace(TxtPfrUrl.Text) ? "http://localhost:8443" : TxtPfrUrl.Text.Trim();
                firma.PfrPacKod = string.IsNullOrWhiteSpace(TxtPfrPacKod.Text) ? "123456" : TxtPfrPacKod.Text.Trim();
                firma.PfrKasirName = string.IsNullOrWhiteSpace(TxtPfrKasirName.Text) ? "Glavni Kasir" : TxtPfrKasirName.Text.Trim();

                await db.SaveChangesAsync();
            }

            MessageBox.Show("Podešavanja su uspešno sačuvana!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju podešavanja:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnTestirajSef_Click(object sender, RoutedEventArgs e)
    {
        string apiKey = TxtSefApiKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            MessageBox.Show("Molimo unesite SEF API ključ pre testiranja konekcije.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string env = CmbSefEnvironment.SelectedIndex == 1 ? "Production" : "Demo";
        var client = new SefApiClient(apiKey, env);

        var res = await client.TestConnectionAsync();
        if (res.Success)
        {
            MessageBox.Show($"✅ {res.Message}", "SEF Konekcija Uspešna", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"❌ {res.Message}", "Greška Konekcije", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnTestirajPfr_Click(object sender, RoutedEventArgs e)
    {
        var pfrClient = new PfrApiClient();
        var postavke = new PfrPostavke
        {
            PfrUrl = TxtPfrUrl.Text.Trim(),
            PacKod = TxtPfrPacKod.Text.Trim(),
            Kasir = TxtPfrKasirName.Text.Trim()
        };

        var res = await pfrClient.TestirajPfrKonekcijuAsync(postavke);
        if (res.Success)
        {
            MessageBox.Show($"✅ {res.Message}", "PFR Konekcija Uspešna", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"❌ {res.Message}", "Greška PFR Konekcije", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnUvozDOS_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new Views.Pomoc.DosImportWindow
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pokretanju uvoza iz DOS sistema:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

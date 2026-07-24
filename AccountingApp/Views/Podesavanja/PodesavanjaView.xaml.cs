using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AccountingApp.Views.Podesavanja;

public partial class PodesavanjaView : UserControl
{
    public PodesavanjaView()
    {
        InitializeComponent();
        UcitajPodesavanja();
    }

    private void UcitajPodesavanja()
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

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
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

            MessageBox.Show("Podešavanja su uspešno sačuvana!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju podešavanja:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

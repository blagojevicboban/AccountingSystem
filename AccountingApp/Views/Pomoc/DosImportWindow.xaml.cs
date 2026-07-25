using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AccountingApp.Services;
using AccountingData;

namespace AccountingApp.Views.Pomoc;

public partial class DosImportWindow : Window
{
    private readonly AccountingDbContext _db;
    private List<DbfFirmaDto> _pronadjeneFirme = new();

    public DosImportWindow(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;

        // Podrazumevana radna putanja
        string defaultPath = @"C:\KNJIGE\Radni";
        if (!Directory.Exists(defaultPath))
        {
            defaultPath = AppDomain.CurrentDomain.BaseDirectory;
        }

        TxtFolderPath.Text = defaultPath;
        SkenirajFolder(defaultPath);
    }

    private void SkenirajFolder(string folderPath)
    {
        try
        {
            _pronadjeneFirme = DosImportService.Instance.SkenirajRadniDirektorijum(folderPath);
            DgFirme.ItemsSource = _pronadjeneFirme;
            TxtFirmCount.Text = $"Pronađeno: {_pronadjeneFirme.Count} firmi";
            AppendLog($"Skeniran folder '{folderPath}'. Pronađeno {_pronadjeneFirme.Count} firmi.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri skeniranju radnog foldera:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Izaberite radni direktorijum sa DOS/DBF podacima",
            InitialDirectory = TxtFolderPath.Text
        };

        if (dialog.ShowDialog() == true)
        {
            TxtFolderPath.Text = dialog.FolderName;
            SkenirajFolder(dialog.FolderName);
        }
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var f in _pronadjeneFirme) f.IsSelected = true;
    }

    private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var f in _pronadjeneFirme) f.IsSelected = false;
    }

    private async void BtnStartImport_Click(object sender, RoutedEventArgs e)
    {
        var izabrane = _pronadjeneFirme.Where(f => f.IsSelected).ToList();
        if (!izabrane.Any())
        {
            MessageBox.Show("Molimo štiklirajte bar jednu firmu za uvoz.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnStartImport.IsEnabled = false;
        TxtLog.Text = "";
        AppendLog($"Započet masovni uvoz za {izabrane.Count} izabranih firmi...");

        var progressHandler = new Progress<DosImportProgress>(p =>
        {
            PbProgress.Value = p.Percentage;
            TxtPercentage.Text = $"{p.Percentage}%";
            TxtStatus.Text = $"{p.FirmName} - {p.StepDescription}";
            if (!string.IsNullOrEmpty(p.LogMessage))
            {
                AppendLog(p.LogMessage);
            }
        });

        try
        {
            await DosImportService.Instance.UveziFirmeAsync(izabrane, _db, progressHandler);
            MessageBox.Show($"Uvoz je uspešno završen za {izabrane.Count} firmi!\n\nPodaci o kontima, partnerima, nalozima i artiklima su zavedeni u bazu.",
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var errDetail = ex.InnerException != null ? $"{ex.Message}\n\nDetalji: {ex.InnerException.Message}" : ex.Message;
            MessageBox.Show($"Došlo je do greške pri uvozu podataka:\n{errDetail}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            AppendLog($"❌ GREŠKA: {errDetail}");
        }
        finally
        {
            BtnStartImport.IsEnabled = true;
            TxtStatus.Text = "Završeno";
        }
    }

    private void AppendLog(string message)
    {
        TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        TxtLog.ScrollToEnd();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

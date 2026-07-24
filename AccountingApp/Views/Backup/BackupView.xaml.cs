using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Services;
using Microsoft.Win32;

namespace AccountingApp.Views.Backup;

public partial class BackupView : UserControl
{
    private bool _isInitializing = true;

    public BackupView()
    {
        InitializeComponent();
        UcitajPodesavanja();
        UcitajIstoriju();
        _isInitializing = false;
    }

    private void UcitajPodesavanja()
    {
        var freq = UserSettings.Instance.AutoBackupFrequency;
        if (freq == 0) RbNikad.IsChecked = true;
        else if (freq == 2) RbDnevno.IsChecked = true;
        else RbPriIzlasku.IsChecked = true;

        TxtBackupFolder.Text = $"Folder: {BackupService.Instance.BackupDir}";
    }

    private void UcitajIstoriju()
    {
        try
        {
            var lista = BackupService.Instance.UcitajIstorijuKopija();
            DgBackupHistory.ItemsSource = lista;
            TxtUkupnoKopija.Text = $"Ukupno: {lista.Count} kopija";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju istorije kopija:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AutoBackup_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        int freq = 1;
        if (RbNikad.IsChecked == true) freq = 0;
        else if (RbDnevno.IsChecked == true) freq = 2;

        UserSettings.Instance.AutoBackupFrequency = freq;
        UserSettings.Instance.Save();
    }

    private void BtnOsvezi_Click(object sender, RoutedEventArgs e)
    {
        UcitajIstoriju();
    }

    private void BtnRucniBackup_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = "Sačuvaj sigurnosnu kopiju baze podataka",
            Filter = "SQLite Baza (*.db)|*.db|Svi fajlovi (*.*)|*.*",
            FileName = $"AccountingDb_rucni_{DateTime.Now:yyyyMMdd_HHmmss}.db"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                BackupService.Instance.NapraviRucniBackup(saveDialog.FileName);
                MessageBox.Show($"Sigurnosna kopija je uspešno sačuvana na:\n\n{saveDialog.FileName}",
                    "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                UcitajIstoriju();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri kreiranju rezervne kopije:\n{ex.Message}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnVratiIzFajla_Click(object sender, RoutedEventArgs e)
    {
        var openDialog = new OpenFileDialog
        {
            Title = "Izaberite rezervnu kopiju za restauraciju baze",
            Filter = "SQLite Baza (*.db)|*.db|Svi fajlovi (*.*)|*.*"
        };

        if (openDialog.ShowDialog() == true)
        {
            IzvrsiRestauraciju(openDialog.FileName);
        }
    }

    private void BtnVratiKopiju_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is BackupItem item)
        {
            IzvrsiRestauraciju(item.Putanja);
        }
    }

    private void IzvrsiRestauraciju(string sourcePath)
    {
        var rez = MessageBox.Show(
            $"UPOZORENJE!\n\nVraćanje rezervne kopije sa lokacije:\n'{Path.GetFileName(sourcePath)}'\nće zameniti trenutne podatke u bazi.\n\nAutomatska sigurnosna kopija trenutnog stanja biće napravljena pre obnavljanja.\n\nDa li želite da nastavite?",
            "Potvrda obnavljanja baze",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (rez == MessageBoxResult.Yes)
        {
            if (BackupService.Instance.VratiBackup(sourcePath, out string err))
            {
                MessageBox.Show("Restauracija baze podataka je uspešno izvršena!\n\nPreporučuje se da ponovo pokrenete aplikaciju kako bi se svi izveštaji osvežili.",
                    "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                UcitajIstoriju();
            }
            else
            {
                MessageBox.Show($"Došlo je do greške pri obnavljanju baze:\n{err}",
                    "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnIzbrisiKopiju_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is BackupItem item)
        {
            var rez = MessageBox.Show($"Da li ste sigurni da želite da izbrišete kopiju '{item.NazivFajla}'?",
                "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (rez == MessageBoxResult.Yes)
            {
                if (BackupService.Instance.IzbrisiBackup(item.Putanja, out string err))
                {
                    UcitajIstoriju();
                }
                else
                {
                    MessageBox.Show($"Greška pri brisanju kopije:\n{err}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

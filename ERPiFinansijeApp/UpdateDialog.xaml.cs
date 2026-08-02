using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;

namespace ERPiFinansijeApp
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo _updateInfo;
        private readonly UpdateManager _updateManager;

        public UpdateDialog(UpdateInfo updateInfo, UpdateManager updateManager)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateManager = updateManager;

            MessageText.Text = $"Dostupna je nova verzija ({_updateInfo.TargetFullRelease.Version}). Da li želite da preuzmete i instalirate ažuriranje sada?";
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sakrij dugmad i prikaži progress bar
                ButtonPanel.Visibility = Visibility.Collapsed;
                ProgressPanel.Visibility = Visibility.Visible;
                MessageText.Text = "Preuzimanje ažuriranja. Molimo sačekajte...";

                // Preuzimanje sa praćenjem napretka
                await _updateManager.DownloadUpdatesAsync(_updateInfo, (progress) =>
                {
                    // Ažuriraj UI na glavnom threadu
                    Dispatcher.Invoke(() =>
                    {
                        UpdateProgress.Value = progress;
                        ProgressText.Text = $"Preuzimanje: {progress}%";
                    });
                });

                ProgressText.Text = "Ažuriranje preuzeto! Aplikacija se ponovo pokreće...";

                // Kratka pauza da korisnik pročita poruku
                await Task.Delay(1000);

                // Primeni i restartuj
                _updateManager.ApplyUpdatesAndRestart(_updateInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Došlo je do greške pri ažuriranju:\n{ex.Message}",
                    "Greška",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }
    }
}

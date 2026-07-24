using System.IO;
using System.Windows;
using QuestPDF.Infrastructure;
using Velopack;

namespace AccountingApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        VelopackApp.Build().Run();

        QuestPDF.Settings.License = LicenseType.Community;

        DispatcherUnhandledException += (s, ex) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AccountingApp", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now}] UI EXCEPTION: {ex.Exception}\n\n");
            MessageBox.Show(
                $"Neočekivana greška:\n\n{ex.Exception.Message}\n\nDetalji u:\n{logPath}",
                "Greška aplikacije",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AccountingApp", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now}] FATAL: {ex.ExceptionObject}\n\n");
        };

        base.OnStartup(e);
    }
}

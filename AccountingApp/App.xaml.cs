using System.IO;
using System.Windows;
using AccountingData;
using QuestPDF.Infrastructure;
using Velopack;

namespace AccountingApp;

public partial class App : Application
{
    public App()
    {
        AppLog.Init();
        AppLog.RegistrujGlobalneHandlere(this);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        VelopackApp.Build().Run();

        QuestPDF.Settings.License = LicenseType.Community;

        for (int i = 0; i < e.Args.Length; i++)
        {
            if (e.Args[i] == "--db-path" && i + 1 < e.Args.Length)
            {
                var customPath = e.Args[i + 1];
                if (File.Exists(customPath))
                {
                    UserSettings.Instance.ActiveDbPath = customPath;
                }
            }
        }

        var db = AccountingDbContext.Create(AppConfig.DbPath);
        var loginWindow = new Views.Korisnici.LoginWindow(db);
        loginWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            var freq = UserSettings.Instance.AutoBackupFrequency;
            if (freq == 1) // Pri svakom izlasku
            {
                Services.BackupService.Instance.NapraviAutomatskiBackup();
            }
            else if (freq == 2) // Jednom dnevno
            {
                var last = UserSettings.Instance.LastAutoBackupDate;
                if (last == null || last.Value.Date < DateTime.Now.Date)
                {
                    Services.BackupService.Instance.NapraviAutomatskiBackup();
                    UserSettings.Instance.LastAutoBackupDate = DateTime.Now;
                    UserSettings.Instance.Save();
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri automatskom pravljenju rezervne kopije prilikom izlaska");
        }

        AppLog.Zatvori();
        base.OnExit(e);
    }
}

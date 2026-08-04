using System.IO;
using System.Linq;
using System.Windows;
using ERPiFinansijeData;
using QuestPDF.Infrastructure;
using Velopack;

namespace ERPiFinansijeApp;

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

        // Mora pre prvog pristupa UserSettings-u: preuzima baze i podešavanja
        // zatečena pod starim imenom foldera (pre preimenovanja u ERPi liniju).
        AppConfig.PreuzmiStariFolderPodataka();

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

#if DEBUG
        // --autologin preskače ekran za prijavu i ulazi kao prvi aktivan administrator.
        // Postoji isključivo zbog UI automatizacije (.claude/skills/run-accounting-app):
        // slanje lozinke tastaturom je u tom okruženju nepouzdano, a i ne bi prošlo jer
        // LoginWindow s pravom zahteva promenu podrazumevane lozinke pre ulaska.
        // Ograđeno sa #if DEBUG — u Release build-u ovog koda nema, pa se prijava ne može
        // zaobići u isporučenoj aplikaciji.
        if (e.Args.Contains("--autologin") && PrijaviAdministratora(db))
        {
            base.OnStartup(e);
            return;
        }
#endif

        var loginWindow = new Views.Korisnici.LoginWindow(db);
        loginWindow.Show();

        base.OnStartup(e);
    }

#if DEBUG
    /// <summary>
    /// Otvara MainWindow kao prvi aktivan administrator, bez provere lozinke.
    /// Vraća false ako takvog korisnika nema, pa se pada nazad na ekran za prijavu.
    /// </summary>
    private static bool PrijaviAdministratora(AccountingDbContext db)
    {
        try
        {
            var korisnik = db.Korisnici.FirstOrDefault(k => k.IsActive && k.Uloga == "Administrator")
                           ?? db.Korisnici.FirstOrDefault(k => k.IsActive);
            if (korisnik == null) return false;

            AppSession.TrenutniKorisnik = korisnik;
            AppSession.TrenutnaFirma ??= db.Firme.FirstOrDefault(f => f.IsActive) ?? db.Firme.FirstOrDefault();

            new MainWindow(db).Show();
            return true;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Automatska prijava (--autologin) nije uspela");
            return false;
        }
    }
#endif

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

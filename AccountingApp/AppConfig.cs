using System;
using System.IO;
using System.Linq;
using AccountingData;

namespace AccountingApp;

public static class AppConfig
{
    public static string DefaultDbPath => @"C:\KNJIGE\Radni\KOR01\accounting_kor01.db";

    public static string BazeDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AccountingApp", "Baze"
    );

    /// <summary>
    /// Zamenjuje razmake i nevalidne znakove u imenu fajla sa '_', za bezbedno
    /// generisanje imena baze iz šifre/naziva firme.
    /// </summary>
    public static string SanitizujZaNazivFajla(string s)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(s.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
    }

    /// <summary>
    /// Jednokratna migracija: ako živa baza i dalje sedi na staroj, fiksnoj DOS lokaciji
    /// (C:\KNJIGE\Radni\KOR01\...) — koju samostalni AccountingMigration alat briše i
    /// ponovo pravi pri svakom reimportu — premešta je u BazeDir po istom obrascu
    /// imenovanja koji koristi uvoz (firma_{Sifra}_{Naziv}.db), analogno
    /// SredstvaApp.AppConfig.PrilagodiNazivZajednickeBaze. Bezbedna za pozivanje pri
    /// svakom pokretanju — nakon prve uspešne migracije DefaultDbPath više ne postoji
    /// (premešten), pa naredni pozivi odmah izlaze.
    /// </summary>
    private static void PrilagodiPostojecuBazu()
    {
        try
        {
            if (!File.Exists(DefaultDbPath)) return;

            var activePath = UserSettings.Instance.ActiveDbPath;
            bool trebaMigraciju = string.IsNullOrWhiteSpace(activePath) ||
                string.Equals(Path.GetFullPath(activePath), Path.GetFullPath(DefaultDbPath), StringComparison.OrdinalIgnoreCase);
            if (!trebaMigraciju) return;

            string sifra = "FIRMA";
            string naziv = "Firma";
            using (var ctx = AccountingDbContext.Create(DefaultDbPath))
            {
                var firma = ctx.Firme.FirstOrDefault();
                if (firma != null)
                {
                    sifra = firma.Sifra;
                    naziv = firma.Naziv;
                }
            }

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            Directory.CreateDirectory(BazeDir);
            var noviPath = Path.Combine(BazeDir, $"firma_{SanitizujZaNazivFajla(sifra)}_{SanitizujZaNazivFajla(naziv)}.db");

            if (File.Exists(noviPath))
            {
                File.Delete(DefaultDbPath);
            }
            else
            {
                File.Move(DefaultDbPath, noviPath);
            }

            UserSettings.Instance.ActiveDbPath = noviPath;
            UserSettings.Instance.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri migraciji postojeće baze u Baze folder: {ex.Message}");
        }
    }

    private static string? _dbPath = null;

    public static string DbPath
    {
        get
        {
            if (_dbPath == null)
            {
                PrilagodiPostojecuBazu();

                var savedPath = UserSettings.Instance.ActiveDbPath;
                if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
                {
                    _dbPath = savedPath;
                }
                else if (File.Exists(DefaultDbPath))
                {
                    _dbPath = DefaultDbPath;
                    UserSettings.Instance.ActiveDbPath = _dbPath;
                    UserSettings.Instance.Save();
                }
                else
                {
                    Directory.CreateDirectory(BazeDir);
                    var baze = Directory.GetFiles(BazeDir, "*.db");
                    _dbPath = baze.Length > 0 ? baze[0] : Path.Combine(BazeDir, "accounting.db");
                    UserSettings.Instance.ActiveDbPath = _dbPath;
                    UserSettings.Instance.Save();
                }
            }
            return _dbPath;
        }
        set
        {
            _dbPath = value;
            UserSettings.Instance.ActiveDbPath = value;
            UserSettings.Instance.Save();
        }
    }
}

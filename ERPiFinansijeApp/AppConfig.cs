using System;
using System.IO;
using System.Linq;
using ERPiFinansijeData;

namespace ERPiFinansijeApp;

public static class AppConfig
{
    public static string DefaultDbPath => @"C:\KNJIGE\Radni\KOR01\accounting_kor01.db";

    public static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ERPiFinansijeApp"
    );

    public static string BazeDir => Path.Combine(AppDataDir, "Baze");

    /// <summary>
    /// Folderi sa podacima pod starim imenima aplikacije (pre preimenovanja u ERPi liniju).
    /// Koriste se isključivo kao izvor jednokratnog preuzimanja podataka.
    /// </summary>
    private static string[] StariAppDataDirs => new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AccountingApp"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AccountingSystemApp")
    };

    /// <summary>Marker da je preuzimanje iz starog foldera već obavljeno.</summary>
    private static string MarkerPreuzimanja => Path.Combine(AppDataDir, "preuzeto_iz_starog_foldera.txt");

    /// <summary>
    /// Jednokratno preuzimanje SVIH zatečenih podataka iz foldera pod starim imenom
    /// aplikacije (%LOCALAPPDATA%\AccountingApp) u novi (%LOCALAPPDATA%\ERPiFinansijeApp) —
    /// baze, rezervne kopije, podešavanja i logove.
    ///
    /// Preimenovanje u ERPi liniju promenilo je i ime foldera sa podacima, pa bi bez ovoga
    /// nova verzija startovala sa praznim spiskom firmi iako baze i dalje postoje na disku.
    ///
    /// Podaci se KOPIRAJU, ne premeštaju — stara instalacija ostaje upotrebljiva dok se
    /// korisnik ne uveri da je sve preneto. Da se obrisana baza ne bi vraćala pri svakom
    /// pokretanju, uspešno preuzimanje se beleži marker fajlom.
    ///
    /// Mora da se pozove PRE prvog pristupa <see cref="UserSettings.Instance"/>, jer se
    /// odmah po kopiranju premapira putanja aktivne baze.
    /// </summary>
    public static void PreuzmiStariFolderPodataka()
    {
        try
        {
            var izvori = StariAppDataDirs.Where(Directory.Exists).ToArray();
            if (izvori.Length == 0) return;

            Directory.CreateDirectory(AppDataDir);
            if (File.Exists(MarkerPreuzimanja)) return;

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            int kopirano = 0;
            foreach (var izvor in izvori)
            {
                kopirano += KopirajFolder(izvor, AppDataDir);
            }

            PremapirajAktivnuBazu();

            File.WriteAllText(MarkerPreuzimanja,
                $"Podaci su preuzeti iz: {string.Join(", ", izvori)} dana {DateTime.Now:dd.MM.yyyy. HH:mm:ss}.{Environment.NewLine}" +
                $"Kopirano fajlova: {kopirano}. Original je ostao netaknut i može se obrisati ručno.{Environment.NewLine}" +
                $"Brisanje ovog fajla ponovo pokreće preuzimanje pri sledećem startu.{Environment.NewLine}");

            Serilog.Log.Information(
                "Preuzeto {Broj} fajlova iz starih foldera {Izvori} u {Odrediste}",
                kopirano, izvori, AppDataDir);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri preuzimanju podataka iz starog foldera aplikacije");
        }
    }

    /// <summary>
    /// Rekurzivno kopira ceo sadržaj foldera. Fajl koji na odredištu već postoji se ne dira —
    /// novi podaci uvek pobeđuju nad zatečenim.
    /// </summary>
    private static int KopirajFolder(string izvor, string odrediste)
    {
        int kopirano = 0;
        Directory.CreateDirectory(odrediste);

        foreach (var fajl in Directory.GetFiles(izvor))
        {
            try
            {
                var cilj = Path.Combine(odrediste, Path.GetFileName(fajl));
                if (File.Exists(cilj)) continue;

                File.Copy(fajl, cilj);
                kopirano++;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Fajl {Fajl} nije kopiran iz starog foldera", fajl);
            }
        }

        foreach (var podfolder in Directory.GetDirectories(izvor))
        {
            kopirano += KopirajFolder(podfolder, Path.Combine(odrediste, Path.GetFileName(podfolder)));
        }

        return kopirano;
    }

    /// <summary>
    /// Aktivna baza iz starih podešavanja pokazuje na stari folder. Ako nova instalacija
    /// još nema ispravnu aktivnu bazu, preuzima se ona iz starih podešavanja — sada iz
    /// kopije u novom folderu.
    /// </summary>
    private static void PremapirajAktivnuBazu()
    {
        try
        {
            var aktivna = UserSettings.Instance.ActiveDbPath;
            if (!string.IsNullOrWhiteSpace(aktivna) && File.Exists(aktivna)) return;

            var staraAktivna = aktivna;
            if (string.IsNullOrWhiteSpace(staraAktivna))
            {
                staraAktivna = StariAppDataDirs
                    .Select(dir => Path.Combine(dir, "settings.json"))
                    .Where(File.Exists)
                    .Select(putanja => System.Text.Json.JsonSerializer
                        .Deserialize<UserSettings>(File.ReadAllText(putanja))?.ActiveDbPath)
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            }

            if (string.IsNullOrWhiteSpace(staraAktivna)) return;

            var kandidat = Path.Combine(BazeDir, Path.GetFileName(staraAktivna));
            if (!File.Exists(kandidat)) return;

            UserSettings.Instance.ActiveDbPath = kandidat;
            UserSettings.Instance.Save();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Aktivna baza iz starih podešavanja nije premapirana");
        }
    }

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
    /// (C:\KNJIGE\Radni\KOR01\...) — koju samostalni ERPiFinansijeMigration alat briše i
    /// ponovo pravi pri svakom reimportu — premešta je u BazeDir po istom obrascu
    /// imenovanja koji koristi uvoz (firma_{Sifra}_{Naziv}.db), analogno
    /// ERPiSredstvaApp.AppConfig.PrilagodiNazivZajednickeBaze. Bezbedna za pozivanje pri
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
            Serilog.Log.Error(ex, "Greška pri migraciji postojeće baze u Baze folder");
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

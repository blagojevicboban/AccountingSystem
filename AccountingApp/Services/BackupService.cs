using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AccountingApp.Services;

/// <summary>
/// Servis za upravljanje rezervnim kopijama (Backup & Restore) SQLite baza podataka u AccountingApp.
/// </summary>
public class BackupService
{
    private static BackupService? _instance;
    public static BackupService Instance => _instance ??= new BackupService();

    /// <summary>
    /// Direktorijum gde se čuvaju automatski i sigurnosni backup-i.
    /// </summary>
    public string BackupDir
    {
        get
        {
            var custom = UserSettings.Instance.CustomBackupFolder;
            if (!string.IsNullOrWhiteSpace(custom) && Directory.Exists(custom))
            {
                return custom;
            }
            return Path.Combine(AppConfig.BazeDir, "RezervneKopije");
        }
    }

    /// <summary>
    /// Pravi ručnu rezervnu kopiju na proizvoljnu putanju koju korisnik izabere.
    /// </summary>
    public void NapraviRucniBackup(string destPath)
    {
        var dbPath = AppConfig.DbPath;
        if (!File.Exists(dbPath))
        {
            throw new FileNotFoundException("Aktivna baza podataka ne postoji na navedenoj putanji!");
        }

        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Copy(dbPath, destPath, true);
    }

    /// <summary>
    /// Kreira automatsku kopiju trenutne baze podataka i vrši rotaciju starih kopija.
    /// </summary>
    public string NapraviAutomatskiBackup(bool preVracanja = false)
    {
        var dbPath = AppConfig.DbPath;
        if (!File.Exists(dbPath))
        {
            return string.Empty;
        }

        try
        {
            Directory.CreateDirectory(BackupDir);

            var dbName = Path.GetFileNameWithoutExtension(dbPath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var tipSuffix = preVracanja ? "pre_vracanja" : "auto";
            var backupFileName = $"{dbName}_{tipSuffix}_{timestamp}.db";
            var backupPath = Path.Combine(BackupDir, backupFileName);

            File.Copy(dbPath, backupPath, true);
            RotirajStareKopije();

            return backupPath;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri pravljenju automatske kopije");
            return string.Empty;
        }
    }

    /// <summary>
    /// Vraća bazu podataka iz izabrane kopije.
    /// Pre vraćanja pravi automatsku sigurnosnu kopiju trenutne baze.
    /// </summary>
    public bool VratiBackup(string sourcePath, out string errorMsg)
    {
        errorMsg = string.Empty;

        if (!File.Exists(sourcePath))
        {
            errorMsg = "Izabrana rezervna kopija ne postoji!";
            return false;
        }

        try
        {
            var destPath = AppConfig.DbPath;

            // 1. Napravi automatsku sigurnosnu kopiju pre nego što prepišemo bazu
            NapraviAutomatskiBackup(preVracanja: true);

            // 2. Oslobodi sve SQLite konekcije iz pool-a kako bismo otključali fajl na disku
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            // 3. Kopiraj rezervnu kopiju preko aktivne baze podataka
            File.Copy(sourcePath, destPath, true);

            return true;
        }
        catch (Exception ex)
        {
            errorMsg = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Učitava sve rezervne kopije iz Backup foldera i parsira njihove detalje.
    /// </summary>
    public List<BackupItem> UcitajIstorijuKopija()
    {
        var list = new List<BackupItem>();
        var dirPath = BackupDir;
        if (!Directory.Exists(dirPath))
        {
            return list;
        }

        try
        {
            var files = Directory.GetFiles(dirPath, "*.db");
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var filename = fileInfo.Name;

                string tip = "Automatski";
                if (filename.Contains("pre_vracanja"))
                {
                    tip = "Pre vraćanja (Sigurnosni)";
                }
                else if (filename.Contains("_rucni_") || (!filename.Contains("_auto_") && !filename.Contains("pre_vracanja")))
                {
                    tip = "Ručni / Ostalo";
                }

                list.Add(new BackupItem
                {
                    NazivFajla = filename,
                    Putanja = file,
                    DatumKreiranja = fileInfo.LastWriteTime,
                    VelicinaMB = (double)fileInfo.Length / (1024 * 1024),
                    Tip = tip
                });
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri učitavanju istorije kopija");
        }

        return list.OrderByDescending(b => b.DatumKreiranja).ToList();
    }

    /// <summary>
    /// Briše pojedinačni fajl kopije.
    /// </summary>
    public bool IzbrisiBackup(string path, out string errorMsg)
    {
        errorMsg = string.Empty;
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            errorMsg = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Briše stare automatske i sigurnosne kopije.
    /// Čuvamo poslednjih 15 automatskih i poslednjih 5 sigurnosnih kopija pre vraćanja.
    /// </summary>
    private void RotirajStareKopije()
    {
        var dirPath = BackupDir;
        if (!Directory.Exists(dirPath)) return;

        try
        {
            var files = Directory.GetFiles(dirPath, "*.db")
                .Select(f => new FileInfo(f))
                .ToList();

            // 1. Rotacija za automatske kopije
            var autoBackups = files
                .Where(f => f.Name.Contains("_auto_"))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (autoBackups.Count > 15)
            {
                for (int i = 15; i < autoBackups.Count; i++)
                {
                    try { autoBackups[i].Delete(); }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Greška pri brisanju stare auto kopije"); }
                }
            }

            // 2. Rotacija za sigurnosne kopije pre vraćanja
            var safetyBackups = files
                .Where(f => f.Name.Contains("pre_vracanja"))
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();

            if (safetyBackups.Count > 5)
            {
                for (int i = 5; i < safetyBackups.Count; i++)
                {
                    try { safetyBackups[i].Delete(); }
                    catch (Exception ex) { Serilog.Log.Error(ex, "Greška pri brisanju stare sigurnosne kopije"); }
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri rotaciji starih kopija");
        }
    }
}

/// <summary>
/// Model za stavku u istoriji rezervnih kopija.
/// </summary>
public class BackupItem
{
    public string NazivFajla { get; set; } = string.Empty;
    public string Putanja { get; set; } = string.Empty;
    public DateTime DatumKreiranja { get; set; }
    public double VelicinaMB { get; set; }
    public string Tip { get; set; } = string.Empty;

    public string VelicinaPrikaz => $"{VelicinaMB:F2} MB";
    public string DatumPrikaz => DatumKreiranja.ToString("dd.MM.yyyy HH:mm:ss");
}

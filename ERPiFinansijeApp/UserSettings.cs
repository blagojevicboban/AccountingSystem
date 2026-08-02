using System;
using System.IO;
using System.Text.Json;

namespace ERPiFinansijeApp;

public class UserSettings
{
    private static readonly string SettingsFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ERPiFinansijeApp", "settings.json"
    );

    public string? ActiveDbPath { get; set; }
    public bool StartMaximized { get; set; } = true;

    // 0 = Nikad, 1 = Pri svakom izlasku, 2 = Jednom dnevno
    public int AutoBackupFrequency { get; set; } = 1; 
    public DateTime? LastAutoBackupDate { get; set; }
    public string? CustomBackupFolder { get; set; }

    // Podaci za štampu / izveštaje
    public string? NazivServisa { get; set; }
    public string? OvlascenoLice { get; set; }

    // Bezbednost i korisnički interfejs
    public bool PotvrdaZaRasknjizavanje { get; set; } = true;
    public bool PotvrdaZaBrisanje { get; set; } = true;

    private static UserSettings? _instance;
    public static UserSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Load();
            }
            return _instance;
        }
    }

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri učitavanju podešavanja");
        }

        return new UserSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFile);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri čuvanju podešavanja");
        }
    }
}

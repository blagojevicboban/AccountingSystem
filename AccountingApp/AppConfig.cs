using System.IO;

namespace AccountingApp;

public static class AppConfig
{
    public static string DefaultDbPath => @"C:\KNJIGE\Radni\KOR01\accounting_kor01.db";

    public static string BazeDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AccountingApp", "Baze"
    );

    private static string? _dbPath = null;

    public static string DbPath
    {
        get
        {
            if (_dbPath == null)
            {
                if (File.Exists(DefaultDbPath))
                {
                    _dbPath = DefaultDbPath;
                }
                else
                {
                    Directory.CreateDirectory(BazeDir);
                    var baze = Directory.GetFiles(BazeDir, "*.db");
                    _dbPath = baze.Length > 0 ? baze[0] : Path.Combine(BazeDir, "accounting.db");
                }
            }
            return _dbPath;
        }
        set
        {
            _dbPath = value;
        }
    }
}

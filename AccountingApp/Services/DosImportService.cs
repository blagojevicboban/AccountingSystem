using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;

namespace AccountingApp.Services;

public class DbfFirmaDto : INotifyPropertyChanged
{
    public string Sifra { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string Pib { get; set; } = "";
    public string MaticniBroj { get; set; } = "";
    public string Adresa { get; set; } = "";
    public string PttIMesto { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string ZiroRacun { get; set; } = "";
    public string FolderPath { get; set; } = "";

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class DosImportProgress
{
    public string FirmName { get; set; } = "";
    public string StepDescription { get; set; } = "";
    public int Percentage { get; set; }
    public string LogMessage { get; set; } = "";
}

public class DosImportService
{
    private static DosImportService? _instance;
    public static DosImportService Instance => _instance ??= new DosImportService();

    private DosImportService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Skenira zadati radni direktorijum i pronalazi sve dostupne firme.
    /// Ako postoji KORISNIC.DBF koristi ga, u suprotnom skenira podfoldere sa DBF fajlovima.
    /// </summary>
    public List<DbfFirmaDto> SkenirajRadniDirektorijum(string radniDir)
    {
        var firme = new List<DbfFirmaDto>();
        if (!Directory.Exists(radniDir)) return firme;

        var korisnicFile = Path.Combine(radniDir, "KORISNIC.DBF");
        if (File.Exists(korisnicFile))
        {
            var rows = DbfImportService.ReadRows(korisnicFile);
            foreach (var r in rows)
            {
                string sifra = GetVal(r, "KOR", "SIFRA", "KOD");
                string naziv = GetVal(r, "IME", "NAZIV", "FIRMA");
                string pib = GetVal(r, "PIB");
                string mb = GetVal(r, "MB", "MATICNI");
                // "UL" nosi celu vrednost "Ulica i broj", a "BR" (uprkos imenu) nosi
                // "Mesto i post. br." — potvrđeno u FIN2.PRG novikorisnik()/izmenakorisnika().
                string adresa = GetVal(r, "UL", "ADRESA", "ULICA");
                string mesto = GetVal(r, "BR", "GRAD", "MESTO");
                string ziro = GetVal(r, "Z", "ZIRO", "RACUN");
                string tel = GetVal(r, "TEL", "TELEFON");

                if (!string.IsNullOrWhiteSpace(sifra))
                {
                    var folderName = "KOR" + sifra.PadLeft(2, '0');
                    var folderPath = Path.Combine(radniDir, folderName);
                    if (!Directory.Exists(folderPath))
                    {
                        folderPath = Path.Combine(radniDir, "KOR" + sifra);
                    }

                    if (Directory.Exists(folderPath))
                    {
                        firme.Add(new DbfFirmaDto
                        {
                            Sifra = folderName,
                            Naziv = string.IsNullOrWhiteSpace(naziv) ? $"Firma {sifra}" : naziv,
                            Pib = pib,
                            MaticniBroj = mb,
                            Adresa = adresa,
                            PttIMesto = mesto,
                            Telefon = tel,
                            ZiroRacun = ziro,
                            FolderPath = folderPath,
                            IsSelected = true
                        });
                    }
                }
            }
        }

        // Ako KORISNIC.DBF nije postojao ili je bio prazan, skeniraj direktne podfoldere
        if (!firme.Any())
        {
            var dirs = Directory.GetDirectories(radniDir);
            foreach (var dir in dirs)
            {
                var dbfFiles = Directory.GetFiles(dir, "*.DBF");
                if (dbfFiles.Any())
                {
                    var folderName = Path.GetFileName(dir);
                    firme.Add(new DbfFirmaDto
                    {
                        Sifra = folderName,
                        Naziv = $"Firma {folderName}",
                        FolderPath = dir,
                        IsSelected = true
                    });
                }
            }
        }

        return firme;
    }

    /// <summary>
    /// Izvršava uvoz tako što za svaku firmu kreira njenu zasebnu SQLite bazu (kao u SredstvaApp).
    /// </summary>
    public async Task UveziFirmeAsync(List<DbfFirmaDto> izabraneFirme, IProgress<DosImportProgress> progress)
    {
        await Task.Run(async () =>
        {
            int totalFirme = izabraneFirme.Count;
            int currentFirmaIdx = 0;

            foreach (var firmaDto in izabraneFirme)
            {
                currentFirmaIdx++;
                int basePercent = (int)(((double)(currentFirmaIdx - 1) / totalFirme) * 100);

                Report(progress, firmaDto.Naziv, "Inicijalizacija baze", basePercent, $"🚀 Kreiranje zasebne baze za firmu: {firmaDto.Naziv} ({firmaDto.Sifra})...");

                // Putanja do zasebne baze u Baze folderu (npr. %LocalAppData%\AccountingApp\Baze\firma_KOR01_ARHIBEL_-_2026.db),
                // NE u DOS folderu firme — taj folder je izvor za reimport i AccountingMigration ga briše/pravi iznova.
                Directory.CreateDirectory(AppConfig.BazeDir);
                string dbFileName = $"firma_{AppConfig.SanitizujZaNazivFajla(firmaDto.Sifra)}_{AppConfig.SanitizujZaNazivFajla(firmaDto.Naziv)}.db";
                string firmaDbPath = Path.Combine(AppConfig.BazeDir, dbFileName);

                // Ako baza već postoji u folderu firme, brišemo je radi čistog uvoza
                if (File.Exists(firmaDbPath))
                {
                    try { File.Delete(firmaDbPath); } catch { }
                }

                using var firmDb = AccountingDbContext.Create(firmaDbPath);

                // 1. Unos Firme u zasebnu bazu
                var dbFirma = firmDb.Firme.FirstOrDefault(f => f.Sifra == firmaDto.Sifra || f.Naziv == firmaDto.Naziv);
                if (dbFirma == null)
                {
                    dbFirma = new Firma
                    {
                        Sifra = firmaDto.Sifra,
                        Naziv = firmaDto.Naziv,
                        Pib = firmaDto.Pib,
                        MaticniBroj = firmaDto.MaticniBroj,
                        Adresa = firmaDto.Adresa,
                        PttIMesto = firmaDto.PttIMesto,
                        Telefon = firmaDto.Telefon,
                        ZiroRacun = firmaDto.ZiroRacun,
                        IsActive = true
                    };
                    firmDb.Firme.Add(dbFirma);
                    await firmDb.SaveChangesAsync();
                }

                if (AppSession.TrenutnaFirma == null)
                {
                    AppSession.TrenutnaFirma = dbFirma;
                }

                // In-memory setovi za pojedinačnu bazu firme
                var existingKonta = firmDb.Konta.Select(k => k.BrojKonta).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingPartneri = firmDb.Partneri.Select(p => p.SifraPartnera).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingMagacini = firmDb.Magacini.Select(m => m.SifraMagacina).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingArtikli = firmDb.Artikli.Select(a => a.SifraArtikla).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingNalogi = firmDb.Nalozi.Select(n => n.BrojNaloga).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingPromene = firmDb.Promene.Select(p => p.Sifra).ToHashSet();

                // 2. Kontni plan (KONTPLAN.DBF)
                var kontplanFile = Path.Combine(firmaDto.FolderPath, "KONTPLAN.DBF");
                if (File.Exists(kontplanFile))
                {
                    Report(progress, firmaDto.Naziv, "Kontni plan", basePercent + 5, "📋 Uvoz Kontnog plana (KONTPLAN.DBF)...");
                    var rows = DbfImportService.ReadRows(kontplanFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var konto = DbfImportService.MapKonto(r);
                        if (konto != null && existingKonta.Add(konto.BrojKonta))
                        {
                            firmDb.Konta.Add(konto);
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Kontni plan", basePercent + 10, $"   --> Uvezeno {count} novih konta!");
                }

                // 3. Partneri (ANKONT.DBF)
                var ankontFile = Path.Combine(firmaDto.FolderPath, "ANKONT.DBF");
                if (File.Exists(ankontFile))
                {
                    Report(progress, firmaDto.Naziv, "Partneri", basePercent + 15, "👥 Uvoz Partnera (ANKONT.DBF)...");
                    var rows = DbfImportService.ReadRows(ankontFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var partner = DbfImportService.MapPartner(r, count + 1);
                        if (partner != null && existingPartneri.Add(partner.SifraPartnera))
                        {
                            firmDb.Partneri.Add(partner);
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Partneri", basePercent + 25, $"   --> Uvezeno {count} novih partnera!");
                }

                // 4. Magacini i Artikli (MAGACIN.DBF i ARTIKLI.DBF)
                var magacinFile = Path.Combine(firmaDto.FolderPath, "MAGACIN.DBF");
                if (File.Exists(magacinFile))
                {
                    Report(progress, firmaDto.Naziv, "Magacini", basePercent + 30, "📦 Uvoz Magacina (MAGACIN.DBF)...");
                    var rows = DbfImportService.ReadRows(magacinFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var magacin = DbfImportService.MapMagacin(r);
                        if (magacin != null && existingMagacini.Add(magacin.SifraMagacina))
                        {
                            firmDb.Magacini.Add(magacin);
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Magacini", basePercent + 35, $"   --> Uvezeno {count} magacina!");
                }

                var artikliFile = Path.Combine(firmaDto.FolderPath, "ARTIKLI.DBF");
                if (File.Exists(artikliFile))
                {
                    Report(progress, firmaDto.Naziv, "Artikli", basePercent + 40, "🛒 Uvoz Artikala (ARTIKLI.DBF)...");
                    var rows = DbfImportService.ReadRows(artikliFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var artikal = DbfImportService.MapArtikal(r);
                        if (artikal != null && existingArtikli.Add(artikal.SifraArtikla))
                        {
                            firmDb.Artikli.Add(artikal);
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Artikli", basePercent + 55, $"   --> Uvezeno {count} novih artikala!");
                }

                // 5. Nalozi za knjiženje (NALOG.DBF)
                var nalogFile = Path.Combine(firmaDto.FolderPath, "NALOG.DBF");

                if (File.Exists(nalogFile))
                {
                    Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 60, "📖 Uvoz Naloga za knjiženje i stavki (NALOG.DBF)...");
                    var rows = DbfImportService.ReadRows(nalogFile);
                    var groups = DbfImportService.GroupNalogRows(rows);

                    int nalogiCount = 0;
                    int stavkeCount = 0;

                    foreach (var (bNaloga, redovi) in groups)
                    {
                        if (!existingNalogi.Add(bNaloga)) continue;

                        var nalog = DbfImportService.MapNalogGrupa(bNaloga, redovi);
                        if (nalog == null) continue;

                        firmDb.Nalozi.Add(nalog);
                        nalogiCount++;
                        stavkeCount += nalog.Stavke.Count;
                    }

                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 90, $"   --> Uvezeno {nalogiCount} naloga i {stavkeCount} stavki knjiženja u zasebnu bazu!");
                }

                // 6. Šifarnik opisa promena (PROMENE.DBF) — razlikuje se po firmi, nije deljen rečnik
                var promeneFile = Path.Combine(firmaDto.FolderPath, "PROMENE.DBF");
                if (File.Exists(promeneFile))
                {
                    Report(progress, firmaDto.Naziv, "Šifarnik promena", basePercent + 92, "🏷️ Uvoz šifarnika opisa promena (PROMENE.DBF)...");
                    var rows = DbfImportService.ReadRows(promeneFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var promena = DbfImportService.MapPromena(r);
                        if (promena != null && existingPromene.Add(promena.Sifra))
                        {
                            firmDb.Promene.Add(promena);
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Šifarnik promena", basePercent + 95, $"   --> Uvezeno {count} šifara promena!");
                }

                Report(progress, firmaDto.Naziv, "Završeno", basePercent + 100, $"✅ Uspešno kreirana i uvežena baza za firmu: {firmaDto.Naziv} ({dbFileName})!\n");
            }
        });
    }

    private void Report(IProgress<DosImportProgress>? progress, string firm, string step, int pct, string logMsg)
    {
        progress?.Report(new DosImportProgress
        {
            FirmName = firm,
            StepDescription = step,
            Percentage = Math.Min(100, pct),
            LogMessage = logMsg
        });
    }

    private string GetVal(Dictionary<string, string> row, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (row.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;
        }
        return "";
    }
}

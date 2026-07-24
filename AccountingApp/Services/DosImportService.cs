using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AccountingData;
using AccountingData.Models;
using DbfDataReader;

namespace AccountingApp.Services;

public class DbfFirmaDto
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
    public bool IsSelected { get; set; } = true;
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
            var rows = ReadDbfRows(korisnicFile);
            foreach (var r in rows)
            {
                string sifra = GetVal(r, "KOR", "SIFRA", "KOD");
                string naziv = GetVal(r, "IME", "NAZIV", "FIRMA");
                string pib = GetVal(r, "PIB");
                string mb = GetVal(r, "MB", "MATICNI");
                string mesto = GetVal(r, "GRAD", "MESTO");
                string adresa = GetVal(r, "ADRESA", "ULICA");
                string ziro = GetVal(r, "ZIRO", "RACUN");
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
                            Sifra = "KOR" + sifra,
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
    /// Izvršava masovni uvoz izabranih firmi u bazu podataka.
    /// </summary>
    public async Task UveziFirmeAsync(List<DbfFirmaDto> izabraneFirme, AccountingDbContext db, IProgress<DosImportProgress> progress)
    {
        await Task.Run(async () =>
        {
            int totalFirme = izabraneFirme.Count;
            int currentFirmaIdx = 0;

            // In-memory setovi za brzu proveru duplikata i sprečavanje jedinstvenih indeksa grešaka
            var existingKonta = db.Konta.Select(k => k.BrojKonta).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingPartneri = db.Partneri.Select(p => p.SifraPartnera).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingMagacini = db.Magacini.Select(m => m.SifraMagacina).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingArtikli = db.Artikli.Select(a => a.SifraArtikla).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingNalogi = db.Nalozi.Select(n => n.BrojNaloga).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var firmaDto in izabraneFirme)
            {
                currentFirmaIdx++;
                int basePercent = (int)(((double)(currentFirmaIdx - 1) / totalFirme) * 100);

                Report(progress, firmaDto.Naziv, "Inicijalizacija", basePercent, $"🚀 Započet uvoz za firmu: {firmaDto.Naziv} ({firmaDto.Sifra})...");

                // 1. Unos ili osvežavanje Firme
                var dbFirma = db.Firme.FirstOrDefault(f => f.Sifra == firmaDto.Sifra || f.Naziv == firmaDto.Naziv);
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
                    db.Firme.Add(dbFirma);
                    await db.SaveChangesAsync();
                }

                if (AppSession.TrenutnaFirma == null)
                {
                    AppSession.TrenutnaFirma = dbFirma;
                }

                // 2. Kontni plan (KONTPLAN.DBF)
                var kontplanFile = Path.Combine(firmaDto.FolderPath, "KONTPLAN.DBF");
                if (File.Exists(kontplanFile))
                {
                    Report(progress, firmaDto.Naziv, "Kontni plan", basePercent + 5, "📋 Uvoz Kontnog plana (KONTPLAN.DBF)...");
                    var rows = ReadDbfRows(kontplanFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        string kBroj = GetVal(r, "BROJ", "KONTO", "SIFRA").Trim();
                        string kNaziv = GetVal(r, "NAZIV", "IME").Trim();

                        if (!string.IsNullOrWhiteSpace(kBroj) && existingKonta.Add(kBroj))
                        {
                            int klasa = 0;
                            if (kBroj.Length > 0 && char.IsDigit(kBroj[0])) klasa = kBroj[0] - '0';

                            db.Konta.Add(new Konto
                            {
                                BrojKonta = kBroj,
                                NazivKonta = string.IsNullOrWhiteSpace(kNaziv) ? $"Konto {kBroj}" : kNaziv,
                                Klasa = klasa,
                                IsSintetika = kBroj.Length <= 3
                            });
                            count++;
                        }
                    }
                    await db.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Kontni plan", basePercent + 10, $"   --> Uvezeno {count} novih konta!");
                }

                // 3. Partneri (ANKONT.DBF)
                var ankontFile = Path.Combine(firmaDto.FolderPath, "ANKONT.DBF");
                if (File.Exists(ankontFile))
                {
                    Report(progress, firmaDto.Naziv, "Partneri", basePercent + 15, "👥 Uvoz Partnera (ANKONT.DBF)...");
                    var rows = ReadDbfRows(ankontFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        string pSifra = GetVal(r, "SIFRA", "KOD", "BROJ").Trim();
                        string pNaziv = GetVal(r, "NAZIV", "IME", "FIRMA").Trim();
                        string adresa = GetVal(r, "ADRESA", "ULICA").Trim();
                        string mesto = GetVal(r, "MESTO", "GRAD").Trim();
                        string ziro = GetVal(r, "ZIRO", "RACUN").Trim();
                        string tel = GetVal(r, "TEL", "TELEFON").Trim();
                        string pib = GetVal(r, "PIB").Trim();

                        if (!string.IsNullOrWhiteSpace(pNaziv))
                        {
                            if (string.IsNullOrWhiteSpace(pSifra)) pSifra = (count + 1).ToString("D4");

                            if (existingPartneri.Add(pSifra))
                            {
                                db.Partneri.Add(new Partner
                                {
                                    SifraPartnera = pSifra,
                                    Naziv = pNaziv,
                                    Adresa = adresa,
                                    PttIMesto = mesto,
                                    ZiroRacun = ziro,
                                    Telefon = tel,
                                    Pib = pib
                                });
                                count++;
                            }
                        }
                    }
                    await db.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Partneri", basePercent + 25, $"   --> Uvezeno {count} novih partnera!");
                }

                // 4. Magacini i Artikli (MAGACIN.DBF i ARTIKLI.DBF)
                var magacinFile = Path.Combine(firmaDto.FolderPath, "MAGACIN.DBF");
                if (File.Exists(magacinFile))
                {
                    Report(progress, firmaDto.Naziv, "Magacini", basePercent + 30, "📦 Uvoz Magacina (MAGACIN.DBF)...");
                    var rows = ReadDbfRows(magacinFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        string mSifra = GetVal(r, "SIFRA", "MAG", "KOD").Trim();
                        string mNaziv = GetVal(r, "NAZIV", "IME").Trim();

                        if (!string.IsNullOrWhiteSpace(mSifra) && existingMagacini.Add(mSifra))
                        {
                            db.Magacini.Add(new Magacin
                            {
                                SifraMagacina = mSifra,
                                NazivMagacina = string.IsNullOrWhiteSpace(mNaziv) ? $"Magacin {mSifra}" : mNaziv,
                                VrstaMagacina = "Veleprodaja"
                            });
                            count++;
                        }
                    }
                    await db.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Magacini", basePercent + 35, $"   --> Uvezeno {count} magacina!");
                }

                var artikliFile = Path.Combine(firmaDto.FolderPath, "ARTIKLI.DBF");
                if (File.Exists(artikliFile))
                {
                    Report(progress, firmaDto.Naziv, "Artikli", basePercent + 40, "🛒 Uvoz Artikala (ARTIKLI.DBF)...");
                    var rows = ReadDbfRows(artikliFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        string aSifra = GetVal(r, "SIFRA", "KOD", "ARTIKAL").Trim();
                        string aNaziv = GetVal(r, "NAZIV", "IME").Trim();
                        string jm = GetVal(r, "JM", "JEDMERA").Trim();
                        string cenaStr = GetVal(r, "CENA", "NCENA", "PCENA").Trim();

                        decimal.TryParse(cenaStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cena);

                        if (!string.IsNullOrWhiteSpace(aSifra) && existingArtikli.Add(aSifra))
                        {
                            db.Artikli.Add(new Artikal
                            {
                                SifraArtikla = aSifra,
                                Naziv = string.IsNullOrWhiteSpace(aNaziv) ? $"Artikal {aSifra}" : aNaziv,
                                JedinicaMere = string.IsNullOrWhiteSpace(jm) ? "kom" : jm,
                                NabavnaCena = cena
                            });
                            count++;
                        }
                    }
                    await db.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Artikli", basePercent + 55, $"   --> Uvezeno {count} novih artikala!");
                }

                // 5. Nalozi za knjiženje (NALOGI.DBF + NALSTAV.DBF)
                var nalogiFile = Path.Combine(firmaDto.FolderPath, "NALOGI.DBF");
                var nalstavFile = Path.Combine(firmaDto.FolderPath, "NALSTAV.DBF");

                if (File.Exists(nalogiFile) && File.Exists(nalstavFile))
                {
                    Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 60, "📖 Uvoz Naloga za knjiženje i stavki...");
                    var nalogRows = ReadDbfRows(nalogiFile);
                    var stavkaRows = ReadDbfRows(nalstavFile);

                    // Grupiši stavke po broju naloga
                    var stavkeGrouped = stavkaRows
                        .GroupBy(s => GetVal(s, "NALOG", "BROJ", "ID").Trim())
                        .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                    int nalogiCount = 0;
                    int stavkeCount = 0;

                    foreach (var nr in nalogRows)
                    {
                        string bNaloga = GetVal(nr, "BROJ", "NALOG", "ID").Trim();
                        string datumStr = GetVal(nr, "DATUM", "DAT").Trim();
                        string opis = GetVal(nr, "OPIS", "NAPOMENA").Trim();

                        if (string.IsNullOrWhiteSpace(bNaloga)) continue;

                        DateTime datum = DateTime.Now;
                        if (DateTime.TryParse(datumStr, out var dParsed)) datum = dParsed;

                        if (existingNalogi.Add(bNaloga))
                        {
                            var nalog = new Nalog
                            {
                                BrojNaloga = bNaloga,
                                DatumNaloga = datum,
                                Opis = string.IsNullOrWhiteSpace(opis) ? $"Nalog {bNaloga}" : opis,
                                IsKnjizen = true,
                                DatumKnjiženja = datum
                            };

                            decimal ukupnoDuguje = 0;
                            decimal ukupnoPotražuje = 0;

                            if (stavkeGrouped.TryGetValue(bNaloga, out var myStavke))
                            {
                                foreach (var sr in myStavke)
                                {
                                    string konto = GetVal(sr, "KONTO", "BROJ").Trim();
                                    string stOpis = GetVal(sr, "OPIS", "TEKST").Trim();
                                    string dugStr = GetVal(sr, "DUGUJE", "DUG").Trim();
                                    string potStr = GetVal(sr, "POTRAZUJE", "POT").Trim();

                                    decimal.TryParse(dugStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dug);
                                    decimal.TryParse(potStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pot);

                                    if (!string.IsNullOrWhiteSpace(konto))
                                    {
                                        nalog.Stavke.Add(new StavkaNaloga
                                        {
                                            BrojKonta = konto,
                                            Opis = string.IsNullOrWhiteSpace(stOpis) ? nalog.Opis : stOpis,
                                            Duguje = dug,
                                            Potrazuje = pot
                                        });

                                        ukupnoDuguje += dug;
                                        ukupnoPotražuje += pot;
                                        stavkeCount++;
                                    }
                                }
                            }

                            nalog.UkupnoDuguje = ukupnoDuguje;
                            nalog.UkupnoPotrazuje = ukupnoPotražuje;

                            db.Nalozi.Add(nalog);
                            nalogiCount++;
                        }
                    }

                    await db.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 90, $"   --> Uvezeno {nalogiCount} naloga i {stavkeCount} stavki knjiženja!");
                }

                Report(progress, firmaDto.Naziv, "Završeno", basePercent + 100, $"✅ Uvoz za firmu {firmaDto.Naziv} uspešno završen!\n");
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

    private List<Dictionary<string, string>> ReadDbfRows(string filepath)
    {
        var list = new List<Dictionary<string, string>>();
        if (!File.Exists(filepath)) return list;

        try
        {
            var encoding = Encoding.GetEncoding(852);
            var opts = new DbfDataReaderOptions { Encoding = encoding };

            using var reader = new DbfDataReader.DbfDataReader(filepath, opts);
            var colNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                colNames.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                    row[colNames[i]] = val;
                }
                list.Add(row);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri čitanju DBF fajla '{filepath}': {ex.Message}");
        }

        return list;
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

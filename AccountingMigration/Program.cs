using System.Globalization;
using System.Text;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingMigration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=================================================");
        Console.WriteLine("🚀 AccountingSystem — Legacy DBF Import & Migration Tool");
        Console.WriteLine("=================================================");

        string radniPath = @"C:\KNJIGE\Radni";
        string kor01Path = Path.Combine(radniPath, "KOR01");

        if (!Directory.Exists(kor01Path))
        {
            Console.WriteLine($"❌ Folder nije pronađen: {kor01Path}");
            return;
        }

        string dbPath = Path.Combine(kor01Path, "accounting_kor01.db");
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
            Console.WriteLine($"🗑️ Stara SQLite baza obrisana: {dbPath}");
        }

        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using var db = new AccountingDbContext(options);
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ Inicijalizovana nova SQLite baza u KOR01!");

        // 1. Firma metadata
        var firma = new Firma
        {
            Sifra = "KOR01",
            Naziv = "ARHIBEL - 2026",
            Adresa = "Srpskih vladara 106",
            PttIMesto = "18300 Pirot",
            Telefon = "010 347-339",
            ZiroRacun = "160-11508-84",
            IsActive = true
        };
        db.Firme.Add(firma);

        // 2. Admin Korisnik
        var admin = new Korisnik
        {
            KorisnickoIme = "admin",
            LozinkaHash = "admin123",
            ImeIPrezime = "Administrator",
            Uloga = "Administrator"
        };
        db.Korisnici.Add(admin);
        await db.SaveChangesAsync();
        Console.WriteLine("🏢 Firma i korisnik kreirani!");

        // 3. Import Kontnog plana (KONTPLAN.DBF)
        string kontplanFile = Path.Combine(kor01Path, "KONTPLAN.DBF");
        if (File.Exists(kontplanFile))
        {
            Console.WriteLine("📋 Uvoz Kontnog plana (KONTPLAN.DBF)...");
            var kontaRows = ReadDbfRows(kontplanFile);
            int kontaCount = 0;

            foreach (var r in kontaRows)
            {
                string kBroj = GetVal(r, 0).Trim();
                string kNaziv = GetVal(r, 2).Trim();

                if (!string.IsNullOrWhiteSpace(kBroj) && !db.Konta.Any(k => k.BrojKonta == kBroj))
                {
                    int klasa = 0;
                    if (kBroj.Length > 0 && char.IsDigit(kBroj[0]))
                        klasa = kBroj[0] - '0';

                    db.Konta.Add(new Konto
                    {
                        BrojKonta = kBroj,
                        NazivKonta = string.IsNullOrWhiteSpace(kNaziv) ? $"Konto {kBroj}" : kNaziv,
                        Klasa = klasa,
                        IsSintetika = kBroj.Length <= 3
                    });
                    kontaCount++;
                }
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {kontaCount} konta!");
        }

        // 4. Import Partnera (ANKONT.DBF)
        string ankontFile = Path.Combine(kor01Path, "ANKONT.DBF");
        if (File.Exists(ankontFile))
        {
            Console.WriteLine("👥 Uvoz Partnera (ANKONT.DBF)...");
            var partnerRows = ReadDbfRows(ankontFile);
            int partneriCount = 0;

            foreach (var r in partnerRows)
            {
                string kBroj = GetVal(r, 0).Trim();
                string pNaziv = GetVal(r, 1).Trim();
                string adresa = GetVal(r, 2).Trim();
                string mesto = GetVal(r, 3).Trim();
                string ziro = GetVal(r, 4).Trim();
                string tel = GetVal(r, 5).Trim();

                if (!string.IsNullOrWhiteSpace(kBroj))
                {
                    db.Partneri.Add(new Partner
                    {
                        SifraPartnera = kBroj,
                        Naziv = string.IsNullOrWhiteSpace(pNaziv) ? $"Partner {kBroj}" : pNaziv,
                        Adresa = adresa,
                        PttIMesto = mesto,
                        ZiroRacun = ziro,
                        Telefon = tel,
                        KontoPartnera = kBroj
                    });
                    partneriCount++;
                }
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {partneriCount} partnera!");
        }

        // 5. Import Materijala / Artikala (M_SIFR.DBF)
        string msifrFile = Path.Combine(kor01Path, "M_SIFR.DBF");
        if (File.Exists(msifrFile))
        {
            Console.WriteLine("📦 Uvoz Šifarnika Materijala (M_SIFR.DBF)...");
            var matRows = ReadDbfRows(msifrFile);
            int artikalCount = 0;

            foreach (var r in matRows)
            {
                string sifra = GetVal(r, 0).Trim();
                string pak = GetVal(r, 1).Trim();
                string jm = GetVal(r, 2).Trim();
                string naziv = GetVal(r, 3).Trim();

                if (!string.IsNullOrWhiteSpace(sifra))
                {
                    db.Artikli.Add(new Artikal
                    {
                        SifraArtikla = sifra,
                        Naziv = string.IsNullOrWhiteSpace(naziv) ? $"Artikal {sifra}" : naziv,
                        JedinicaMere = string.IsNullOrWhiteSpace(jm) ? "kom" : jm,
                        Pakovanje = pak,
                        Vrsta = "Materijal"
                    });
                    artikalCount++;
                }
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {artikalCount} materijala/artikala!");
        }

        // 6. Import Magacina (MAGACIN.DBF)
        string magFile = Path.Combine(kor01Path, "MAGACIN.DBF");
        if (File.Exists(magFile))
        {
            Console.WriteLine("🏬 Uvoz Magacina (MAGACIN.DBF)...");
            var magRows = ReadDbfRows(magFile);
            int magCount = 0;

            foreach (var r in magRows)
            {
                string sifra = GetVal(r, 0).Trim();
                string naziv = GetVal(r, 1).Trim();

                if (!string.IsNullOrWhiteSpace(sifra))
                {
                    db.Magacini.Add(new Magacin
                    {
                        SifraMagacina = sifra,
                        NazivMagacina = string.IsNullOrWhiteSpace(naziv) ? $"Magacin {sifra}" : naziv,
                        VrstaMagacina = "Materijalno"
                    });
                    magCount++;
                }
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {magCount} magacina!");
        }

        // 7. Import Naloga i Stavki Knjiženja (NALOG.DBF)
        string nalogFile = Path.Combine(kor01Path, "NALOG.DBF");
        if (File.Exists(nalogFile))
        {
            Console.WriteLine("📖 Uvoz Naloga Knjiženja i Stavki (NALOG.DBF)...");
            var rows = ReadDbfRows(nalogFile);

            var naloziGroups = rows
                .Where(r => r.Count > 8 && !string.IsNullOrWhiteSpace(GetVal(r, 0)))
                .GroupBy(r => GetVal(r, 0).Trim());

            int nalogCount = 0;
            int stavkeCount = 0;

            foreach (var group in naloziGroups)
            {
                string brNaloga = group.Key;
                var firstRow = group.First();

                DateTime datNaloga = ParseDate(GetVal(firstRow, 8));
                string prviOpis = GetVal(firstRow, 5).Trim();

                var nalog = new Nalog
                {
                    BrojNaloga = brNaloga,
                    DatumNaloga = datNaloga,
                    VrstaNaloga = "Finansijski",
                    Opis = string.IsNullOrWhiteSpace(prviOpis) ? $"Nalog {brNaloga}" : prviOpis,
                    IsKnjizen = true,
                    DatumKnjiženja = datNaloga
                };

                int rBr = 1;
                foreach (var row in group)
                {
                    string kBroj = GetVal(row, 3).Trim();
                    string opisDok = GetVal(row, 5).Trim();
                    decimal dug = ParseDecimal(GetVal(row, 6));
                    decimal pot = ParseDecimal(GetVal(row, 7));

                    if (!string.IsNullOrWhiteSpace(kBroj) || dug > 0 || pot > 0)
                    {
                        var stavka = new StavkaNaloga
                        {
                            RedniBroj = rBr++,
                            BrojKonta = kBroj,
                            BrojDokumenta = opisDok,
                            Opis = opisDok,
                            Duguje = dug,
                            Potrazuje = pot
                        };
                        nalog.Stavke.Add(stavka);
                        stavkeCount++;
                    }
                }

                nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
                nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

                db.Nalozi.Add(nalog);
                nalogCount++;
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {nalogCount} naloga i {stavkeCount} stavki knjiženja!");
        }

        Console.WriteLine("\n=================================================");
        Console.WriteLine("✨ USPEŠNO ZAVRŠENA MIGRACIJA PODATAKA ZA KOR01!");
        Console.WriteLine($"📁 SQLite Baza: {dbPath}");
        Console.WriteLine("=================================================");
    }

    private static List<List<string>> ReadDbfRows(string dbfPath)
    {
        var result = new List<List<string>>();
        using var fs = new FileStream(dbfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(fs);

        byte[] header = reader.ReadBytes(32);
        if (header.Length < 32) return result;

        int numRecords = BitConverter.ToInt32(header, 4);
        short headerLen = BitConverter.ToInt16(header, 8);
        short recordLen = BitConverter.ToInt16(header, 10);

        int numFields = (headerLen - 33) / 32;
        var fields = new List<(string name, char type, byte len, byte dec)>();

        for (int i = 0; i < numFields; i++)
        {
            byte[] fBytes = reader.ReadBytes(32);
            if (fBytes.Length < 32) break;

            string name = Encoding.ASCII.GetString(fBytes, 0, 11).Replace("\0", "").Trim();
            char type = (char)fBytes[11];
            byte len = fBytes[16];
            byte dec = fBytes[17];
            fields.Add((name, type, len, dec));
        }

        fs.Seek(headerLen, SeekOrigin.Begin);
        var latin1 = Encoding.GetEncoding("latin1");

        for (int r = 0; r < numRecords; r++)
        {
            byte[] recordBytes = reader.ReadBytes(recordLen);
            if (recordBytes.Length < recordLen) break;

            if (recordBytes[0] == 0x2A) continue;

            var rowVals = new List<string>();
            int pos = 1;

            foreach (var f in fields)
            {
                if (pos + f.len > recordBytes.Length) break;
                string val = latin1.GetString(recordBytes, pos, f.len).Trim();
                rowVals.Add(val);
                pos += f.len;
            }

            result.Add(rowVals);
        }

        return result;
    }

    private static string GetVal(List<string> row, int index)
    {
        if (index >= 0 && index < row.Count) return row[index];
        return string.Empty;
    }

    private static decimal ParseDecimal(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return 0m;
        if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
            return val;
        return 0m;
    }

    private static DateTime ParseDate(string str)
    {
        if (str.Length == 8 && DateTime.TryParseExact(str, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            return dt;
        return DateTime.Now;
    }
}

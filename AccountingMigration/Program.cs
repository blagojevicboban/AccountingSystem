using System.Globalization;
using System.Text;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;

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

        using var db = AccountingDbContext.Create(dbPath);
        Console.WriteLine("✅ Inicijalizovana nova SQLite baza u KOR01 (EF Core migracije)!");

        // 1. Firma metadata (admin korisnik je već zaseden EF migracijom — admin/admin123)
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
        await db.SaveChangesAsync();
        Console.WriteLine("🏢 Firma kreirana!");

        // 3. Import Kontnog plana (KONTPLAN.DBF)
        string kontplanFile = Path.Combine(kor01Path, "KONTPLAN.DBF");
        if (File.Exists(kontplanFile))
        {
            Console.WriteLine("📋 Uvoz Kontnog plana (KONTPLAN.DBF)...");
            var kontaRows = DbfImportService.ReadRows(kontplanFile);
            var existingKonta = db.Konta.Select(k => k.BrojKonta).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int kontaCount = 0;

            foreach (var r in kontaRows)
            {
                var konto = DbfImportService.MapKonto(r);
                if (konto != null && existingKonta.Add(konto.BrojKonta))
                {
                    db.Konta.Add(konto);
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
            var partnerRows = DbfImportService.ReadRows(ankontFile);
            var existingPartneri = db.Partneri.Select(p => p.SifraPartnera).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int partneriCount = 0;

            foreach (var r in partnerRows)
            {
                var partner = DbfImportService.MapPartner(r, partneriCount + 1);
                if (partner != null && existingPartneri.Add(partner.SifraPartnera))
                {
                    db.Partneri.Add(partner);
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
            var magRows = DbfImportService.ReadRows(magFile);
            var existingMagacini = db.Magacini.Select(m => m.SifraMagacina).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int magCount = 0;

            foreach (var r in magRows)
            {
                var magacin = DbfImportService.MapMagacin(r);
                if (magacin != null && existingMagacini.Add(magacin.SifraMagacina))
                {
                    magacin.VrstaMagacina = "Materijalno";
                    db.Magacini.Add(magacin);
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
            var rows = DbfImportService.ReadRows(nalogFile);
            var naloziGroups = DbfImportService.GroupNalogRows(rows);

            int nalogCount = 0;
            int stavkeCount = 0;

            foreach (var (brNaloga, redovi) in naloziGroups)
            {
                var nalog = DbfImportService.MapNalogGrupa(brNaloga, redovi);
                if (nalog == null) continue;

                db.Nalozi.Add(nalog);
                nalogCount++;
                stavkeCount += nalog.Stavke.Count;
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {nalogCount} naloga i {stavkeCount} stavki knjiženja!");
        }

        // 8. Import Materijalnih kartica (M_KART.DBF) — prosečna cena po magacinu/artiklu
        string mKartFile = Path.Combine(kor01Path, "M_KART.DBF");
        if (File.Exists(mKartFile))
        {
            Console.WriteLine("🗂️ Uvoz Materijalnih kartica (M_KART.DBF)...");
            var rows = ReadDbfRows(mKartFile);
            int count = 0;

            foreach (var r in rows)
            {
                string mag = GetVal(r, 0).Trim();
                string art = GetVal(r, 1).Trim();
                if (string.IsNullOrWhiteSpace(mag) || string.IsNullOrWhiteSpace(art)) continue;

                db.MaterijalneKartice.Add(new MaterijalnaKartica
                {
                    SifraMagacina = mag,
                    SifraArtikla = art,
                    RedniBroj = (int)ParseDecimal(GetVal(r, 2)),
                    DatumPromene = ParseDate(GetVal(r, 3)),
                    OpisPromene = GetVal(r, 4).Trim(),
                    Ulaz = ParseDecimal(GetVal(r, 5)),
                    Izlaz = ParseDecimal(GetVal(r, 6)),
                    Stanje = ParseDecimal(GetVal(r, 7)),
                    Cena = ParseDecimal(GetVal(r, 8)),
                    CenaIzlaz = ParseDecimal(GetVal(r, 9)),
                    Duguje = ParseDecimal(GetVal(r, 10)),
                    Potrazuje = ParseDecimal(GetVal(r, 11)),
                    Saldo = ParseDecimal(GetVal(r, 12))
                });
                count++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {count} stavki materijalnih kartica!");
        }

        // 9. Import Ulaza materijala (ULAZ.DBF)
        string ulazFile = Path.Combine(kor01Path, "ULAZ.DBF");
        if (File.Exists(ulazFile))
        {
            Console.WriteLine("📥 Uvoz Ulaza materijala (ULAZ.DBF)...");
            var rows = ReadDbfRows(ulazFile);
            var groups = rows.Where(r => !string.IsNullOrWhiteSpace(GetVal(r, 0))).GroupBy(r => GetVal(r, 0).Trim());
            int nalogCount = 0, stavkeCount = 0;

            foreach (var group in groups)
            {
                var first = group.First();
                var nalog = new UlazNalog
                {
                    BrojNaloga = group.Key,
                    Datum = ParseDate(GetVal(first, 1)),
                    SifraMagacina = GetVal(first, 2).Trim(),
                    BrojRacuna = GetVal(first, 8).Trim(),
                    DatumRacuna = ParseDate(GetVal(first, 9)),
                    IsKnjizen = GetVal(first, 10).Trim() == "1"
                };

                int rBr = 1;
                foreach (var row in group)
                {
                    string art = GetVal(row, 4).Trim();
                    if (string.IsNullOrWhiteSpace(art)) continue;

                    nalog.Stavke.Add(new UlazStavka
                    {
                        RedniBroj = rBr++,
                        SifraArtikla = art,
                        Kolicina = ParseDecimal(GetVal(row, 5)),
                        Cena = ParseDecimal(GetVal(row, 6)),
                        Iznos = ParseDecimal(GetVal(row, 7))
                    });
                    stavkeCount++;
                }

                db.UlazNalozi.Add(nalog);
                nalogCount++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {nalogCount} ulaznih naloga i {stavkeCount} stavki!");
        }

        // 10. Import Trebovanja (TREBOV.DBF)
        string trebovFile = Path.Combine(kor01Path, "TREBOV.DBF");
        if (File.Exists(trebovFile))
        {
            Console.WriteLine("📤 Uvoz Trebovanja (TREBOV.DBF)...");
            var rows = ReadDbfRows(trebovFile);
            var groups = rows.Where(r => !string.IsNullOrWhiteSpace(GetVal(r, 0))).GroupBy(r => GetVal(r, 0).Trim());
            int nalogCount = 0, stavkeCount = 0;

            foreach (var group in groups)
            {
                var first = group.First();
                var nalog = new TrebovanjeNalog
                {
                    BrojNaloga = group.Key,
                    Datum = ParseDate(GetVal(first, 1)),
                    SifraMagacina = GetVal(first, 2).Trim(),
                    IsKnjizen = GetVal(first, 8).Trim() == "1"
                };

                int rBr = 1;
                foreach (var row in group)
                {
                    string art = GetVal(row, 4).Trim();
                    if (string.IsNullOrWhiteSpace(art)) continue;

                    nalog.Stavke.Add(new TrebovanjeStavka
                    {
                        RedniBroj = rBr++,
                        SifraArtikla = art,
                        Kolicina = ParseDecimal(GetVal(row, 5)),
                        Cena = ParseDecimal(GetVal(row, 6)),
                        Iznos = ParseDecimal(GetVal(row, 7)),
                        KontoTroska = GetVal(row, 9).Trim()
                    });
                    stavkeCount++;
                }

                db.TrebovanjeNalozi.Add(nalog);
                nalogCount++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {nalogCount} naloga trebovanja i {stavkeCount} stavki!");
        }

        // 11. Import Primopredaja (M_PRIMO.DBF)
        string mPrimoFile = Path.Combine(kor01Path, "M_PRIMO.DBF");
        if (File.Exists(mPrimoFile))
        {
            Console.WriteLine("🔄 Uvoz Primopredaja (M_PRIMO.DBF)...");
            var rows = ReadDbfRows(mPrimoFile);
            var groups = rows.Where(r => !string.IsNullOrWhiteSpace(GetVal(r, 0))).GroupBy(r => GetVal(r, 0).Trim());
            int nalogCount = 0, stavkeCount = 0;

            foreach (var group in groups)
            {
                var first = group.First();
                var nalog = new PrimopredajaNalog
                {
                    BrojNaloga = group.Key,
                    Datum = ParseDate(GetVal(first, 1)),
                    SifraMagacinaDaje = GetVal(first, 2).Trim(),
                    SifraMagacinaPrima = GetVal(first, 3).Trim(),
                    IsKnjizen = GetVal(first, 9).Trim() == "1"
                };

                int rBr = 1;
                foreach (var row in group)
                {
                    string art = GetVal(row, 5).Trim();
                    if (string.IsNullOrWhiteSpace(art)) continue;

                    nalog.Stavke.Add(new PrimopredajaStavka
                    {
                        RedniBroj = rBr++,
                        SifraArtikla = art,
                        Kolicina = ParseDecimal(GetVal(row, 6)),
                        Cena = ParseDecimal(GetVal(row, 7)),
                        Iznos = ParseDecimal(GetVal(row, 8))
                    });
                    stavkeCount++;
                }

                db.PrimopredajaNalozi.Add(nalog);
                nalogCount++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {nalogCount} naloga primopredaje i {stavkeCount} stavki!");
        }

        // 12. Import Kalkulacija veleprodaje (KALKULAC.DBF)
        string kalkulacFile = Path.Combine(kor01Path, "KALKULAC.DBF");
        if (File.Exists(kalkulacFile))
        {
            Console.WriteLine("🧮 Uvoz Kalkulacija veleprodaje (KALKULAC.DBF)...");
            var rows = ReadDbfRows(kalkulacFile);
            int count = 0;

            foreach (var r in rows)
            {
                string brKalkul = GetVal(r, 0).Trim();
                if (string.IsNullOrWhiteSpace(brKalkul)) continue;

                db.Kalkulacije.Add(new Kalkulacija
                {
                    BrojKalkulacije = brKalkul,
                    Datum = ParseDate(GetVal(r, 1)),
                    SifraDobavljaca = GetVal(r, 2).Trim(),
                    BrojOtpremnice = GetVal(r, 3).Trim(),
                    DatumOtpremnice = ParseDate(GetVal(r, 4)),
                    BrojRacuna = GetVal(r, 5).Trim(),
                    DatumRacuna = ParseDate(GetVal(r, 6)),
                    NabavnaVrednost = ParseDecimal(GetVal(r, 7)),
                    TransportniTroskovi = ParseDecimal(GetVal(r, 8)),
                    TroskoviUskladistenja = ParseDecimal(GetVal(r, 9)),
                    UtovarIstovar = ParseDecimal(GetVal(r, 10)),
                    TransportnoOsiguranje = ParseDecimal(GetVal(r, 11)),
                    OstaliTroskovi = ParseDecimal(GetVal(r, 12)),
                    SvegaTroskovi = ParseDecimal(GetVal(r, 13)),
                    SvegaNabavno = ParseDecimal(GetVal(r, 14)),
                    Razlika = ParseDecimal(GetVal(r, 15)),
                    Porez = ParseDecimal(GetVal(r, 16)),
                    ProdajnaVrednost = ParseDecimal(GetVal(r, 17)),
                    SifraMagacina = GetVal(r, 18).Trim(),
                    IsKnjizen = GetVal(r, 19).Trim() == "1"
                });
                count++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {count} kalkulacija veleprodaje!");
        }

        // 13. Import Kalkulacija maloprodaje (MALKULAC.DBF)
        string malkulacFile = Path.Combine(kor01Path, "MALKULAC.DBF");
        if (File.Exists(malkulacFile))
        {
            Console.WriteLine("🧮 Uvoz Kalkulacija maloprodaje (MALKULAC.DBF)...");
            var rows = ReadDbfRows(malkulacFile);
            int count = 0;

            foreach (var r in rows)
            {
                string brKalkul = GetVal(r, 1).Trim();
                if (string.IsNullOrWhiteSpace(brKalkul)) continue;

                db.MaloprodajneKalkulacije.Add(new MaloprodajnaKalkulacija
                {
                    SifraProdavnice = (int)ParseDecimal(GetVal(r, 0)),
                    BrojKalkulacije = brKalkul,
                    Datum = ParseDate(GetVal(r, 2)),
                    SifraMagacinaPrima = GetVal(r, 3).Trim(),
                    SifraMagacinaDaje = GetVal(r, 4).Trim(),
                    SifraDobavljaca = GetVal(r, 5).Trim(),
                    BrojOtpremnice = GetVal(r, 6).Trim(),
                    DatumOtpremnice = ParseDate(GetVal(r, 7)),
                    BrojRacuna = GetVal(r, 8).Trim(),
                    DatumRacuna = ParseDate(GetVal(r, 9)),
                    TransportniTroskovi = ParseDecimal(GetVal(r, 10)),
                    TroskoviUskladistenja = ParseDecimal(GetVal(r, 11)),
                    UtovarIstovar = ParseDecimal(GetVal(r, 12)),
                    TransportnoOsiguranje = ParseDecimal(GetVal(r, 13)),
                    OstaliTroskovi = ParseDecimal(GetVal(r, 14)),
                    IsKnjizen = GetVal(r, 15).Trim() == "1",
                    IsTrgovinskiKnjizen = GetVal(r, 16).Trim() == "1",
                    SvegaTroskovi = ParseDecimal(GetVal(r, 17)),
                    RabatPri = ParseDecimal(GetVal(r, 18)),
                    NabavnaVrednost = ParseDecimal(GetVal(r, 19)),
                    SvegaNabavno = ParseDecimal(GetVal(r, 20)),
                    Razlika = ParseDecimal(GetVal(r, 21)),
                    Porez = ParseDecimal(GetVal(r, 22)),
                    ProdajnaVrednost = ParseDecimal(GetVal(r, 23)),
                    RabatIznos = ParseDecimal(GetVal(r, 24))
                });
                count++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {count} kalkulacija maloprodaje!");
        }

        // 14. Import Kartica konta (KARTICA.DBF) — legacy snapshot za poređenje
        string karticaFile = Path.Combine(kor01Path, "KARTICA.DBF");
        if (File.Exists(karticaFile))
        {
            Console.WriteLine("📇 Uvoz Kartica konta (KARTICA.DBF)...");
            var rows = ReadDbfRows(karticaFile);
            int count = 0;

            foreach (var r in rows)
            {
                string konto = GetVal(r, 1).Trim();
                if (string.IsNullOrWhiteSpace(konto)) continue;

                db.KarticeKonta.Add(new KarticaKonta
                {
                    RedniBroj = (int)ParseDecimal(GetVal(r, 0)),
                    BrojKonta = konto,
                    DatumNaloga = ParseDate(GetVal(r, 2)),
                    BrojNaloga = GetVal(r, 3).Trim(),
                    OpisPromeneKod = GetVal(r, 4).Trim(),
                    BrojDokumenta = GetVal(r, 5).Trim(),
                    TekuceDuguje = ParseDecimal(GetVal(r, 6)),
                    TekucePotrazuje = ParseDecimal(GetVal(r, 7)),
                    UkupnoDuguje = ParseDecimal(GetVal(r, 8)),
                    UkupnoPotrazuje = ParseDecimal(GetVal(r, 9)),
                    Saldo = ParseDecimal(GetVal(r, 10))
                });
                count++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {count} stavki kartica konta!");
        }

        // 15. Import Kamatnih stopa (KAM_STOP.DBF) — istorijske stope, NAPOMENA: zastarele
        // (poslednja iz legacy baze je iz 2006. godine) — treba dopuniti aktuelnim stopama
        // pre stvarnog obračuna kamate.
        string kamStopFile = Path.Combine(kor01Path, "KAM_STOP.DBF");
        if (File.Exists(kamStopFile))
        {
            Console.WriteLine("💰 Uvoz Kamatnih stopa (KAM_STOP.DBF)...");
            var rows = ReadDbfRows(kamStopFile);
            int count = 0;

            foreach (var r in rows)
            {
                string datum = GetVal(r, 0).Trim();
                if (string.IsNullOrWhiteSpace(datum)) continue;

                db.KamatneStope.Add(new KamatnaStopa
                {
                    DatumOd = ParseDate(datum),
                    GodisnjaStopaProcenat = ParseDecimal(GetVal(r, 1)),
                    Napomena = "Uvezeno iz legacy KAM_STOP.DBF"
                });
                count++;
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {count} kamatnih stopa (napomena: istorijske, proverite aktuelnost)!");
        }

        // 16. Import šifarnika opisa promena (PROMENE.DBF) — razlikuje se po firmi, nije deljen rečnik
        string promeneFile = Path.Combine(kor01Path, "PROMENE.DBF");
        if (File.Exists(promeneFile))
        {
            Console.WriteLine("🏷️ Uvoz šifarnika opisa promena (PROMENE.DBF)...");
            var rows = DbfImportService.ReadRows(promeneFile);
            var existingPromene = db.Promene.Select(p => p.Sifra).ToHashSet();
            int count = 0;

            foreach (var r in rows)
            {
                var promena = DbfImportService.MapPromena(r);
                if (promena != null && existingPromene.Add(promena.Sifra))
                {
                    db.Promene.Add(promena);
                    count++;
                }
            }
            await db.SaveChangesAsync();
            Console.WriteLine($"   --> Uvezeno {count} šifara promena!");
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

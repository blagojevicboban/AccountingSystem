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
using Microsoft.EntityFrameworkCore;

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
                var existingTarife = firmDb.PoreskeTarife.Select(t => t.TarifniBroj).ToHashSet(StringComparer.OrdinalIgnoreCase);

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
                if (!File.Exists(artikliFile)) artikliFile = Path.Combine(firmaDto.FolderPath, "M_SIFR.DBF");

                if (File.Exists(artikliFile))
                {
                    Report(progress, firmaDto.Naziv, "Artikli", basePercent + 40, $"🛒 Uvoz Artikala ({Path.GetFileName(artikliFile)})...");
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

                // 4b. Poreske tarife (TARIFE.DBF)
                var tarifeFile = Path.Combine(firmaDto.FolderPath, "TARIFE.DBF");
                if (File.Exists(tarifeFile))
                {
                    Report(progress, firmaDto.Naziv, "Poreske tarife", basePercent + 56, "🧾 Uvoz Poreskih tarifa (TARIFE.DBF)...");
                    var rows = DbfImportService.ReadRows(tarifeFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var tarifa = DbfImportService.MapPoreskaTarifa(r);
                        if (tarifa != null && existingTarife.Add(tarifa.TarifniBroj))
                        {
                            firmDb.PoreskeTarife.Add(tarifa);
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Poreske tarife", basePercent + 57, $"   --> Uvezeno {count} poreskih tarifa!");
                }

                // 5. Šifarnik opisa promena (PROMENE.DBF) — razlikuje se po firmi, nije deljen rečnik
                var promeneMap = new Dictionary<int, string>();
                var promeneFile = Path.Combine(firmaDto.FolderPath, "PROMENE.DBF");
                if (File.Exists(promeneFile))
                {
                    Report(progress, firmaDto.Naziv, "Šifarnik promena", basePercent + 58, "🏷️ Uvoz šifarnika opisa promena (PROMENE.DBF)...");
                    var rows = DbfImportService.ReadRows(promeneFile);
                    int count = 0;
                    foreach (var r in rows)
                    {
                        var promena = DbfImportService.MapPromena(r);
                        if (promena != null && existingPromene.Add(promena.Sifra))
                        {
                            firmDb.Promene.Add(promena);
                            promeneMap[promena.Sifra] = promena.Opis;
                            count++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Šifarnik promena", basePercent + 60, $"   --> Uvezeno {count} opisa promena!");
                }

                // Ako u bazi već postoje promene od ranije, učitaj ih u promeneMap
                var svePromene = await firmDb.Promene.ToListAsync();
                foreach (var p in svePromene) promeneMap[p.Sifra] = p.Opis;

                // 6. Nalozi za knjiženje (NALOG.DBF)
                var nalogFile = Path.Combine(firmaDto.FolderPath, "NALOG.DBF");

                if (File.Exists(nalogFile))
                {
                    Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 65, "📖 Uvoz Naloga za knjiženje i stavki (NALOG.DBF)...");
                    var rows = DbfImportService.ReadRows(nalogFile);
                    var groups = DbfImportService.GroupNalogRows(rows);

                    int nalogiCount = 0;
                    int stavkeCount = 0;

                    foreach (var (bNaloga, redovi) in groups)
                    {
                        if (!existingNalogi.Add(bNaloga)) continue;

                        var nalog = DbfImportService.MapNalogGrupa(bNaloga, redovi, promeneMap);
                        if (nalog == null) continue;

                        firmDb.Nalozi.Add(nalog);
                        nalogiCount++;
                        stavkeCount += nalog.Stavke.Count;
                    }

                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Nalozi", basePercent + 80, $"   --> Uvezeno {nalogiCount} naloga i {stavkeCount} stavki knjiženja u zasebnu bazu!");
                }

                // 7. Materijalne / Robne kartice (MAT_KART.DBF / M_KART.DBF)
                var karticaFile = Path.Combine(firmaDto.FolderPath, "MAT_KART.DBF");
                if (!File.Exists(karticaFile)) karticaFile = Path.Combine(firmaDto.FolderPath, "M_KART.DBF");

                if (File.Exists(karticaFile))
                {
                    Report(progress, firmaDto.Naziv, "Materijalne kartice", basePercent + 85, $"📊 Uvoz Materijalnih/Robnih kartica ({Path.GetFileName(karticaFile)})...");
                    var rows = DbfImportService.ReadRows(karticaFile);
                    int karticeCount = 0;
                    int rBr = 1;
                    foreach (var r in rows)
                    {
                        var mk = DbfImportService.MapMaterijalnaKartica(r, rBr++);
                        if (mk != null)
                        {
                            firmDb.MaterijalneKartice.Add(mk);
                            karticeCount++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Materijalne kartice", basePercent + 90, $"   --> Uvezeno {karticeCount} stavki robnih/materijalnih kartica!");
                }

                // 8. Kalkulacije veleprodaje (KALKULAC.DBF i KAL_NAL.DBF)
                var kalkulacFile = Path.Combine(firmaDto.FolderPath, "KALKULAC.DBF");
                var kalNalFile = Path.Combine(firmaDto.FolderPath, "KAL_NAL.DBF");
                if (File.Exists(kalkulacFile))
                {
                    Report(progress, firmaDto.Naziv, "Kalkulacije", basePercent + 92, "🧮 Uvoz Kalkulacija (KALKULAC.DBF & KAL_NAL.DBF)...");
                    var kalkRows = DbfImportService.ReadRows(kalkulacFile);
                    var stavkeRows = File.Exists(kalNalFile) ? DbfImportService.ReadRows(kalNalFile) : new List<Dictionary<string, string>>();
                    var stavkeGrouped = DbfImportService.GroupKalkulacijaStavke(stavkeRows);

                    int kalkCount = 0;
                    int totalStavke = 0;
                    foreach (var r in kalkRows)
                    {
                        var kalk = DbfImportService.MapKalkulacija(r);
                        if (kalk != null)
                        {
                            if (stavkeGrouped.TryGetValue(kalk.BrojKalkulacije, out var redoviStavki))
                            {
                                int rBr = 1;
                                foreach (var sRow in redoviStavki)
                                {
                                    var st = DbfImportService.MapKalkulacijaStavka(sRow, rBr++);
                                    if (st != null)
                                    {
                                        kalk.Stavke.Add(st);
                                        totalStavke++;
                                    }
                                }
                            }
                            firmDb.Kalkulacije.Add(kalk);
                            kalkCount++;
                        }
                    }
                    await firmDb.SaveChangesAsync();
                    Report(progress, firmaDto.Naziv, "Kalkulacije", basePercent + 97, $"   --> Uvezeno {kalkCount} kalkulacija sa {totalStavke} stavki!");
                }

                // 9. Uvoz Naloga za primopredaju (MAT_NAL.DBF)
                // 9. Uvoz Naloga za primopredaju, zaduženja i razduženja (MAT_NAL.DBF, ZADUZ.DBF, RAZDUZ.DBF)
                var svePrimopredaje = new List<PrimopredajaNalog>();
                string matNalPath = Path.Combine(firmaDto.FolderPath, "MAT_NAL.DBF");
                if (File.Exists(matNalPath))
                {
                    svePrimopredaje.AddRange(DbfImportService.MapPrimopredajaNalozi(DbfImportService.ReadRows(matNalPath), "Primopredaja"));
                }
                string zaduzPath = Path.Combine(firmaDto.FolderPath, "ZADUZ.DBF");
                if (File.Exists(zaduzPath))
                {
                    svePrimopredaje.AddRange(DbfImportService.MapPrimopredajaNalozi(DbfImportService.ReadRows(zaduzPath), "Zaduženje"));
                }
                string razduzPath = Path.Combine(firmaDto.FolderPath, "RAZDUZ.DBF");
                if (File.Exists(razduzPath))
                {
                    svePrimopredaje.AddRange(DbfImportService.MapPrimopredajaNalozi(DbfImportService.ReadRows(razduzPath), "Razduženje"));
                }
                if (svePrimopredaje.Count > 0)
                {
                    firmDb.PrimopredajaNalozi.AddRange(svePrimopredaje);
                    await firmDb.SaveChangesAsync();
                    int totStavke = svePrimopredaje.Sum(n => n.Stavke.Count);
                    Report(progress, firmaDto.Naziv, "Primopredaje", basePercent + 99, $"   --> Uvezeno {svePrimopredaje.Count} naloga (primopredaje/zaduženja/razduženja) sa {totStavke} stavki!");
                }

                // 10. Uvoz Računa - Otpremnica (RAC_OTP.DBF & RAC_POD.DBF)
                string racOtpPath = Path.Combine(firmaDto.FolderPath, "RAC_OTP.DBF");
                string racPodPath = Path.Combine(firmaDto.FolderPath, "RAC_POD.DBF");
                if (File.Exists(racOtpPath))
                {
                    Report(progress, firmaDto.Naziv, "Računi-Otpremnice", basePercent + 99, "📜 Uvoz Računa-Otpremnica (RAC_OTP.DBF & RAC_POD.DBF)...");
                    var racOtpRows = DbfImportService.ReadRows(racOtpPath);
                    var racPodRows = File.Exists(racPodPath) ? DbfImportService.ReadRows(racPodPath) : new List<Dictionary<string, string>>();
                    var magaciniMap = await firmDb.Magacini.ToDictionaryAsync(m => m.SifraMagacina, m => m.MagacinId, StringComparer.OrdinalIgnoreCase);
                    var artikliMap = await firmDb.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a.ArtikalId, StringComparer.OrdinalIgnoreCase);
                    var racuni = DbfImportService.MapRacunOtpremnice(racOtpRows, racPodRows, magaciniMap, artikliMap);
                    if (racuni.Count > 0)
                    {
                        firmDb.RacuniOtpremnice.AddRange(racuni);
                        await firmDb.SaveChangesAsync();
                        int totStavke = racuni.Sum(r => r.Stavke.Count);
                        Report(progress, firmaDto.Naziv, "Računi-Otpremnice", basePercent + 99, $"   --> Uvezeno {racuni.Count} računa-otpremnica sa {totStavke} stavki!");
                    }
                }

                // 11. Uvoz Nivelacija cena (NIV_NAL.DBF & P_M_NIV.DBF)
                string nivNalPath = Path.Combine(firmaDto.FolderPath, "NIV_NAL.DBF");
                string pmNivPath = Path.Combine(firmaDto.FolderPath, "P_M_NIV.DBF");
                if (File.Exists(nivNalPath) || File.Exists(pmNivPath))
                {
                    Report(progress, firmaDto.Naziv, "Nivelacije cena", basePercent + 100, "🏷️ Uvoz Nivelacija cena (NIV_NAL.DBF & P_M_NIV.DBF)...");
                    var nivNalRows = File.Exists(nivNalPath) ? DbfImportService.ReadRows(nivNalPath) : new List<Dictionary<string, string>>();
                    var pmNivRows = File.Exists(pmNivPath) ? DbfImportService.ReadRows(pmNivPath) : new List<Dictionary<string, string>>();
                    var magaciniMap = await firmDb.Magacini.ToDictionaryAsync(m => m.SifraMagacina, m => m.MagacinId, StringComparer.OrdinalIgnoreCase);
                    var artikliMap = await firmDb.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a.ArtikalId, StringComparer.OrdinalIgnoreCase);
                    var nivelacije = DbfImportService.MapNivelacijeCena(nivNalRows, pmNivRows, magaciniMap, artikliMap);
                    if (nivelacije.Count > 0)
                    {
                        firmDb.NivelacijeCena.AddRange(nivelacije);
                        await firmDb.SaveChangesAsync();
                        int totStavke = nivelacije.Sum(n => n.Stavke.Count);
                        Report(progress, firmaDto.Naziv, "Nivelacije cena", basePercent + 100, $"   --> Uvezeno {nivelacije.Count} nivelacija cena sa {totStavke} stavki!");
                    }
                }

                // Postavi novouvezenu bazu kao aktivnu u AppConfig
                AppConfig.DbPath = firmaDbPath;

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class PutniNalogService
{
    private readonly AccountingDbContext _db;

    public PutniNalogService(AccountingDbContext db)
    {
        _db = db;
    }

    public static (double TrajanjeSati, decimal BrojDnevnica, decimal UkupnoDnevnice) IzracunajDnevnice(
        DateTime polazak, DateTime povratak, decimal iznosDnevniceRsd)
    {
        if (povratak <= polazak) return (0, 0, 0);

        TimeSpan ts = povratak - polazak;
        double ukupnoSati = ts.TotalHours;

        int celiDani = (int)(ukupnoSati / 24);
        double preostaliSati = ukupnoSati % 24;

        decimal dodatnaDnevnica = 0.0m;
        if (preostaliSati >= 12)
        {
            dodatnaDnevnica = 1.0m;
        }
        else if (preostaliSati >= 8)
        {
            dodatnaDnevnica = 0.5m;
        }

        decimal brojDnevnica = celiDani + dodatnaDnevnica;
        decimal ukupnoDnevnice = brojDnevnica * iznosDnevniceRsd;

        return (ukupnoSati, brojDnevnica, ukupnoDnevnice);
    }

    public async Task<List<PutniNalog>> GetPutniNaloziAsync()
    {
        return await _db.PutniNalozi
            .Include(p => p.StavkeTroskova)
            .OrderByDescending(p => p.DatumPolaska)
            .ThenByDescending(p => p.PutniNalogId)
            .ToListAsync();
    }

    public async Task<PutniNalog?> GetPutniNalogByIdAsync(int id)
    {
        return await _db.PutniNalozi
            .Include(p => p.StavkeTroskova)
            .FirstOrDefaultAsync(p => p.PutniNalogId == id);
    }

    public async Task<PutniNalog> SacuvajPutniNalogAsync(PutniNalog pn)
    {
        var (sati, dnevnice, ukupnoDnevnice) = IzracunajDnevnice(pn.DatumPolaska, pn.DatumPovratka, pn.IznosDnevniceRsd);
        pn.TrajanjeSati = sati;
        pn.BrojDnevnica = dnevnice;
        pn.UkupnoDnevnice = ukupnoDnevnice;

        pn.TroskoviGoriva = pn.StavkeTroskova.Where(s => s.VrstaTroska == "Gorivo").Sum(s => s.Iznos);
        pn.TroskoviSmestaja = pn.StavkeTroskova.Where(s => s.VrstaTroska == "Smeštaj").Sum(s => s.Iznos);
        pn.TroskoviPrevoza = pn.StavkeTroskova.Where(s => s.VrstaTroska == "Prevoz" || s.VrstaTroska == "Putarina").Sum(s => s.Iznos);
        pn.OstaliTroskovi = pn.StavkeTroskova.Where(s => s.VrstaTroska == "Ostalo" || s.VrstaTroska == "Taksiji").Sum(s => s.Iznos);

        decimal ukupniTroskovi = pn.UkupnoDnevnice + pn.TroskoviGoriva + pn.TroskoviSmestaja + pn.TroskoviPrevoza + pn.OstaliTroskovi;
        pn.UkupnoZaIsplatu = Math.Max(0, ukupniTroskovi - pn.Akontacija);

        if (pn.PutniNalogId == 0)
        {
            if (string.IsNullOrWhiteSpace(pn.BrojNaloga))
            {
                int sledeciBroj = await _db.PutniNalozi.CountAsync() + 1;
                string prefiks = pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? "PNI" : "PNZ";
                pn.BrojNaloga = $"{prefiks}-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.PutniNalozi.Add(pn);
        }
        else
        {
            var postojeceStavke = _db.PutniNaloziTroskoviStavke.Where(s => s.PutniNalogId == pn.PutniNalogId);
            _db.PutniNaloziTroskoviStavke.RemoveRange(postojeceStavke);

            _db.PutniNalozi.Update(pn);
        }

        await _db.SaveChangesAsync();
        return pn;
    }

    public async Task<bool> ObrisiPutniNalogAsync(int id)
    {
        var pn = await _db.PutniNalozi.FindAsync(id);
        if (pn == null || pn.IsKnjizeno) return false;

        _db.PutniNalozi.Remove(pn);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Knjiženje putnog naloga na Konto 5330 (Zemlja) ili Konto 5340 (Inostranstvo) u Glavnoj Knjizi
    /// </summary>
    public async Task<(bool Success, string Message, int? NalogId)> KnjiziPutniNalogAsync(int putniNalogId)
    {
        var pn = await GetPutniNalogByIdAsync(putniNalogId);
        if (pn == null) return (false, "Putni nalog ne postoji.", null);

        if (pn.IsKnjizeno)
        {
            return (false, "Putni nalog je već proknjižen.", pn.NalogId);
        }

        decimal ukupniTroskovi = pn.UkupnoDnevnice + pn.TroskoviGoriva + pn.TroskoviSmestaja + pn.TroskoviPrevoza + pn.OstaliTroskovi;
        if (ukupniTroskovi <= 0)
        {
            return (false, "Ukupni troškovi putnog naloga moraju biti veći od 0 za knjiženje.", null);
        }

        string brojKontaTroska = pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? "5340" : "5330";
        string nazivKontaStr = pn.Vrsta == VrstaSlužbenogPutovanja.Inostranstvo ? "Troškovi službenog puta u inostranstvu" : "Troškovi službenog puta u zemlji";

        int sledeciBrojNaloga = await _db.Nalozi.CountAsync() + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            VrstaNaloga = "PN",
            DatumNaloga = pn.DatumPovratka,
            Opis = $"Obračun putnog naloga br. {pn.BrojNaloga} za radnika {pn.ZaposleniIme} ({pn.Relacija})",
            IsKnjizen = true,
            UkupnoDuguje = ukupniTroskovi,
            UkupnoPotrazuje = ukupniTroskovi
        };

        // 1. Duguje Konto 5330 ili 5340 (Trošak službenog putovanja)
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 1,
            BrojKonta = brojKontaTroska,
            Opis = $"{nazivKontaStr} po naloga br. {pn.BrojNaloga} ({pn.ZaposleniIme})",
            Duguje = ukupniTroskovi,
            Potrazuje = 0m,
            BrojDokumenta = pn.BrojNaloga,
            DatumDokumenta = pn.DatumPovratka
        });

        // 2. Potražuje Konto 4650 / 2410 (Obaveza / Isplata iz blagajne ili tekućeg računa)
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 2,
            BrojKonta = "4650", // Obaveze prema zaposlenima za službena putovanja
            Opis = $"Obračunati putni nalog br. {pn.BrojNaloga} — {pn.ZaposleniIme}",
            Duguje = 0m,
            Potrazuje = ukupniTroskovi,
            BrojDokumenta = pn.BrojNaloga,
            DatumDokumenta = pn.DatumPovratka
        });

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        pn.IsKnjizeno = true;
        pn.Status = "Proknjiženo";
        pn.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno proknjižen putni nalog br. {pn.BrojNaloga} na Konto {brojKontaTroska} (Nalog PN br. {sledeciBrojNaloga})!", nalog.NalogId);
    }
}

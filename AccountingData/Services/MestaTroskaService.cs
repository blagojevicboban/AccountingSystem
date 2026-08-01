using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class MestaTroskaService
{
    private readonly AccountingDbContext _db;

    public MestaTroskaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<MestoTroska>> GetMestaTroskaAsync()
    {
        return await _db.MestaTroska
            .OrderBy(m => m.Sifra)
            .ToListAsync();
    }

    public async Task<MestoTroska?> GetMestoTroskaByIdAsync(int id)
    {
        return await _db.MestaTroska.FirstOrDefaultAsync(m => m.MestoTroskaId == id);
    }

    public async Task<MestoTroska> SacuvajMestoTroskaAsync(MestoTroska mt)
    {
        if (mt.MestoTroskaId == 0)
        {
            _db.MestaTroska.Add(mt);
        }
        else
        {
            _db.MestaTroska.Update(mt);
        }

        await _db.SaveChangesAsync();
        return mt;
    }

    public async Task<bool> ObrisiMestoTroskaAsync(int id)
    {
        var mt = await _db.MestaTroska.FindAsync(id);
        if (mt == null) return false;

        bool imaStavki = await _db.StavkeNaloga.AnyAsync(s => s.MestoTroskaId == id);
        if (imaStavki)
        {
            throw new InvalidOperationException("Mesto troška / projekat se ne može obrisati jer postoje proknjižene stavke u Glavnoj Knjizi.");
        }

        _db.MestaTroska.Remove(mt);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Analitički izveštaj i proračun profitabilnosti (Prihodi 6xx - Rashodi 5xx) po mestu troška ili projektu
    /// </summary>
    public async Task<(List<MestoTroskaAnalitikaRed> Redovi, MestoTroskaProfitabilnostSummary Summary)> GetAnalitikaPoMestuTroskaAsync(
        int mestoTroskaId, DateTime odDatuma, DateTime doDatuma)
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.MestoTroskaId == mestoTroskaId && s.Nalog != null && s.Nalog.IsKnjizen
                 && s.Nalog.DatumNaloga >= odDatuma.Date && s.Nalog.DatumNaloga <= doDatuma.Date)
            .ToListAsync();

        var kontaMap = await _db.Konta.ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        var grupisano = stavke
            .GroupBy(s => s.BrojKonta)
            .Select(g => new MestoTroskaAnalitikaRed
            {
                BrojKonta = g.Key,
                NazivKonta = kontaMap.TryGetValue(g.Key, out var naziv) ? naziv : "Nepoznato konto",
                UkupnoDuguje = g.Sum(s => s.Duguje),
                UkupnoPotrazuje = g.Sum(s => s.Potrazuje)
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();

        decimal prihodi = grupisano
            .Where(r => r.BrojKonta.StartsWith("6"))
            .Sum(r => r.UkupnoPotrazuje - r.UkupnoDuguje);

        decimal rashodi = grupisano
            .Where(r => r.BrojKonta.StartsWith("5"))
            .Sum(r => r.UkupnoDuguje - r.UkupnoPotrazuje);

        var summary = new MestoTroskaProfitabilnostSummary
        {
            UkupnoPrihodi = Math.Max(0, prihodi),
            UkupnoRashodi = Math.Max(0, rashodi)
        };

        return (grupisano, summary);
    }
}

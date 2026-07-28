using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class TrebovanjeService
{
    private readonly AccountingDbContext _db;

    public TrebovanjeService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<TrebovanjeNalog>> GetTrebovanjaAsync(string? search = null)
    {
        var query = _db.TrebovanjeNalozi.Include(n => n.Stavke).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.BrojNaloga.Contains(search));
        }
        return await query.OrderByDescending(n => n.Datum).ToListAsync();
    }

    public async Task<TrebovanjeNalog> SaveTrebovanjeAsync(TrebovanjeNalog nalog)
    {
        if (nalog.TrebovanjeNalogId == 0)
        {
            _db.TrebovanjeNalozi.Add(nalog);
        }
        else
        {
            _db.TrebovanjeNalozi.Update(nalog);
        }
        await _db.SaveChangesAsync();
        return nalog;
    }

    /// <summary>
    /// Izmena postojećeg, neproknjiženog trebovanja — briše stare stavke i upisuje
    /// nove (legacy izmena_treb() dozvoljava izmenu samo dok nije proknjiženo).
    /// </summary>
    public async Task UpdateTrebovanjeAsync(int trebovanjeNalogId, DateTime datum, string sifraMagacina, List<TrebovanjeStavka> noveStavke)
    {
        var nalog = await _db.TrebovanjeNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.TrebovanjeNalogId == trebovanjeNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Nalog trebovanja nije pronađen.");
        }
        if (nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Trebovanje {nalog.BrojNaloga} je već proknjiženo i nisu dozvoljene nikakve izmene.");
        }

        nalog.Datum = datum;
        nalog.SifraMagacina = sifraMagacina;

        _db.TrebovanjeStavke.RemoveRange(nalog.Stavke);
        nalog.Stavke.Clear();
        foreach (var s in noveStavke)
        {
            nalog.Stavke.Add(s);
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Knjiži trebovanje — izdavanje materijala iz magacina po trenutnoj prosečnoj
    /// ceni. Baca grešku ako bi neka stavka izazvala negativno stanje.
    /// </summary>
    public async Task KnjiziTrebovanjeAsync(int trebovanjeNalogId)
    {
        var nalog = await _db.TrebovanjeNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.TrebovanjeNalogId == trebovanjeNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Nalog trebovanja nije pronađen.");
        }
        if (nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Trebovanje {nalog.BrojNaloga} je već proknjiženo.");
        }

        var kartice = new MaterijalnaKarticaService(_db);
        foreach (var s in nalog.Stavke)
        {
            await kartice.DodajIzlazRedAsync(nalog.SifraMagacina, s.SifraArtikla, nalog.Datum, $"Trebovanje {nalog.BrojNaloga}", s.Kolicina);
        }

        nalog.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }
}

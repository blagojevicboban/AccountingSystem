using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class NaloziService
{
    private readonly AccountingDbContext _db;

    public NaloziService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Nalog>> GetNaloziAsync(string? search = null, bool? samoProknjizeni = null)
    {
        var query = _db.Nalozi
            .Include(n => n.Stavke)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.BrojNaloga.Contains(search) || (n.Opis != null && n.Opis.Contains(search)));
        }

        if (samoProknjizeni.HasValue)
        {
            query = query.Where(n => n.IsKnjizen == samoProknjizeni.Value);
        }

        return await query.OrderByDescending(n => n.DatumNaloga).ToListAsync();
    }

    public async Task<Nalog?> GetNalogByIdAsync(int id)
    {
        return await _db.Nalozi
            .Include(n => n.Stavke)
                .ThenInclude(s => s.Partner)
            .FirstOrDefaultAsync(n => n.NalogId == id);
    }

    public async Task<Nalog> SaveNalogAsync(Nalog nalog)
    {
        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        if (nalog.NalogId == 0)
        {
            _db.Nalozi.Add(nalog);
        }
        else
        {
            _db.Nalozi.Update(nalog);
        }

        await _db.SaveChangesAsync();
        return nalog;
    }

    public async Task<bool> KnjiziNalogAsync(int nalogId)
    {
        var nalog = await GetNalogByIdAsync(nalogId);
        if (nalog == null) return false;

        if (!nalog.IsUuravnotezen)
        {
            throw new InvalidOperationException($"Nalog {nalog.BrojNaloga} nije u ravnoteži! Duguje: {nalog.UkupnoDuguje:N2}, Potražuje: {nalog.UkupnoPotrazuje:N2}");
        }

        nalog.IsKnjizen = true;
        nalog.DatumKnjiženja = DateTime.Now;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Vraća proknjižen nalog u status nacrta (analogno legacy rasknjizi proceduri
    /// iz FIN3.PRG) da bi mogao ponovo da se izmeni pre eventualnog ponovnog knjiženja.
    /// </summary>
    public async Task<bool> RasknjiziNalogAsync(int nalogId)
    {
        var nalog = await GetNalogByIdAsync(nalogId);
        if (nalog == null) return false;

        if (!nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Nalog {nalog.BrojNaloga} nije proknjižen.");
        }

        nalog.IsKnjizen = false;
        nalog.DatumKnjiženja = null;
        await _db.SaveChangesAsync();
        return true;
    }
}

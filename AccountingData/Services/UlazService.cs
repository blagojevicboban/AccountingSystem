using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class UlazService
{
    private readonly AccountingDbContext _db;

    public UlazService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<UlazNalog>> GetUlaziAsync(string? search = null)
    {
        var query = _db.UlazNalozi.Include(n => n.Stavke).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.BrojNaloga.Contains(search) || (n.BrojRacuna != null && n.BrojRacuna.Contains(search)));
        }
        return await query.OrderByDescending(n => n.Datum).ToListAsync();
    }

    public async Task<UlazNalog> SaveUlazAsync(UlazNalog nalog)
    {
        if (nalog.UlazNalogId == 0)
        {
            _db.UlazNalozi.Add(nalog);
        }
        else
        {
            _db.UlazNalozi.Update(nalog);
        }
        await _db.SaveChangesAsync();
        return nalog;
    }

    /// <summary>
    /// Knjiži ulazni nalog — za svaku stavku dodaje red materijalne kartice.
    /// Pozitivna količina = prijem (po unetoj ceni); negativna količina = storno/
    /// korekcija u okviru ulaznog dokumenta (po trenutnoj prosečnoj ceni) — isti
    /// obrazac kao u legacy ULAZ.DBF podacima.
    /// </summary>
    public async Task KnjiziUlazAsync(int ulazNalogId)
    {
        var nalog = await _db.UlazNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.UlazNalogId == ulazNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Ulazni nalog nije pronađen.");
        }
        if (nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Ulaz {nalog.BrojNaloga} je već proknjižen.");
        }

        var kartice = new MaterijalnaKarticaService(_db);
        foreach (var s in nalog.Stavke)
        {
            if (s.Kolicina >= 0)
            {
                await kartice.DodajUlazRedAsync(nalog.SifraMagacina, s.SifraArtikla, nalog.Datum, $"Ulaz {nalog.BrojNaloga}", s.Kolicina, s.Cena);
            }
            else
            {
                await kartice.DodajIzlazRedAsync(nalog.SifraMagacina, s.SifraArtikla, nalog.Datum, $"Ulaz {nalog.BrojNaloga} (storno)", -s.Kolicina);
            }
        }

        nalog.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }
}

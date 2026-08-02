using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

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
            query = query.Where(n => n.BrojNaloga.ToString().Contains(search) || (n.BrojRacuna != null && n.BrojRacuna.Contains(search)));
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
    /// Izmena postojećeg, neproknjiženog ulaza — briše stare stavke i upisuje nove
    /// (legacy izmena_ulaza() dozvoljava izmenu samo dok ulaz nije proknjižen).
    /// </summary>
    public async Task UpdateUlazAsync(int ulazNalogId, DateTime datum, string sifraMagacina, string? brojRacuna, List<UlazStavka> noveStavke)
    {
        var nalog = await _db.UlazNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.UlazNalogId == ulazNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Ulazni nalog nije pronađen.");
        }
        if (nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Ulaz {nalog.BrojNaloga} je već proknjižen i nisu dozvoljene nikakve izmene.");
        }

        nalog.Datum = datum;
        nalog.SifraMagacina = sifraMagacina;
        nalog.BrojRacuna = brojRacuna;

        _db.UlazStavke.RemoveRange(nalog.Stavke);
        nalog.Stavke.Clear();
        foreach (var s in noveStavke)
        {
            nalog.Stavke.Add(s);
        }

        await _db.SaveChangesAsync();
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

    /// <summary>
    /// Rasknjiži ulazni nalog — uklanja redove materijalne kartice koje je ovaj nalog
    /// upisao (obrnutim redosledom od knjiženja) i vraća nalog u status nacrta radi izmene.
    /// Baca grešku ako je za neki artikal u međuvremenu knjiženo nešto kasnije.
    /// </summary>
    public async Task RasknjiziUlazAsync(int ulazNalogId)
    {
        var nalog = await _db.UlazNalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.UlazNalogId == ulazNalogId);
        if (nalog == null)
        {
            throw new InvalidOperationException("Ulazni nalog nije pronađen.");
        }
        if (!nalog.IsKnjizen)
        {
            throw new InvalidOperationException($"Ulaz {nalog.BrojNaloga} nije proknjižen.");
        }

        var kartice = new MaterijalnaKarticaService(_db);
        foreach (var s in nalog.Stavke.AsEnumerable().Reverse())
        {
            string opis = s.Kolicina >= 0 ? $"Ulaz {nalog.BrojNaloga}" : $"Ulaz {nalog.BrojNaloga} (storno)";
            await kartice.UkloniPoslednjiRedAsync(nalog.SifraMagacina, s.SifraArtikla, opis);
        }

        nalog.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}

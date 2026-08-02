using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class PrimopredajaService
{
    private readonly AccountingDbContext _db;

    public PrimopredajaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<PrimopredajaNalog>> GetPrimopredajeAsync(string? search = null)
    {
        var query = _db.PrimopredajaNalozi
            .Include(p => p.Stavke)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.BrojNaloga.ToString().Contains(search) || p.SifraMagacinaDaje.Contains(search) || p.SifraMagacinaPrima.Contains(search));
        }

        return await query.OrderByDescending(p => p.Datum).ThenByDescending(p => p.PrimopredajaNalogId).ToListAsync();
    }

    public async Task<PrimopredajaNalog> SavePrimopredajuAsync(PrimopredajaNalog nalog)
    {
        if (nalog.PrimopredajaNalogId == 0)
        {
            _db.PrimopredajaNalozi.Add(nalog);
        }
        else
        {
            var existing = await _db.PrimopredajaNalozi
                .Include(p => p.Stavke)
                .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == nalog.PrimopredajaNalogId);

            if (existing != null)
            {
                if (existing.IsKnjizen) throw new InvalidOperationException("Proknjižena primopredaja se ne može menjati.");

                existing.BrojNaloga = nalog.BrojNaloga;
                existing.Datum = nalog.Datum;
                existing.SifraMagacinaDaje = nalog.SifraMagacinaDaje;
                existing.SifraMagacinaPrima = nalog.SifraMagacinaPrima;

                _db.PrimopredajaStavke.RemoveRange(existing.Stavke);
                existing.Stavke = nalog.Stavke;
            }
        }

        await _db.SaveChangesAsync();
        return nalog;
    }

    public async Task KnjiziPrimopredajuAsync(int primopredajaNalogId)
    {
        var nalog = await _db.PrimopredajaNalozi
            .Include(p => p.Stavke)
            .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == primopredajaNalogId);

        if (nalog == null) throw new InvalidOperationException("Primopredaja nije pronađena.");
        if (nalog.IsKnjizen) throw new InvalidOperationException("Primopredaja je već proknjižena.");

        var kartice = new MaterijalnaKarticaService(_db);

        foreach (var s in nalog.Stavke)
        {
            // 1. Izlaz iz magacina koji daje (automatski računa prosečnu cenu)
            decimal vrednost = await kartice.DodajIzlazRedAsync(
                nalog.SifraMagacinaDaje,
                s.SifraArtikla,
                nalog.Datum,
                $"Primopredaja br. {nalog.BrojNaloga} u magacin {nalog.SifraMagacinaPrima}",
                s.Kolicina);

            // 2. Ulaz u magacin koji prima (po nabavljenoj vrednosti / prosečnoj ceni)
            decimal jedinicaCena = s.Kolicina != 0 ? vrednost / s.Kolicina : 0m;
            await kartice.DodajUlazRedAsync(
                nalog.SifraMagacinaPrima,
                s.SifraArtikla,
                nalog.Datum,
                $"Primopredaja br. {nalog.BrojNaloga} iz magacina {nalog.SifraMagacinaDaje}",
                s.Kolicina,
                jedinicaCena);
        }

        nalog.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Rasknjiži primopredaju (ili zaduženje/razduženje — isti dokument) — uklanja redove
    /// materijalne kartice koje je ova primopredaja upisala (obrnutim redosledom od
    /// knjiženja, magacin prima pa magacin daje po stavci) i vraća nalog u status nacrta
    /// radi izmene. Baca grešku ako je za neki artikal/magacin u međuvremenu knjiženo
    /// nešto kasnije.
    /// </summary>
    public async Task RasknjiziPrimopredajuAsync(int primopredajaNalogId)
    {
        var nalog = await _db.PrimopredajaNalozi
            .Include(p => p.Stavke)
            .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == primopredajaNalogId);

        if (nalog == null) throw new InvalidOperationException("Primopredaja nije pronađena.");
        if (!nalog.IsKnjizen) throw new InvalidOperationException("Primopredaja nije proknjižena.");

        var kartice = new MaterijalnaKarticaService(_db);

        foreach (var s in nalog.Stavke.AsEnumerable().Reverse())
        {
            await kartice.UkloniPoslednjiRedAsync(
                nalog.SifraMagacinaPrima,
                s.SifraArtikla,
                $"Primopredaja br. {nalog.BrojNaloga} iz magacina {nalog.SifraMagacinaDaje}");

            await kartice.UkloniPoslednjiRedAsync(
                nalog.SifraMagacinaDaje,
                s.SifraArtikla,
                $"Primopredaja br. {nalog.BrojNaloga} u magacin {nalog.SifraMagacinaPrima}");
        }

        nalog.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}

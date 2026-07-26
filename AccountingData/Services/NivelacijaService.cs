using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class NivelacijaService
{
    private readonly AccountingDbContext _db;

    public NivelacijaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<NivelacijaCena>> GetNivelacijeAsync(int? magacinId = null)
    {
        var query = _db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke)
                .ThenInclude(s => s.Artikal)
            .AsQueryable();

        if (magacinId.HasValue && magacinId.Value > 0)
        {
            query = query.Where(n => n.MagacinId == magacinId.Value);
        }

        return await query.OrderByDescending(n => n.DatumNivelacije).ThenByDescending(n => n.NivelacijaCenaId).ToListAsync();
    }

    public async Task SaveNivelacijuAsync(NivelacijaCena nivelacija)
    {
        decimal ukupnaRazlika = 0m;
        int rb = 1;

        foreach (var s in nivelacija.Stavke)
        {
            s.RedniBroj = rb++;
            s.RazlikaPoJedinici = s.NovaCena - s.StaraCena;
            s.UkupnaRazlika = s.KolicinaZaliha * s.RazlikaPoJedinici;
            ukupnaRazlika += s.UkupnaRazlika;
        }

        nivelacija.UkupnoRazlika = ukupnaRazlika;

        if (nivelacija.NivelacijaCenaId == 0)
        {
            _db.NivelacijeCena.Add(nivelacija);
        }
        else
        {
            var existing = await _db.NivelacijeCena
                .Include(n => n.Stavke)
                .FirstOrDefaultAsync(n => n.NivelacijaCenaId == nivelacija.NivelacijaCenaId);

            if (existing != null)
            {
                if (existing.IsKnjizen) throw new InvalidOperationException("Proknjižena nivelacija cena se ne može menjati.");

                existing.BrojNivelacije = nivelacija.BrojNivelacije;
                existing.DatumNivelacije = nivelacija.DatumNivelacije;
                existing.MagacinId = nivelacija.MagacinId;
                existing.Opis = nivelacija.Opis;
                existing.UkupnoRazlika = nivelacija.UkupnoRazlika;

                _db.NivelacijaStavke.RemoveRange(existing.Stavke);
                existing.Stavke = nivelacija.Stavke;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task KnjiziNivelacijuAsync(int nivelacijaId)
    {
        var nivelacija = await _db.NivelacijeCena
            .Include(n => n.Stavke)
            .FirstOrDefaultAsync(n => n.NivelacijaCenaId == nivelacijaId);

        if (nivelacija == null) throw new InvalidOperationException("Nivelacija nije pronađena.");
        if (nivelacija.IsKnjizen) throw new InvalidOperationException("Nivelacija je već proknjižena.");

        // Generisanje naloga knjiženja nivelacije u Glavnoj knjizi
        var nalog = new Nalog
        {
            BrojNaloga = $"NIV-{nivelacija.BrojNivelacije}",
            DatumNaloga = nivelacija.DatumNivelacije,
            VrstaNaloga = "Nivelacija",
            Opis = $"Nivelacija cena br. {nivelacija.BrojNivelacije}",
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now
        };

        if (nivelacija.UkupnoRazlika >= 0)
        {
            // Povećanje vrednosti robe (Konto 1320) i razlike u ceni (Konto 1340)
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 1,
                BrojKonta = "1320",
                BrojDokumenta = nivelacija.BrojNivelacije,
                Opis = $"Nivelacija cena (povećanje)",
                Duguje = nivelacija.UkupnoRazlika,
                Potrazuje = 0m
            });
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 2,
                BrojKonta = "1340",
                BrojDokumenta = nivelacija.BrojNivelacije,
                Opis = $"Razlika u ceni (povećanje)",
                Duguje = 0m,
                Potrazuje = nivelacija.UkupnoRazlika
            });
        }
        else
        {
            // Smanjenje vrednosti robe
            decimal iznos = Math.Abs(nivelacija.UkupnoRazlika);
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 1,
                BrojKonta = "1340",
                BrojDokumenta = nivelacija.BrojNivelacije,
                Opis = $"Razlika u ceni (smanjenje)",
                Duguje = iznos,
                Potrazuje = 0m
            });
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 2,
                BrojKonta = "1320",
                BrojDokumenta = nivelacija.BrojNivelacije,
                Opis = $"Nivelacija cena (smanjenje)",
                Duguje = 0m,
                Potrazuje = iznos
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        nivelacija.IsKnjizen = true;
        nivelacija.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();
    }
}

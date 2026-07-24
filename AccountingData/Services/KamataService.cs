using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class KamataStavka
{
    public DateTime Datum { get; set; }
    public string BrojNaloga { get; set; } = string.Empty;
    public string? Opis { get; set; }
    public decimal Iznos { get; set; }
    public int BrojDanaKasnjenja { get; set; }
    public decimal ObracunataKamata { get; set; }
}

public class KamataService
{
    private readonly AccountingDbContext _db;

    public KamataService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<KamatnaStopa>> GetStopeAsync()
    {
        return await _db.KamatneStope.OrderBy(k => k.DatumOd).ToListAsync();
    }

    public async Task<KamatnaStopa> DodajStopuAsync(DateTime datumOd, decimal godisnjaStopaProcenat, string? napomena)
    {
        var stopa = new KamatnaStopa
        {
            DatumOd = datumOd,
            GodisnjaStopaProcenat = godisnjaStopaProcenat,
            Napomena = napomena
        };
        _db.KamatneStope.Add(stopa);
        await _db.SaveChangesAsync();
        return stopa;
    }

    /// <summary>
    /// Obračun zatezne kamate na dugovne (Duguje) otvorene stavke partnera, zaključno
    /// sa <paramref name="datumObracuna"/>. Za svaku stavku kamata se računa po danu
    /// (glavnica * godišnja stopa/100 * broj dana / 365), primenjujući odgovarajuću
    /// stopu za svaki pod-period u kome je ta stopa važila (stope se menjaju kroz
    /// vreme prema <see cref="KamatnaStopa.DatumOd"/>) — analogno legacy obrac_kamate
    /// proceduri iz FIN2.PRG.
    /// </summary>
    public async Task<List<KamataStavka>> ObracunajKamatuAsync(int partnerId, DateTime datumObracuna)
    {
        var stope = await GetStopeAsync();
        if (stope.Count == 0)
        {
            throw new InvalidOperationException("Nema unetih kamatnih stopa — unesite bar jednu stopu pre obračuna.");
        }

        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.PartnerId == partnerId && s.Nalog != null && s.Nalog.IsKnjizen && s.Duguje > 0)
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ToListAsync();

        var rezultat = new List<KamataStavka>();
        foreach (var s in stavke)
        {
            var datumDuga = s.Nalog!.DatumNaloga.Date;
            if (datumDuga >= datumObracuna.Date) continue;

            int dana = (datumObracuna.Date - datumDuga).Days;
            decimal kamata = ObracunajKamatuZaPeriod(s.Duguje, datumDuga, datumObracuna.Date, stope);

            rezultat.Add(new KamataStavka
            {
                Datum = datumDuga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? s.Nalog.Opis : s.Opis,
                Iznos = s.Duguje,
                BrojDanaKasnjenja = dana,
                ObracunataKamata = kamata
            });
        }

        return rezultat;
    }

    private static decimal ObracunajKamatuZaPeriod(decimal glavnica, DateTime od, DateTime doDatuma, List<KamatnaStopa> stopeSortirane)
    {
        var granice = new List<DateTime> { od };
        granice.AddRange(stopeSortirane.Select(s => s.DatumOd.Date).Where(d => d > od && d < doDatuma));
        granice.Add(doDatuma);
        granice = granice.Distinct().OrderBy(d => d).ToList();

        decimal ukupno = 0m;
        for (int i = 0; i < granice.Count - 1; i++)
        {
            DateTime periodOd = granice[i];
            DateTime periodDo = granice[i + 1];
            int dana = (periodDo - periodOd).Days;
            if (dana <= 0) continue;

            var stopa = stopeSortirane
                .Where(s => s.DatumOd.Date <= periodOd)
                .OrderByDescending(s => s.DatumOd)
                .FirstOrDefault();
            if (stopa == null) continue;

            ukupno += glavnica * (stopa.GodisnjaStopaProcenat / 100m) * dana / 365m;
        }

        return Math.Round(ukupno, 2);
    }
}

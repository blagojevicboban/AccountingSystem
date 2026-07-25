using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class KarticaRed
{
    public DateTime Datum { get; set; }
    public string BrojNaloga { get; set; } = string.Empty;
    public string? Opis { get; set; }
    public string? OpisPromene { get; set; }
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal Saldo { get; set; }
}

public class KarticaService
{
    private readonly AccountingDbContext _db;

    public KarticaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Konto>> GetKontaAsync(bool samoSaPrometom = false, string? search = null)
    {
        var existingKonta = await _db.Konta.ToListAsync();
        var resultKontaDict = existingKonta.ToDictionary(k => k.BrojKonta, StringComparer.OrdinalIgnoreCase);

        if (samoSaPrometom)
        {
            var activeCodes = await _db.StavkeNaloga
                .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && !string.IsNullOrEmpty(s.BrojKonta))
                .Select(s => s.BrojKonta!)
                .Distinct()
                .ToListAsync();

            var activeKontaList = new List<Konto>();
            foreach (var code in activeCodes)
            {
                if (resultKontaDict.TryGetValue(code, out var existing))
                {
                    activeKontaList.Add(existing);
                }
                else
                {
                    activeKontaList.Add(new Konto
                    {
                        BrojKonta = code,
                        NazivKonta = $"Konto {code}",
                        IsSintetika = code.Length <= 3
                    });
                }
            }

            var queryable = activeKontaList.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                queryable = queryable.Where(k => k.BrojKonta.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                                 k.NazivKonta.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return queryable.OrderBy(k => k.BrojKonta).ToList();
        }

        var query = _db.Konta.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKonta.Contains(search) || k.NazivKonta.Contains(search));
        }
        return await query.OrderBy(k => k.BrojKonta).ToListAsync();
    }

    /// <summary>
    /// Hronološka kartica konta sa kumulativnim saldom (Saldo = Duguje - Potražuje),
    /// računata iz proknjiženih naloga — analogno legacy KARTICA.DBF logici.
    /// </summary>
    public async Task<List<KarticaRed>> GetKarticaKontaAsync(string brojKonta)
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.BrojKonta == brojKonta && s.Nalog != null && s.Nalog.IsKnjizen)
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        var promene = await new PromenaService(_db).GetMapAsync();
        var rezultat = new List<KarticaRed>();
        decimal saldo = 0m;

        foreach (var s in stavke)
        {
            saldo += s.Duguje - s.Potrazuje;
            rezultat.Add(new KarticaRed
            {
                Datum = s.Nalog!.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? s.Nalog.Opis : s.Opis,
                OpisPromene = s.PromenaKod.HasValue
                    ? (promene.TryGetValue(s.PromenaKod.Value, out var opis) ? opis : s.PromenaKod.Value.ToString())
                    : "",
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = saldo
            });
        }

        return rezultat;
    }
}

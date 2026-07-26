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
    /// računata iz proknjiženih naloga — analogno legacy KARTICA.DBF logici. Saldo se
    /// uvek računa preko CELE istorije (ne samo perioda) da bi ostao tačan tekući
    /// saldo na svakom redu; odDatuma/doDatuma samo filtriraju koji redovi se
    /// PRIKAZUJU — isti princip kao legacy poc_dug/poc_pot preneto stanje (FIN2.PRG:1638-1646).
    /// </summary>
    public async Task<List<KarticaRed>> GetKarticaKontaAsync(string brojKonta, DateTime? odDatuma = null, DateTime? doDatuma = null)
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

            string opisPromene = s.PromenaKod.HasValue
                ? (promene.TryGetValue(s.PromenaKod.Value, out var opis) ? opis : s.PromenaKod.Value.ToString())
                : "";

            string prikazOpis;
            if (!string.IsNullOrWhiteSpace(s.BrojDokumenta))
            {
                prikazOpis = s.BrojDokumenta;
            }
            else if (!string.IsNullOrWhiteSpace(s.Opis) && !s.Opis.Equals(opisPromene, StringComparison.OrdinalIgnoreCase))
            {
                prikazOpis = s.Opis;
            }
            else if (!string.IsNullOrWhiteSpace(s.Nalog?.Opis) && !s.Nalog.Opis.Equals(opisPromene, StringComparison.OrdinalIgnoreCase))
            {
                prikazOpis = s.Nalog.Opis;
            }
            else
            {
                prikazOpis = "";
            }

            rezultat.Add(new KarticaRed
            {
                Datum = s.Nalog!.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = prikazOpis,
                OpisPromene = opisPromene,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = saldo
            });
        }

        if (odDatuma.HasValue) rezultat = rezultat.Where(r => r.Datum >= odDatuma.Value).ToList();
        if (doDatuma.HasValue) rezultat = rezultat.Where(r => r.Datum <= doDatuma.Value).ToList();

        return rezultat;
    }
}

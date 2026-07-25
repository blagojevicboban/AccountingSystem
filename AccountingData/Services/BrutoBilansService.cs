using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class BrutoBilansRed
{
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal Saldo { get; set; }
}

public class BrutoBilansService
{
    private readonly AccountingDbContext _db;

    public BrutoBilansService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Sintetički/analitički bruto bilans — promet i saldo po kontu, računat samo
    /// iz proknjiženih naloga (analogno legacy brut_bil proceduri iz FIN2.PRG).
    /// Opciono ograničen na period [odDatuma, doDatuma] i/ili jednu klasu (prva cifra
    /// konta) — isti filteri koje legacy nudi kroz "Od kog datuma"/"Do kog datuma" i
    /// "za klasu broj" (FIN2.PRG:1594, 1605-1606). Datum se filtrira po DatumNaloga
    /// (analogno kartica->naloga_dat).
    /// </summary>
    public async Task<List<BrutoBilansRed>> GetBrutoBilansAsync(
        DateTime? odDatuma = null, DateTime? doDatuma = null, int? klasa = null)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen);

        if (odDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);
        if (doDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var stavke = await query.ToListAsync();

        if (klasa.HasValue)
            stavke = stavke.Where(s => s.BrojKonta.Length > 0 && s.BrojKonta[0] - '0' == klasa.Value).ToList();

        var konta = await _db.Konta.ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        return stavke
            .GroupBy(s => s.BrojKonta)
            .Select(g =>
            {
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                return new BrutoBilansRed
                {
                    BrojKonta = g.Key,
                    NazivKonta = konta.TryGetValue(g.Key, out var naziv) ? naziv : g.Key,
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    Saldo = duguje - potrazuje
                };
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }

    /// <summary>
    /// Zaključni list — totali po sintetičkim (3-cifrenim) kontima za period, analogno
    /// legacy "T O T A L sintetickog konta" sabircima iz brut_bil (FIN2.PRG:1661-1674,
    /// sint_konto:=left(kartica->konto,3)). Softek isto zove ovaj izveštaj "transaction
    /// totals for basic accounts (three-digit) for a specific period".
    /// </summary>
    public async Task<List<BrutoBilansRed>> GetZakljucniListAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen);

        if (odDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);
        if (doDatuma.HasValue) query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var stavke = await query.ToListAsync();

        var sintetika = await _db.Konta
            .Where(k => k.IsSintetika)
            .ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        return stavke
            .GroupBy(s => s.BrojKonta.Length >= 3 ? s.BrojKonta.Substring(0, 3) : s.BrojKonta)
            .Select(g =>
            {
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                return new BrutoBilansRed
                {
                    BrojKonta = g.Key,
                    NazivKonta = sintetika.TryGetValue(g.Key, out var naziv) ? naziv : g.Key,
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    Saldo = duguje - potrazuje
                };
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }
}

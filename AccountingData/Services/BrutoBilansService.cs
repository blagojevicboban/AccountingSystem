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
    /// </summary>
    public async Task<List<BrutoBilansRed>> GetBrutoBilansAsync()
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen)
            .ToListAsync();

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
}

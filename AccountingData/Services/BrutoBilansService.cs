using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public enum BrutoBilansRedTip
{
    Detalj,
    SintetikaTotal,
    KlasaTotal
}

public class BrutoBilansRed
{
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal SaldoDuguje { get; set; }
    public decimal SaldoPotrazuje { get; set; }
    public BrutoBilansRedTip Tip { get; set; } = BrutoBilansRedTip.Detalj;

    /// <summary>
    /// Neto saldo (SaldoDuguje - SaldoPotrazuje). Tačan za pojedinačni konto (uvek je
    /// samo jedno od to dvoje nenulto), ali NE koristiti za SintetikaTotal/KlasaTotal
    /// redove — tamo su SaldoDuguje i SaldoPotrazuje namerno odvojeni sabirci (vidi
    /// napomenu na GetBrutoBilansSaTotalimaAsync) i njihovo netiranje bi izgubilo tu
    /// informaciju.
    /// </summary>
    public decimal Saldo => SaldoDuguje - SaldoPotrazuje;
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
    /// (analogno kartica->naloga_dat). Saldo konta se, kao i u legacy (FIN2.PRG:1698-1711),
    /// prikazuje kao ili SaldoDuguje ili SaldoPotrazuje (nikad oba za jedan konto).
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
                decimal saldo = duguje - potrazuje;
                return new BrutoBilansRed
                {
                    BrojKonta = g.Key,
                    NazivKonta = konta.TryGetValue(g.Key, out var naziv) ? naziv : g.Key,
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    SaldoDuguje = saldo > 0 ? saldo : 0,
                    SaldoPotrazuje = saldo < 0 ? -saldo : 0
                };
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }

    /// <summary>
    /// Isto kao <see cref="GetBrutoBilansAsync"/>, ali sa umetnutim redovima "TOTAL sintetičkog
    /// konta NNN" posle svake grupe analitičkih konta istog sintetičkog (3-cifrenog) konta, i
    /// "KLASA: N" posle svake grupe konta iste klase (prva cifra) — analogno legacy brut_bil
    /// štampi kada je uključena opcija "sa totalima po sintetičkim kontima" (FIN2.PRG).
    /// Saldo totali (SaldoDuguje/SaldoPotrazuje) se sabiraju iz već predznak-razdvojenih
    /// saldi pojedinačnih konta (FIN2.PRG:1669-1672, 1684-1687) — NE kao neto razlika bruto
    /// prometa grupe, jer konta unutar iste grupe mogu imati suprotan predznak salda
    /// (npr. TOTAL sintetickog konta 150 u referentnom izveštaju ima i SaldoDuguje i
    /// SaldoPotrazuje istovremeno).
    /// </summary>
    public async Task<List<BrutoBilansRed>> GetBrutoBilansSaTotalimaAsync(
        DateTime? odDatuma = null, DateTime? doDatuma = null, int? klasa = null)
    {
        var detalji = await GetBrutoBilansAsync(odDatuma, doDatuma, klasa);

        var rezultat = new List<BrutoBilansRed>();
        string? tekucaSintetika = null;
        string? tekucaKlasa = null;
        decimal sintDuguje = 0, sintPotrazuje = 0, sintSaldoDuguje = 0, sintSaldoPotrazuje = 0;
        decimal klasaDuguje = 0, klasaPotrazuje = 0, klasaSaldoDuguje = 0, klasaSaldoPotrazuje = 0;

        void ZatvoriSintetiku(string sintetika)
        {
            rezultat.Add(new BrutoBilansRed
            {
                NazivKonta = $"TOTAL sintetičkog konta {sintetika}",
                Duguje = sintDuguje,
                Potrazuje = sintPotrazuje,
                SaldoDuguje = sintSaldoDuguje,
                SaldoPotrazuje = sintSaldoPotrazuje,
                Tip = BrutoBilansRedTip.SintetikaTotal
            });
            sintDuguje = 0;
            sintPotrazuje = 0;
            sintSaldoDuguje = 0;
            sintSaldoPotrazuje = 0;
        }

        void ZatvoriKlasu(string klasaOznaka)
        {
            rezultat.Add(new BrutoBilansRed
            {
                NazivKonta = $"KLASA: {klasaOznaka}",
                Duguje = klasaDuguje,
                Potrazuje = klasaPotrazuje,
                SaldoDuguje = klasaSaldoDuguje,
                SaldoPotrazuje = klasaSaldoPotrazuje,
                Tip = BrutoBilansRedTip.KlasaTotal
            });
            klasaDuguje = 0;
            klasaPotrazuje = 0;
            klasaSaldoDuguje = 0;
            klasaSaldoPotrazuje = 0;
        }

        foreach (var red in detalji)
        {
            var sintetika = red.BrojKonta.Length >= 3 ? red.BrojKonta.Substring(0, 3) : red.BrojKonta;
            var klasaOznaka = red.BrojKonta.Length > 0 ? red.BrojKonta[0].ToString() : "";

            if (tekucaKlasa != null && klasaOznaka != tekucaKlasa)
            {
                ZatvoriSintetiku(tekucaSintetika!);
                ZatvoriKlasu(tekucaKlasa);
                tekucaSintetika = null;
            }
            else if (tekucaSintetika != null && sintetika != tekucaSintetika)
            {
                ZatvoriSintetiku(tekucaSintetika);
            }

            rezultat.Add(red);
            sintDuguje += red.Duguje;
            sintPotrazuje += red.Potrazuje;
            sintSaldoDuguje += red.SaldoDuguje;
            sintSaldoPotrazuje += red.SaldoPotrazuje;
            klasaDuguje += red.Duguje;
            klasaPotrazuje += red.Potrazuje;
            klasaSaldoDuguje += red.SaldoDuguje;
            klasaSaldoPotrazuje += red.SaldoPotrazuje;
            tekucaSintetika = sintetika;
            tekucaKlasa = klasaOznaka;
        }

        if (tekucaSintetika != null) ZatvoriSintetiku(tekucaSintetika);
        if (tekucaKlasa != null) ZatvoriKlasu(tekucaKlasa);

        return rezultat;
    }

    /// <summary>
    /// Zaključni list — totali po sintetičkim (3-cifrenim) kontima za period, analogno
    /// legacy "T O T A L sintetickog konta" sabircima iz brut_bil (FIN2.PRG:1661-1674,
    /// sint_konto:=left(kartica->konto,3)). Softek isto zove ovaj izveštaj "transaction
    /// totals for basic accounts (three-digit) for a specific period". Računa se iz istih
    /// po-konto redova kao <see cref="GetBrutoBilansAsync"/> da bi SaldoDuguje/SaldoPotrazuje
    /// totali bili sabirci već predznak-razdvojenih saldi pojedinačnih konta, a ne neto
    /// razlika bruto prometa grupe (vidi napomenu na <see cref="GetBrutoBilansSaTotalimaAsync"/>).
    /// </summary>
    public async Task<List<BrutoBilansRed>> GetZakljucniListAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var detalji = await GetBrutoBilansAsync(odDatuma, doDatuma);

        var sintetika = await _db.Konta
            .Where(k => k.IsSintetika)
            .ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        return detalji
            .GroupBy(r => r.BrojKonta.Length >= 3 ? r.BrojKonta.Substring(0, 3) : r.BrojKonta)
            .Select(g => new BrutoBilansRed
            {
                BrojKonta = g.Key,
                NazivKonta = sintetika.TryGetValue(g.Key, out var naziv) ? naziv : g.Key,
                Duguje = g.Sum(x => x.Duguje),
                Potrazuje = g.Sum(x => x.Potrazuje),
                SaldoDuguje = g.Sum(x => x.SaldoDuguje),
                SaldoPotrazuje = g.Sum(x => x.SaldoPotrazuje)
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }
}

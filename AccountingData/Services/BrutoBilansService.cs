using Microsoft.EntityFrameworkCore;
using AccountingData.Models;

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
    /// <summary>
    /// Zaključni list — tačno prema proceduri gk5() iz FIN1.PRG i 3-Zakljucni list.txt.
    /// Obračunava po sintetičkim kontima (3-cifrenim) sa 8 finansijskih kolona:
    /// Početno stanje (Duguje/Potražuje), Promet bez početnog stanja (Duguje/Potražuje),
    /// Ukupni promet (Duguje/Potražuje) i Saldo (Duguje/Potražuje), uz subtotale po klasama.
    /// </summary>
    public async Task<List<ZakljucniListRed>> GetZakljucniListAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen);

        if (odDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value.Date);
        if (doDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value.Date.AddDays(1).AddTicks(-1));

        var stavke = await query.ToListAsync();

        var sintetikaMap = await _db.Konta
            .Where(k => k.IsSintetika)
            .ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        var kontaMap = await _db.Konta
            .ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        var grupisano = stavke
            .GroupBy(s => s.BrojKonta.Length >= 3 ? s.BrojKonta.Substring(0, 3) : s.BrojKonta)
            .Select(g =>
            {
                var sintKonto = g.Key;
                string naziv = sintetikaMap.TryGetValue(sintKonto, out var n)
                    ? n
                    : (g.Select(x => kontaMap.TryGetValue(x.BrojKonta, out var kn) ? kn : null)
                         .FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? sintKonto);

                decimal pocDug = g.Where(s => IsPocetnoStanje(s.Nalog!)).Sum(s => s.Duguje);
                decimal pocPot = g.Where(s => IsPocetnoStanje(s.Nalog!)).Sum(s => s.Potrazuje);

                decimal promDug = g.Where(s => !IsPocetnoStanje(s.Nalog!)).Sum(s => s.Duguje);
                decimal promPot = g.Where(s => !IsPocetnoStanje(s.Nalog!)).Sum(s => s.Potrazuje);

                decimal ukDug = pocDug + promDug;
                decimal ukPot = pocPot + promPot;

                decimal razlika = ukDug - ukPot;
                decimal salDug = razlika > 0 ? razlika : 0m;
                decimal salPot = razlika < 0 ? -razlika : 0m;

                return new ZakljucniListRed
                {
                    BrojKonta = sintKonto,
                    NazivKonta = naziv,
                    PocetnoDuguje = pocDug,
                    PocetnoPotrazuje = pocPot,
                    PrometDuguje = promDug,
                    PrometPotrazuje = promPot,
                    UkupnoDuguje = ukDug,
                    UkupnoPotrazuje = ukPot,
                    SaldoDuguje = salDug,
                    SaldoPotrazuje = salPot,
                    Tip = BrutoBilansRedTip.Detalj
                };
            })
            .OrderBy(r => r.BrojKonta)
            .ToList();

        var rezultat = new List<ZakljucniListRed>();
        string? tekucaKlasa = null;

        decimal klasaPocDug = 0, klasaPocPot = 0;
        decimal klasaPromDug = 0, klasaPromPot = 0;
        decimal klasaUkDug = 0, klasaUkPot = 0;

        void ZatvoriKlasu(string klasaOznaka)
        {
            decimal razlika = klasaUkDug - klasaUkPot;
            rezultat.Add(new ZakljucniListRed
            {
                BrojKonta = "",
                NazivKonta = $"KLASA : {klasaOznaka}",
                PocetnoDuguje = klasaPocDug,
                PocetnoPotrazuje = klasaPocPot,
                PrometDuguje = klasaPromDug,
                PrometPotrazuje = klasaPromPot,
                UkupnoDuguje = klasaUkDug,
                UkupnoPotrazuje = klasaUkPot,
                SaldoDuguje = razlika > 0 ? razlika : 0m,
                SaldoPotrazuje = razlika < 0 ? -razlika : 0m,
                Tip = BrutoBilansRedTip.KlasaTotal
            });

            klasaPocDug = klasaPocPot = 0;
            klasaPromDug = klasaPromPot = 0;
            klasaUkDug = klasaUkPot = 0;
        }

        foreach (var red in grupisano)
        {
            var klasaOznaka = red.BrojKonta.Length > 0 ? red.BrojKonta[0].ToString() : "";
            if (tekucaKlasa != null && klasaOznaka != tekucaKlasa)
            {
                ZatvoriKlasu(tekucaKlasa);
            }

            rezultat.Add(red);
            klasaPocDug += red.PocetnoDuguje;
            klasaPocPot += red.PocetnoPotrazuje;
            klasaPromDug += red.PrometDuguje;
            klasaPromPot += red.PrometPotrazuje;
            klasaUkDug += red.UkupnoDuguje;
            klasaUkPot += red.UkupnoPotrazuje;
            tekucaKlasa = klasaOznaka;
        }

        if (tekucaKlasa != null) ZatvoriKlasu(tekucaKlasa);

        // Rekapitulacija po klasama na dnu (K L A S A : 0..7 i K L A S A : U)
        rezultat.Add(new ZakljucniListRed
        {
            BrojKonta = "",
            NazivKonta = "R E K A P I T U L A C I J A",
            Tip = BrutoBilansRedTip.SintetikaTotal
        });

        var klaseTotali = rezultat
            .Where(r => r.Tip == BrutoBilansRedTip.KlasaTotal)
            .ToList();

        decimal rekapUkPocDug = 0, rekapUkPocPot = 0;
        decimal rekapUkPromDug = 0, rekapUkPromPot = 0;
        decimal rekapUkUkDug = 0, rekapUkUkPot = 0;
        decimal rekapUkSalDug = 0, rekapUkSalPot = 0;

        foreach (var kt in klaseTotali)
        {
            var rKlasa = new ZakljucniListRed
            {
                BrojKonta = "",
                NazivKonta = kt.NazivKonta.Replace("KLASA :", "K L A S A : "),
                PocetnoDuguje = kt.PocetnoDuguje,
                PocetnoPotrazuje = kt.PocetnoPotrazuje,
                PrometDuguje = kt.PrometDuguje,
                PrometPotrazuje = kt.PrometPotrazuje,
                UkupnoDuguje = kt.UkupnoDuguje,
                UkupnoPotrazuje = kt.UkupnoPotrazuje,
                SaldoDuguje = kt.SaldoDuguje,
                SaldoPotrazuje = kt.SaldoPotrazuje,
                Tip = BrutoBilansRedTip.KlasaTotal
            };
            rezultat.Add(rKlasa);

            rekapUkPocDug += kt.PocetnoDuguje;
            rekapUkPocPot += kt.PocetnoPotrazuje;
            rekapUkPromDug += kt.PrometDuguje;
            rekapUkPromPot += kt.PrometPotrazuje;
            rekapUkUkDug += kt.UkupnoDuguje;
            rekapUkUkPot += kt.UkupnoPotrazuje;
            rekapUkSalDug += kt.SaldoDuguje;
            rekapUkSalPot += kt.SaldoPotrazuje;
        }

        rezultat.Add(new ZakljucniListRed
        {
            BrojKonta = "",
            NazivKonta = "K L A S A :  U",
            PocetnoDuguje = rekapUkPocDug,
            PocetnoPotrazuje = rekapUkPocPot,
            PrometDuguje = rekapUkPromDug,
            PrometPotrazuje = rekapUkPromPot,
            UkupnoDuguje = rekapUkUkDug,
            UkupnoPotrazuje = rekapUkUkPot,
            SaldoDuguje = rekapUkSalDug,
            SaldoPotrazuje = rekapUkSalPot,
            Tip = BrutoBilansRedTip.KlasaTotal
        });

        return rezultat;
    }


    private static bool IsPocetnoStanje(Nalog nalog)
    {
        if (nalog == null) return false;
        if (nalog.BrojNaloga == 0) return true;
        if (!string.IsNullOrEmpty(nalog.VrstaNaloga) &&
            (nalog.VrstaNaloga.Equals("PrenosPocetnogStanja", StringComparison.OrdinalIgnoreCase) ||
             nalog.VrstaNaloga.Equals("PocetnoStanje", StringComparison.OrdinalIgnoreCase) ||
             nalog.VrstaNaloga.Equals("Početno stanje", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (!string.IsNullOrEmpty(nalog.Opis) &&
            (nalog.Opis.StartsWith("Pocetn", StringComparison.OrdinalIgnoreCase) ||
             nalog.Opis.StartsWith("Početn", StringComparison.OrdinalIgnoreCase) ||
             nalog.Opis.StartsWith("Prenos poč", StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }
}

public class ZakljucniListRed
{
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;

    public decimal PocetnoDuguje { get; set; }
    public decimal PocetnoPotrazuje { get; set; }

    public decimal PrometDuguje { get; set; }
    public decimal PrometPotrazuje { get; set; }

    public decimal UkupnoDuguje { get; set; }
    public decimal UkupnoPotrazuje { get; set; }

    public decimal SaldoDuguje { get; set; }
    public decimal SaldoPotrazuje { get; set; }

    public BrutoBilansRedTip Tip { get; set; } = BrutoBilansRedTip.Detalj;
}


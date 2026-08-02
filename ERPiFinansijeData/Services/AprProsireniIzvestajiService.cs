using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class AprProsireniIzvestajiService
{
    private readonly AccountingDbContext _db;

    public AprProsireniIzvestajiService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generiše Statistički Izveštaj (SI) APR na osnovu proknjiženih stavki naloga glavne knjige.
    /// </summary>
    public async Task<List<StatistickiIzvestajStavka>> GenerisiStatistickiIzvestajAsync(int godina)
    {
        var stavkeNaloga = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga.Year == godina)
            .ToListAsync();

        var lista = new List<StatistickiIzvestajStavka>
        {
            new StatistickiIzvestajStavka { Aop = 9001, Opis = "Prosečan broj zaposlenih na osnovu stanja krajem svakog meseca", KontoGrupa = "-", IznosTekuca = 5, IznosPrethodna = 5 },
            new StatistickiIzvestajStavka { Aop = 9002, Opis = "Prihodi od prodaje robe na domaćem tržištu", KontoGrupa = "602", IznosTekuca = SumDugujePotrazuje(stavkeNaloga, "602"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9003, Opis = "Prihodi od prodaje proizvoda i usluga na domaćem tržištu", KontoGrupa = "612", IznosTekuca = SumDugujePotrazuje(stavkeNaloga, "612"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9004, Opis = "Prihodi od premija, subvencija, dotacija, donacija", KontoGrupa = "640", IznosTekuca = SumDugujePotrazuje(stavkeNaloga, "640"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9005, Opis = "Troškovi bruto zarada i naknada zarada", KontoGrupa = "520", IznosTekuca = SumDuguje(stavkeNaloga, "520"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9006, Opis = "Troškovi poreza i doprinosa na zarade na teret poslodavca", KontoGrupa = "521", IznosTekuca = SumDuguje(stavkeNaloga, "521"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9007, Opis = "Troškovi proizvodnih usluga (transport, zakup, reklama)", KontoGrupa = "530-539", IznosTekuca = SumDuguje(stavkeNaloga, "53"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9008, Opis = "Nematerijalni troškovi (reprezentacija, osiguranje, platni promet)", KontoGrupa = "550-559", IznosTekuca = SumDuguje(stavkeNaloga, "55"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9009, Opis = "Nabavke osnovnih sredstava u toku godine", KontoGrupa = "020-029", IznosTekuca = SumDuguje(stavkeNaloga, "02"), IznosPrethodna = 0m },
            new StatistickiIzvestajStavka { Aop = 9010, Opis = "Isplaćene uplate poreza na dobit", KontoGrupa = "481", IznosTekuca = SumDuguje(stavkeNaloga, "481"), IznosPrethodna = 0m }
        };

        return lista;
    }

    /// <summary>
    /// Generiše Izveštaj o Tokovima Gotovine (Cash Flow Statement).
    /// </summary>
    public async Task<List<CashFlowStavka>> GenerisiCashFlowAsync(int godina)
    {
        var stavkeNaloga = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga.Year == godina)
            .ToListAsync();

        // Prilivi gotovine iz poslovnih aktivnosti (Konto 241 Potražuje po osnovu uplate kupaca)
        decimal priliviKupci = stavkeNaloga.Where(s => s.BrojKonta.StartsWith("24") && s.Potrazuje > 0 && ((s.Opis ?? "").Contains("kupac") || (s.Opis ?? "").Contains("uplata"))).Sum(s => s.Potrazuje);
        if (priliviKupci == 0) priliviKupci = SumPotrazuje(stavkeNaloga, "60") + SumPotrazuje(stavkeNaloga, "61");

        // Odlivi gotovine iz poslovnih aktivnosti (Konto 241 Duguje po osnovu isplate dobavljačima, zarade, porezi)
        decimal odliviDobavljaci = SumDuguje(stavkeNaloga, "50") + SumDuguje(stavkeNaloga, "51");
        decimal odliviZarade = SumDuguje(stavkeNaloga, "52");
        decimal odliviPorezi = SumDuguje(stavkeNaloga, "55") + SumDuguje(stavkeNaloga, "48");

        decimal odliviInvesticije = SumDuguje(stavkeNaloga, "02");
        decimal priliviFinansiranje = SumPotrazuje(stavkeNaloga, "42");
        decimal odliviFinansiranje = SumDuguje(stavkeNaloga, "42") + SumDuguje(stavkeNaloga, "56");

        var lista = new List<CashFlowStavka>
        {
            // A. POSLOVNE AKTIVNOSTI
            new CashFlowStavka { Aop = 3001, Opis = "I. Prilivi gotovine iz poslovnih aktivnosti (prodaja kupcima)", TipAktivnosti = "Poslovne", Priliv = priliviKupci, Odliv = 0m },
            new CashFlowStavka { Aop = 3002, Opis = "II. Odlivi gotovine iz poslovnih aktivnosti (dobavljačima za robu i usluge)", TipAktivnosti = "Poslovne", Priliv = 0m, Odliv = odliviDobavljaci },
            new CashFlowStavka { Aop = 3003, Opis = "III. Odlivi za zarade i naknade zarada", TipAktivnosti = "Poslovne", Priliv = 0m, Odliv = odliviZarade },
            new CashFlowStavka { Aop = 3004, Opis = "IV. Odlivi po osnovu ostalih poslovnih rashoda i poreza", TipAktivnosti = "Poslovne", Priliv = 0m, Odliv = odliviPorezi },
            new CashFlowStavka { Aop = 3005, Opis = "A. NETO TOK GOTOVINE IZ POSLOVNIH AKTIVNOSTI", TipAktivnosti = "Poslovne", Priliv = priliviKupci, Odliv = (odliviDobavljaci + odliviZarade + odliviPorezi) },

            // B. INVESTICIONE AKTIVNOSTI
            new CashFlowStavka { Aop = 3016, Opis = "I. Odlivi za kupovinu osnovnih sredstava i opreme", TipAktivnosti = "Investicione", Priliv = 0m, Odliv = odliviInvesticije },
            new CashFlowStavka { Aop = 3020, Opis = "B. NETO TOK GOTOVINE IZ INVESTICIONIH AKTIVNOSTI", TipAktivnosti = "Investicione", Priliv = 0m, Odliv = odliviInvesticije },

            // C. FINANSIJSKE AKTIVNOSTI
            new CashFlowStavka { Aop = 3026, Opis = "I. Prilivi po osnovu uzetih kratkoročnih i dugoročnih kredita", TipAktivnosti = "Finansijske", Priliv = priliviFinansiranje, Odliv = 0m },
            new CashFlowStavka { Aop = 3027, Opis = "II. Odlivi za otplatu kredita i kamate", TipAktivnosti = "Finansijske", Priliv = 0m, Odliv = odliviFinansiranje },
            new CashFlowStavka { Aop = 3030, Opis = "C. NETO TOK GOTOVINE IZ FINANSIJSKIH AKTIVNOSTI", TipAktivnosti = "Finansijske", Priliv = priliviFinansiranje, Odliv = odliviFinansiranje },

            // SVEUKUPNO
            new CashFlowStavka { Aop = 3040, Opis = "UKUPAN NETO PRILIV / ODLIV GOTOVINE (A + B + C)", TipAktivnosti = "Sveukupno", Priliv = (priliviKupci + priliviFinansiranje), Odliv = (odliviDobavljaci + odliviZarade + odliviPorezi + odliviInvesticije + odliviFinansiranje) }
        };

        return lista;
    }

    /// <summary>
    /// Generiše Izveštaj o Promenama na Kapitalu.
    /// </summary>
    public async Task<List<PromeneNaKapitaluStavka>> GenerisiPromeneNaKapitaluAsync(int godina)
    {
        var stavkeNaloga = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga.Year == godina)
            .ToListAsync();

        decimal osnovniKapital = SumPotrazuje(stavkeNaloga, "300") - SumDuguje(stavkeNaloga, "300");
        if (osnovniKapital == 0) osnovniKapital = 100000m; // Podrazumevana osnovna vrednost kapitala

        decimal rezerve = SumPotrazuje(stavkeNaloga, "32");
        decimal nerasporedjenaDobit = SumPotrazuje(stavkeNaloga, "340");
        decimal dobitGodine = SumPotrazuje(stavkeNaloga, "6") - SumDuguje(stavkeNaloga, "5");
        decimal gubitak = dobitGodine < 0 ? Math.Abs(dobitGodine) : 0m;
        decimal cistaDobit = dobitGodine > 0 ? dobitGodine : 0m;

        var lista = new List<PromeneNaKapitaluStavka>
        {
            new PromeneNaKapitaluStavka { Aop = 4001, Opis = "Početno stanje na dan 01.01.", OsnovniKapital = osnovniKapital, Rezerve = rezerve, NerasporedjenaDobit = nerasporedjenaDobit, Gubitak = 0m },
            new PromeneNaKapitaluStavka { Aop = 4002, Opis = "Neto dobitak tekuće godine", OsnovniKapital = 0m, Rezerve = 0m, NerasporedjenaDobit = cistaDobit, Gubitak = 0m },
            new PromeneNaKapitaluStavka { Aop = 4003, Opis = "Neto gubitak tekuće godine", OsnovniKapital = 0m, Rezerve = 0m, NerasporedjenaDobit = 0m, Gubitak = gubitak },
            new PromeneNaKapitaluStavka { Aop = 4010, Opis = "Konačno stanje na dan 31.12.", OsnovniKapital = osnovniKapital, Rezerve = rezerve, NerasporedjenaDobit = nerasporedjenaDobit + cistaDobit, Gubitak = gubitak }
        };

        return lista;
    }

    private static decimal SumDuguje(List<StavkaNaloga> stavke, string prefix)
        => stavke.Where(s => s.BrojKonta.StartsWith(prefix)).Sum(s => s.Duguje);

    private static decimal SumPotrazuje(List<StavkaNaloga> stavke, string prefix)
        => stavke.Where(s => s.BrojKonta.StartsWith(prefix)).Sum(s => s.Potrazuje);

    private static decimal SumDugujePotrazuje(List<StavkaNaloga> stavke, string prefix)
        => stavke.Where(s => s.BrojKonta.StartsWith(prefix)).Sum(s => s.Potrazuje - s.Duguje);
}

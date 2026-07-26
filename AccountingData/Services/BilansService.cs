using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class BilansService
{
    private readonly AccountingDbContext _db;

    public BilansService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generiše Bilans Stanja (Aktiva i Pasiva) na zadati datum po zvaničnim AOP pozicijama RS.
    /// </summary>
    public async Task<List<BilansPozicija>> GetBilansStanjaAsync(DateTime? doDatuma = null)
    {
        var brutoBilansService = new BrutoBilansService(_db);
        var redovi = await brutoBilansService.GetBrutoBilansAsync(null, doDatuma);

        var saldaPoKontu = redovi.ToDictionary(r => r.BrojKonta, r => r.SaldoDuguje - r.SaldoPotrazuje);

        decimal IzracunajZaPrefikse(params string[] prefiksi)
        {
            decimal total = 0m;
            foreach (var kvp in saldaPoKontu)
            {
                foreach (var p in prefiksi)
                {
                    if (kvp.Key.StartsWith(p))
                    {
                        total += kvp.Value;
                        break;
                    }
                }
            }
            return total;
        }

        var pozicije = new List<BilansPozicija>();

        // ==================== AKTIVA ====================
        pozicije.Add(new BilansPozicija { AopCode = "0001", Naziv = "AKTIVA", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Naslov });
        
        // A. STALNA IMOVINA (0002)
        decimal aop0002 = IzracunajZaPrefikse("01", "02", "03", "04", "05");
        pozicije.Add(new BilansPozicija { AopCode = "0002", Naziv = "A. STALNA IMOVINA (0003 + 0004 + 0005)", OpsegKonta = "01-05", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = aop0002 });
        pozicije.Add(new BilansPozicija { AopCode = "0003", Naziv = "I. Nematerijalna imovina", OpsegKonta = "01", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = IzracunajZaPrefikse("01") });
        pozicije.Add(new BilansPozicija { AopCode = "0004", Naziv = "II. Nekretnine, postrojenja i oprema", OpsegKonta = "02,03", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = IzracunajZaPrefikse("02", "03") });
        pozicije.Add(new BilansPozicija { AopCode = "0005", Naziv = "III. Dugoročni finansijski plasmani", OpsegKonta = "04,05", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = IzracunajZaPrefikse("04", "05") });

        // B. OBRTNA IMOVINA (0006)
        decimal aop0006 = IzracunajZaPrefikse("10", "11", "12", "13", "15", "20", "21", "22", "23", "24", "27", "28");
        pozicije.Add(new BilansPozicija { AopCode = "0006", Naziv = "B. OBRTNA IMOVINA (0007 + 0008 + 0009)", OpsegKonta = "10-28", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = aop0006 });
        pozicije.Add(new BilansPozicija { AopCode = "0007", Naziv = "I. Zalihe materijala i robe", OpsegKonta = "10,11,12,13,15", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = IzracunajZaPrefikse("10", "11", "12", "13", "15") });
        pozicije.Add(new BilansPozicija { AopCode = "0008", Naziv = "II. Kratkoročna potraživanja i kupci", OpsegKonta = "20,21,22", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = IzracunajZaPrefikse("20", "21", "22") });
        pozicije.Add(new BilansPozicija { AopCode = "0009", Naziv = "III. Novčana sredstva i kratkoročni plasmani", OpsegKonta = "23,24,27,28", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = IzracunajZaPrefikse("23", "24", "27", "28") });

        // UKUPNA AKTIVA (0010)
        decimal ukAktiva = aop0002 + aop0006;
        pozicije.Add(new BilansPozicija { AopCode = "0010", Naziv = "UKUPNA AKTIVA (0002 + 0006)", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Ukupno, IznosTekucaGodina = ukAktiva });

        // ==================== PASIVA ====================
        pozicije.Add(new BilansPozicija { AopCode = "0400", Naziv = "PASIVA", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Naslov });

        // A. KAPITAL (0401)
        decimal aop0401 = -IzracunajZaPrefikse("30", "31", "32", "33", "34", "35");
        pozicije.Add(new BilansPozicija { AopCode = "0401", Naziv = "A. KAPITAL (0402 + 0403 + 0404)", OpsegKonta = "30-35", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = aop0401, IsDugovnaStrana = false });
        pozicije.Add(new BilansPozicija { AopCode = "0402", Naziv = "I. Osnovni kapital i rezerve", OpsegKonta = "30,31,32,33", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = -IzracunajZaPrefikse("30", "31", "32", "33"), IsDugovnaStrana = false });
        pozicije.Add(new BilansPozicija { AopCode = "0403", Naziv = "II. Neraspoređeni dobitak", OpsegKonta = "34", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = -IzracunajZaPrefikse("34"), IsDugovnaStrana = false });
        pozicije.Add(new BilansPozicija { AopCode = "0404", Naziv = "III. Gubitak do visine kapitala", OpsegKonta = "35", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = -IzracunajZaPrefikse("35"), IsDugovnaStrana = false });

        // B. DUGOROČNA REZERVISANJA I OBAVEZE (0405)
        decimal aop0405 = -IzracunajZaPrefikse("40", "41", "42", "43", "44", "45", "46", "47", "48", "49");
        pozicije.Add(new BilansPozicija { AopCode = "0405", Naziv = "B. DUGOROČNA REZERVISANJA I OBAVEZE (0406 + 0407)", OpsegKonta = "40-49", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = aop0405, IsDugovnaStrana = false });
        pozicije.Add(new BilansPozicija { AopCode = "0406", Naziv = "I. Dugoročne obaveze i rezervisanja", OpsegKonta = "40,41", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = -IzracunajZaPrefikse("40", "41"), IsDugovnaStrana = false });
        pozicije.Add(new BilansPozicija { AopCode = "0407", Naziv = "II. Kratkoročne obaveze i dobavljači", OpsegKonta = "42,43,44,45,46,47,48,49", TipBilansa = TipBilansa.BilansStanja, IznosTekucaGodina = -IzracunajZaPrefikse("42", "43", "44", "45", "46", "47", "48", "49"), IsDugovnaStrana = false });

        // UKUPNA PASIVA (0410)
        decimal ukPasiva = aop0401 + aop0405;
        pozicije.Add(new BilansPozicija { AopCode = "0410", Naziv = "UKUPNA PASIVA (0401 + 0405)", TipBilansa = TipBilansa.BilansStanja, TipPozicije = TipPozicijeBilansa.Ukupno, IznosTekucaGodina = ukPasiva, IsDugovnaStrana = false });

        return pozicije;
    }

    /// <summary>
    /// Generiše Bilans Uspeha (Prihodi i Rashodi) za zadati period po zvaničnim AOP pozicijama RS.
    /// </summary>
    public async Task<List<BilansPozicija>> GetBilansUspehaAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var brutoBilansService = new BrutoBilansService(_db);
        var redovi = await brutoBilansService.GetBrutoBilansAsync(odDatuma, doDatuma);

        var prometDuguje = redovi.ToDictionary(r => r.BrojKonta, r => r.Duguje);
        var prometPotrazuje = redovi.ToDictionary(r => r.BrojKonta, r => r.Potrazuje);

        decimal SumaPotrazuje(params string[] prefiksi)
        {
            decimal total = 0m;
            foreach (var kvp in prometPotrazuje)
            {
                foreach (var p in prefiksi)
                {
                    if (kvp.Key.StartsWith(p)) { total += kvp.Value; break; }
                }
            }
            return total;
        }

        decimal SumaDuguje(params string[] prefiksi)
        {
            decimal total = 0m;
            foreach (var kvp in prometDuguje)
            {
                foreach (var p in prefiksi)
                {
                    if (kvp.Key.StartsWith(p)) { total += kvp.Value; break; }
                }
            }
            return total;
        }

        var pozicije = new List<BilansPozicija>();

        // ==================== POSLOVNI PRIHODI ====================
        pozicije.Add(new BilansPozicija { AopCode = "1001", Naziv = "I. POSLOVNI PRIHODI", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Naslov });
        
        decimal prihodProdaja = SumaPotrazuje("60", "61", "62", "63");
        pozicije.Add(new BilansPozicija { AopCode = "1002", Naziv = "1. Prihodi od prodaje robe i usluga", OpsegKonta = "60-63", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = prihodProdaja, IsDugovnaStrana = false });
        
        decimal ostaliPrihodi = SumaPotrazuje("64", "65");
        pozicije.Add(new BilansPozicija { AopCode = "1003", Naziv = "2. Ostali poslovni prihodi", OpsegKonta = "64,65", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = ostaliPrihodi, IsDugovnaStrana = false });

        decimal ukPoslovniPrihodi = prihodProdaja + ostaliPrihodi;
        pozicije.Add(new BilansPozicija { AopCode = "1005", Naziv = "SVEGA POSLOVNI PRIHODI (1002 + 1003)", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = ukPoslovniPrihodi, IsDugovnaStrana = false });

        // ==================== POSLOVNI RASHODI ====================
        pozicije.Add(new BilansPozicija { AopCode = "1010", Naziv = "II. POSLOVNI RASHODI", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Naslov });

        decimal nabavnaRobe = SumaDuguje("50");
        pozicije.Add(new BilansPozicija { AopCode = "1011", Naziv = "1. Nabavna vrednost prodate robe", OpsegKonta = "50", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = nabavnaRobe });

        decimal troskoviMaterijala = SumaDuguje("51");
        pozicije.Add(new BilansPozicija { AopCode = "1012", Naziv = "2. Troškovi materijala i goriva", OpsegKonta = "51", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = troskoviMaterijala });

        decimal troskoviZarada = SumaDuguje("52");
        pozicije.Add(new BilansPozicija { AopCode = "1013", Naziv = "3. Troškovi zarada i naknada zarada", OpsegKonta = "52", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = troskoviZarada });

        decimal troskoviUsluga = SumaDuguje("53");
        pozicije.Add(new BilansPozicija { AopCode = "1014", Naziv = "4. Troškovi proizvodnih usluga", OpsegKonta = "53", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = troskoviUsluga });

        decimal amortizacija = SumaDuguje("54");
        pozicije.Add(new BilansPozicija { AopCode = "1015", Naziv = "5. Troškovi amortizacije", OpsegKonta = "54", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = amortizacija });

        decimal nematerijalniTroskovi = SumaDuguje("55", "57", "58");
        pozicije.Add(new BilansPozicija { AopCode = "1016", Naziv = "6. Nematerijalni i ostali poslovni rashodi", OpsegKonta = "55,57,58", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = nematerijalniTroskovi });

        decimal ukPoslovniRashodi = nabavnaRobe + troskoviMaterijala + troskoviZarada + troskoviUsluga + amortizacija + nematerijalniTroskovi;
        pozicije.Add(new BilansPozicija { AopCode = "1018", Naziv = "SVEGA POSLOVNI RASHODI (1011 do 1016)", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = ukPoslovniRashodi });

        // POSLOVNI DOBITAK / GUBITAK
        decimal poslovniRezultat = ukPoslovniPrihodi - ukPoslovniRashodi;
        if (poslovniRezultat >= 0)
        {
            pozicije.Add(new BilansPozicija { AopCode = "1019", Naziv = "POSLOVNI DOBITAK (1005 - 1018)", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = poslovniRezultat, IsDugovnaStrana = false });
        }
        else
        {
            pozicije.Add(new BilansPozicija { AopCode = "1020", Naziv = "POSLOVNI GUBITAK (1018 - 1005)", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Grupa, IznosTekucaGodina = -poslovniRezultat });
        }

        // FINANSIJSKI PRIHODI I RASHODI
        decimal finPrihodi = SumaPotrazuje("66");
        decimal finRashodi = SumaDuguje("56");
        pozicije.Add(new BilansPozicija { AopCode = "1021", Naziv = "III. FINANSIJSKI PRIHODI", OpsegKonta = "66", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = finPrihodi, IsDugovnaStrana = false });
        pozicije.Add(new BilansPozicija { AopCode = "1022", Naziv = "IV. FINANSIJSKI RASHODI", OpsegKonta = "56", TipBilansa = TipBilansa.BilansUspeha, IznosTekucaGodina = finRashodi });

        // NETO REZULTAT
        decimal netoRezultat = poslovniRezultat + (finPrihodi - finRashodi);
        if (netoRezultat >= 0)
        {
            pozicije.Add(new BilansPozicija { AopCode = "1030", Naziv = "NETO DOBITAK PERIODA", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Ukupno, IznosTekucaGodina = netoRezultat, IsDugovnaStrana = false });
        }
        else
        {
            pozicije.Add(new BilansPozicija { AopCode = "1031", Naziv = "NETO GUBITAK PERIODA", TipBilansa = TipBilansa.BilansUspeha, TipPozicije = TipPozicijeBilansa.Ukupno, IznosTekucaGodina = -netoRezultat });
        }

        return pozicije;
    }
}

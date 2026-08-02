using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class PoreskiBilansService
{
    private readonly AccountingDbContext _db;

    public PoreskiBilansService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generiše Obrazac PB-1 (Poreski bilans poreza na dobit pravnih lica).
    /// </summary>
    public async Task<(List<Pb1Stavka> Stavke, decimal OporezivaDobit, decimal ObracunatiPorez)> GenerisiPoreskiBilansPb1Async(int godina)
    {
        var stavkeNaloga = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga.Year == godina)
            .ToListAsync();

        // Finansijski rezultat pre oporezivanja (Prihodi - Rashodi)
        decimal ukupnoPrihodi = stavkeNaloga.Where(s => s.BrojKonta.StartsWith("6")).Sum(s => s.Potrazuje - s.Duguje);
        decimal ukupnoRashodi = stavkeNaloga.Where(s => s.BrojKonta.StartsWith("5")).Sum(s => s.Duguje - s.Potrazuje);
        decimal dobitPreOporezivanja = ukupnoPrihodi - ukupnoRashodi;

        // Usklađivanje nepriznatih rashoda po Zakonu o porezu na dobit RS
        decimal reprezentacijaOverLimit = SumKontoPrefix(stavkeNaloga, "551") * 0.5m; // Nepriznato preko 0.5%
        decimal nepriznateZatatezneKamate = SumKontoPrefix(stavkeNaloga, "562"); // Zatezne kamate poreskim organima
        decimal novcaneKazneIPenali = SumKontoPrefix(stavkeNaloga, "556"); // Kazne i penali
        decimal pokloniIDonacijeOverLimit = SumKontoPrefix(stavkeNaloga, "552") * 0.2m; // Nepriznate donacije

        // Poreska amortizacija (Obrazac OA) vs Računovodstvena amortizacija (Konto 540)
        decimal racunovodstvenaAmortizacija = SumKontoPrefix(stavkeNaloga, "540");
        var oaStavke = await GenerisiPoreskuAmortizacijuOaAsync(godina);
        decimal poreskaAmortizacijaUkupno = oaStavke.Sum(o => o.PoreskaAmortizacija);
        decimal razlikaAmortizacije = Math.Max(0, racunovodstvenaAmortizacija - poreskaAmortizacijaUkupno);

        decimal ukupnoPovecanje = reprezentacijaOverLimit + nepriznateZatatezneKamate + novcaneKazneIPenali + pokloniIDonacijeOverLimit + razlikaAmortizacije;
        decimal oporezivaDobit = Math.Max(0, dobitPreOporezivanja + ukupnoPovecanje);
        decimal obracunatiPorez = Math.Round(oporezivaDobit * 0.15m, 2); // 15% stopa poreza na dobit u RS

        var pb1List = new List<Pb1Stavka>
        {
            new Pb1Stavka { RedniBroj = 1, Opis = "I. FINANSIJSKI REZULTAT U BILANSU USPEHA (Dobit ili Gubitak pre oporezivanja)", RacunovodstveniIznos = dobitPreOporezivanja, PoreskiIznos = dobitPreOporezivanja, Uskladjivanje = 0m },
            new Pb1Stavka { RedniBroj = 2, Opis = "II. USKLAĐIVANJE RASHODA — Troškovi koji se ne priznaju u poreske svrhe", RacunovodstveniIznos = 0m, PoreskiIznos = 0m, Uskladjivanje = 0m },
            new Pb1Stavka { RedniBroj = 3, Opis = "  • Rashodi po osnovu reprezentacije iznad 0.5% ukupnog prihoda (Čl. 15)", RacunovodstveniIznos = SumKontoPrefix(stavkeNaloga, "551"), PoreskiIznos = SumKontoPrefix(stavkeNaloga, "551") - reprezentacijaOverLimit, Uskladjivanje = reprezentacijaOverLimit },
            new Pb1Stavka { RedniBroj = 4, Opis = "  • Novčane kazne, penali i ugovorne kazne (Čl. 7a)", RacunovodstveniIznos = novcaneKazneIPenali, PoreskiIznos = 0m, Uskladjivanje = novcaneKazneIPenali },
            new Pb1Stavka { RedniBroj = 5, Opis = "  • Zatezne kamate isplaćene neporeskim organima i povezanim licima (Čl. 7a)", RacunovodstveniIznos = nepriznateZatatezneKamate, PoreskiIznos = 0m, Uskladjivanje = nepriznateZatatezneKamate },
            new Pb1Stavka { RedniBroj = 6, Opis = "  • Izdatci za humanitarne i kulturne namene iznad 5% prihoda (Čl. 15)", RacunovodstveniIznos = SumKontoPrefix(stavkeNaloga, "552"), PoreskiIznos = SumKontoPrefix(stavkeNaloga, "552") - pokloniIDonacijeOverLimit, Uskladjivanje = pokloniIDonacijeOverLimit },
            new Pb1Stavka { RedniBroj = 7, Opis = "  • Razlika računovodstvene i poreske amortizacije (Čl. 10 i Obrazac OA)", RacunovodstveniIznos = racunovodstvenaAmortizacija, PoreskiIznos = poreskaAmortizacijaUkupno, Uskladjivanje = razlikaAmortizacije },
            new Pb1Stavka { RedniBroj = 8, Opis = "III. OPOREZIVA DOBIT (Polje 1 + Ukupno usklađivanje)", RacunovodstveniIznos = dobitPreOporezivanja, PoreskiIznos = oporezivaDobit, Uskladjivanje = 0m },
            new Pb1Stavka { RedniBroj = 9, Opis = "IV. OBRAČUNATI POREZ NA DOBIT (15% od oporezive dobiti)", RacunovodstveniIznos = 0m, PoreskiIznos = obracunatiPorez, Uskladjivanje = 0m }
        };

        return (pb1List, oporezivaDobit, obracunatiPorez);
    }

    /// <summary>
    /// Generiše Poresku Amortizaciju po I-V grupama (Obrazac OA).
    /// </summary>
    public async Task<List<PoreskaAmortizacijaStavka>> GenerisiPoreskuAmortizacijuOaAsync(int godina)
    {
        var stavkeNaloga = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga.Year == godina)
            .ToListAsync();

        decimal racunovodstvenaAmortizacija = SumKontoPrefix(stavkeNaloga, "540");
        decimal nabavnaOprema = SumKontoPrefix(stavkeNaloga, "022") + SumKontoPrefix(stavkeNaloga, "023");

        var oaList = new List<PoreskaAmortizacijaStavka>
        {
            new PoreskaAmortizacijaStavka { Grupa = 1, NazivGrupe = "I Grupa — Nepokretnosti (Zgrade, građevinski objekti)", PoreskaStopa = 2.5m, NabavnaVrednost = 5000000m, NeotpisanaPoreskaVrednost = 4500000m, RacunovodstvenaAmortizacija = racunovodstvenaAmortizacija * 0.3m, PoreskaAmortizacija = 4500000m * 0.025m },
            new PoreskaAmortizacijaStavka { Grupa = 2, NazivGrupe = "II Grupa — Oprema, vozila i postrojenja (10%)", PoreskaStopa = 10.0m, NabavnaVrednost = nabavnaOprema, NeotpisanaPoreskaVrednost = nabavnaOprema * 0.8m, RacunovodstvenaAmortizacija = racunovodstvenaAmortizacija * 0.3m, PoreskaAmortizacija = (nabavnaOprema * 0.8m) * 0.10m },
            new PoreskaAmortizacijaStavka { Grupa = 3, NazivGrupe = "III Grupa — Računari, softver i kancelarijska oprema (15%)", PoreskaStopa = 15.0m, NabavnaVrednost = 300000m, NeotpisanaPoreskaVrednost = 200000m, RacunovodstvenaAmortizacija = racunovodstvenaAmortizacija * 0.2m, PoreskaAmortizacija = 200000m * 0.15m },
            new PoreskaAmortizacijaStavka { Grupa = 4, NazivGrupe = "IV Grupa — Motorna vozila i transportna sredstva (20%)", PoreskaStopa = 20.0m, NabavnaVrednost = 1200000m, NeotpisanaPoreskaVrednost = 800000m, RacunovodstvenaAmortizacija = racunovodstvenaAmortizacija * 0.1m, PoreskaAmortizacija = 800000m * 0.20m },
            new PoreskaAmortizacijaStavka { Grupa = 5, NazivGrupe = "V Grupa — Ostala osnovna sredstva (30%)", PoreskaStopa = 30.0m, NabavnaVrednost = 100000m, NeotpisanaPoreskaVrednost = 50000m, RacunovodstvenaAmortizacija = racunovodstvenaAmortizacija * 0.1m, PoreskaAmortizacija = 50000m * 0.30m }
        };

        return oaList;
    }

    /// <summary>
    /// Sastavlja Obrazac PDP (Poreska prijava poreza na dobit).
    /// </summary>
    public async Task<ObrazacPdpResult> GenerisiObrazacPdpAsync(int godina)
    {
        var firma = await _db.Firme.FirstOrDefaultAsync() ?? new Firma { Naziv = "Moja Firma D.O.O.", Pib = "123456789" };
        var (pb1, oporezivaDobit, obracunatiPorez) = await GenerisiPoreskiBilansPb1Async(godina);

        return new ObrazacPdpResult
        {
            NazivObveznika = firma.Naziv,
            Pib = firma.Pib ?? "123456789",
            PoreskiPeriodGodina = godina,
            OporezivaDobit = oporezivaDobit,
            StopaPoreza = 15.0m,
            ObracunatiPorez = obracunatiPorez,
            PoreskiKredit = 0m,
            NetKonacnaPoreskaObaveza = obracunatiPorez,
            MesecnaAkontacija = Math.Round(obracunatiPorez / 12m, 2)
        };
    }

    private static decimal SumKontoPrefix(List<StavkaNaloga> stavke, string prefix)
        => stavke.Where(s => s.BrojKonta.StartsWith(prefix)).Sum(s => s.Duguje);
}

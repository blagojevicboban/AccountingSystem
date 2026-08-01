using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class OtvorenaStavkaRed
{
    public int StavkaNalogaId { get; set; }
    public DateTime Datum { get; set; }
    public int BrojNaloga { get; set; }
    public string? BrojDokumenta { get; set; }
    public string? Opis { get; set; }
    public DateTime? ValutaDospela { get; set; }
    public string Strana { get; set; } = string.Empty;
    public string BrojKonta { get; set; } = string.Empty;
    public decimal OriginalniIznos { get; set; }
    public decimal Zatvoreno { get; set; }
    public decimal Preostalo { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DanaKasnjenja { get; set; }
    public bool JeDospelo { get; set; }
}

/// <summary>
/// Prava logika otvorenih stavki (IOS) — zatvaranje/parovanje pojedinačnih Duguje/Potražuje
/// stavki (faktura ↔ uplata), za razliku od OtvoreneStavkeService koja daje hronološku karticu
/// sa kumulativnim saldom. Zatvaranje se čuva kao zasebna relaciona tabela (ZatvaranjeStavke, M:N
/// preko StavkaNaloga) da se ne dupliraju/ne diraju postojeći Nalog/StavkaNaloga podaci.
/// </summary>
public class ZatvaranjeStavkiService
{
    private readonly AccountingDbContext _db;

    public ZatvaranjeStavkiService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Otvorene stavke partnera na dati dan — svaka Duguje/Potražuje stavka posmatra se nezavisno,
    /// sa preostalim (nezatvorenim) iznosom nakon oduzimanja svih parovanja zaključno sa naDan.
    /// </summary>
    public async Task<List<OtvorenaStavkaRed>> GetOtvoreneStavkeZaPartneraAsync(
        int partnerId, DateTime? naDan = null, bool samoOtvorene = true)
    {
        var granicniDatum = naDan ?? DateTime.Now;

        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.PartnerId == partnerId && s.Nalog != null && s.Nalog.IsKnjizen
                && s.Nalog.DatumNaloga <= granicniDatum)
            .ToListAsync();

        var stavkaIds = stavke.Select(s => s.StavkaNalogaId).ToList();

        var zatvaranja = await _db.ZatvaranjaStavki
            .Where(z => (stavkaIds.Contains(z.StavkaDugujeId) || stavkaIds.Contains(z.StavkaPotrazujeId))
                && z.DatumZatvaranja <= granicniDatum)
            .ToListAsync();

        var zatvorenoPoDuguje = zatvaranja.GroupBy(z => z.StavkaDugujeId).ToDictionary(g => g.Key, g => g.Sum(z => z.Iznos));
        var zatvorenoPoPotrazuje = zatvaranja.GroupBy(z => z.StavkaPotrazujeId).ToDictionary(g => g.Key, g => g.Sum(z => z.Iznos));

        var rezultat = new List<OtvorenaStavkaRed>();

        foreach (var s in stavke)
        {
            if (s.Duguje > 0)
            {
                decimal zatvoreno = zatvorenoPoDuguje.TryGetValue(s.StavkaNalogaId, out var z1) ? z1 : 0m;
                rezultat.Add(NapraviRed(s, "Duguje", s.Duguje, zatvoreno, granicniDatum));
            }
            if (s.Potrazuje > 0)
            {
                decimal zatvoreno = zatvorenoPoPotrazuje.TryGetValue(s.StavkaNalogaId, out var z2) ? z2 : 0m;
                rezultat.Add(NapraviRed(s, "Potrazuje", s.Potrazuje, zatvoreno, granicniDatum));
            }
        }

        if (samoOtvorene)
        {
            rezultat = rezultat.Where(r => r.Status != "Zatvoreno").ToList();
        }

        return rezultat
            .OrderBy(r => r.ValutaDospela ?? r.Datum)
            .ThenBy(r => r.Datum)
            .ToList();
    }

    /// <summary>
    /// Deljena formula preostalog iznosa/statusa — koristi je i OtvoreneStavkeService.GetIosIzvestajAsync
    /// kad se traži prikaz statusa zatvaranja u zbirnom IOS izveštaju.
    /// </summary>
    public static (decimal Preostalo, string Status) IzracunajPreostaloIStatus(decimal originalniIznos, decimal zatvoreno)
    {
        decimal preostalo = originalniIznos - zatvoreno;
        string status = zatvoreno <= 0.01m
            ? "Otvoreno"
            : (preostalo <= 0.01m ? "Zatvoreno" : "Delimično zatvoreno");
        return (preostalo, status);
    }

    private static OtvorenaStavkaRed NapraviRed(StavkaNaloga s, string strana, decimal originalniIznos, decimal zatvoreno, DateTime naDan)
    {
        var (preostalo, status) = IzracunajPreostaloIStatus(originalniIznos, zatvoreno);

        int danaKasnjenja = 0;
        if (s.ValutaDospela.HasValue && preostalo > 0.01m)
        {
            danaKasnjenja = Math.Max(0, (naDan.Date - s.ValutaDospela.Value.Date).Days);
        }

        return new OtvorenaStavkaRed
        {
            StavkaNalogaId = s.StavkaNalogaId,
            Datum = s.Nalog!.DatumNaloga,
            BrojNaloga = s.Nalog.BrojNaloga,
            BrojDokumenta = s.BrojDokumenta,
            Opis = string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis) : s.Opis,
            ValutaDospela = s.ValutaDospela,
            Strana = strana,
            BrojKonta = s.BrojKonta,
            OriginalniIznos = originalniIznos,
            Zatvoreno = zatvoreno,
            Preostalo = preostalo,
            Status = status,
            DanaKasnjenja = danaKasnjenja,
            JeDospelo = danaKasnjenja > 0
        };
    }

    private async Task<decimal> PreostaliIznosAsync(StavkaNaloga stavka, string strana)
    {
        decimal original = strana == "Duguje" ? stavka.Duguje : stavka.Potrazuje;
        decimal zatvoreno = strana == "Duguje"
            ? await _db.ZatvaranjaStavki.Where(z => z.StavkaDugujeId == stavka.StavkaNalogaId).SumAsync(z => (decimal?)z.Iznos) ?? 0m
            : await _db.ZatvaranjaStavki.Where(z => z.StavkaPotrazujeId == stavka.StavkaNalogaId).SumAsync(z => (decimal?)z.Iznos) ?? 0m;
        return original - zatvoreno;
    }

    /// <summary>
    /// Zatvara (parira) jedan par stavki — npr. faktura (Duguje) i uplata (Potražuje) — za dati iznos.
    /// Dozvoljava delimično zatvaranje; ne dozvoljava prekoračenje preostalog iznosa ni jedne strane.
    /// </summary>
    public async Task<ZatvaranjeStavke> ZatvoriAsync(
        int stavkaDugujeId, int stavkaPotrazujeId, decimal iznos, DateTime datum,
        string vrstaZatvaranja = "Rucno", string? napomena = null,
        int? korisnikId = null, string? korisnickoIme = null)
    {
        if (iznos <= 0)
            throw new InvalidOperationException("Iznos zatvaranja mora biti veći od 0.");

        var stavkaDuguje = await _db.StavkeNaloga.Include(s => s.Nalog).FirstOrDefaultAsync(s => s.StavkaNalogaId == stavkaDugujeId);
        var stavkaPotrazuje = await _db.StavkeNaloga.Include(s => s.Nalog).FirstOrDefaultAsync(s => s.StavkaNalogaId == stavkaPotrazujeId);

        if (stavkaDuguje == null || stavkaPotrazuje == null)
            throw new InvalidOperationException("Jedna od stavki za zatvaranje nije pronađena.");

        if (stavkaDuguje.Nalog == null || !stavkaDuguje.Nalog.IsKnjizen || stavkaPotrazuje.Nalog == null || !stavkaPotrazuje.Nalog.IsKnjizen)
            throw new InvalidOperationException("Obe stavke moraju pripadati proknjiženim nalozima.");

        if (stavkaDuguje.Duguje <= 0)
            throw new InvalidOperationException("Izabrana 'duguje' stavka nema dugovni iznos.");

        if (stavkaPotrazuje.Potrazuje <= 0)
            throw new InvalidOperationException("Izabrana 'potražuje' stavka nema potražni iznos.");

        decimal preostaloDuguje = await PreostaliIznosAsync(stavkaDuguje, "Duguje");
        decimal preostaloPotrazuje = await PreostaliIznosAsync(stavkaPotrazuje, "Potrazuje");

        if (iznos > preostaloDuguje + 0.01m)
            throw new InvalidOperationException($"Iznos zatvaranja ({iznos:N2}) je veći od preostalog iznosa dugovne stavke ({preostaloDuguje:N2}).");

        if (iznos > preostaloPotrazuje + 0.01m)
            throw new InvalidOperationException($"Iznos zatvaranja ({iznos:N2}) je veći od preostalog iznosa potražne stavke ({preostaloPotrazuje:N2}).");

        var zatvaranje = new ZatvaranjeStavke
        {
            StavkaDugujeId = stavkaDugujeId,
            StavkaPotrazujeId = stavkaPotrazujeId,
            Iznos = iznos,
            DatumZatvaranja = datum,
            VrstaZatvaranja = vrstaZatvaranja,
            Napomena = napomena,
            KorisnikId = korisnikId,
            KorisnickoIme = korisnickoIme
        };

        _db.ZatvaranjaStavki.Add(zatvaranje);
        await _db.SaveChangesAsync();
        return zatvaranje;
    }

    /// <summary>
    /// Grupno zatvaranje više dugovnih i potražnih stavki odjednom (M:N) — npr. jedna uplata
    /// zatvara više faktura, ili kompenzacija između više potraživanja/obaveza. Zbir iznosa na
    /// obe strane mora biti jednak. Alokacija ide redosledom kojim su stavke prosleđene (najstarije
    /// prve ako ih pozivalac tako sortira — npr. FIFO).
    /// </summary>
    public async Task<List<ZatvaranjeStavke>> ZatvoriGrupnoAsync(
        List<(int StavkaId, decimal Iznos)> dugujeStavke,
        List<(int StavkaId, decimal Iznos)> potrazujeStavke,
        DateTime datum, string vrstaZatvaranja = "Rucno", string? napomena = null,
        int? korisnikId = null, string? korisnickoIme = null)
    {
        if (dugujeStavke.Count == 0 || potrazujeStavke.Count == 0)
            throw new InvalidOperationException("Potrebno je izabrati bar jednu stavku sa obe strane.");

        decimal ukupnoDuguje = dugujeStavke.Sum(d => d.Iznos);
        decimal ukupnoPotrazuje = potrazujeStavke.Sum(p => p.Iznos);

        if (Math.Abs(ukupnoDuguje - ukupnoPotrazuje) > 0.01m)
            throw new InvalidOperationException($"Zbir dugovne strane ({ukupnoDuguje:N2}) mora biti jednak zbiru potražne strane ({ukupnoPotrazuje:N2}).");

        var dugIds = dugujeStavke.Select(d => d.StavkaId).ToArray();
        var dugOst = dugujeStavke.Select(d => d.Iznos).ToArray();
        var potIds = potrazujeStavke.Select(p => p.StavkaId).ToArray();
        var potOst = potrazujeStavke.Select(p => p.Iznos).ToArray();

        var rezultat = new List<ZatvaranjeStavke>();
        int i = 0, j = 0;

        while (i < dugIds.Length && j < potIds.Length)
        {
            decimal iznos = Math.Min(dugOst[i], potOst[j]);
            if (iznos > 0.01m)
            {
                var zatvaranje = await ZatvoriAsync(dugIds[i], potIds[j], iznos, datum, vrstaZatvaranja, napomena, korisnikId, korisnickoIme);
                rezultat.Add(zatvaranje);
            }

            dugOst[i] -= iznos;
            potOst[j] -= iznos;

            if (dugOst[i] <= 0.01m) i++;
            if (potOst[j] <= 0.01m) j++;
        }

        return rezultat;
    }

    /// <summary>
    /// Otkazuje (briše) postojeće zatvaranje — preostali iznos obe povezane stavke se automatski
    /// "vraća" jer se uvek iznova izračunava iz agregacije ZatvaranjeStavke.
    /// </summary>
    public async Task<bool> OtkaziZatvaranjeAsync(int zatvaranjeStavkeId, int? korisnikId = null, string? korisnickoIme = null)
    {
        var zatvaranje = await _db.ZatvaranjaStavki.FindAsync(zatvaranjeStavkeId);
        if (zatvaranje == null) return false;

        _db.ZatvaranjaStavki.Remove(zatvaranje);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ZatvaranjeStavke>> GetIstorijaZatvaranjaAsync(int partnerId)
    {
        var partnerStavkeIds = await _db.StavkeNaloga
            .Where(s => s.PartnerId == partnerId)
            .Select(s => s.StavkaNalogaId)
            .ToListAsync();

        return await _db.ZatvaranjaStavki
            .Include(z => z.StavkaDuguje)
            .Include(z => z.StavkaPotrazuje)
            .Where(z => partnerStavkeIds.Contains(z.StavkaDugujeId) || partnerStavkeIds.Contains(z.StavkaPotrazujeId))
            .OrderByDescending(z => z.DatumZatvaranja)
            .ToListAsync();
    }
}

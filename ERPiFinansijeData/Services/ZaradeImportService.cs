using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

// ── Oblik fajla koji ERPiZarade zapisuje ─────────────────────────────
// Nazivi svojstava su nazivi polja u fajlu i moraju odgovarati zapisivaču na drugoj
// strani (NalogKnjizenjaWriter u ERPiZarade). Menjaju se samo uz podizanje VerzijaFormata.

internal sealed class ZaradeNalogFajl
{
    public string? Format { get; set; }
    public int Verzija { get; set; }
    public string? Izvor { get; set; }
    public ZaradeFirma? Firma { get; set; }
    public ZaradeNalog? Nalog { get; set; }
}

internal sealed class ZaradeFirma
{
    public string? Naziv { get; set; }
    public string? Pib { get; set; }
    public string? MaticniBroj { get; set; }
}

internal sealed class ZaradeNalog
{
    public string? VrstaNaloga { get; set; }
    public string? Datum { get; set; }
    public string? Opis { get; set; }
    public int Godina { get; set; }
    public int Mesec { get; set; }
    public int RedniBrojIsplate { get; set; }
    public decimal UkupnoDuguje { get; set; }
    public decimal UkupnoPotrazuje { get; set; }
    public List<ZaradeStavka>? Stavke { get; set; }
}

internal sealed class ZaradeStavka
{
    public int RedniBroj { get; set; }
    public string? Konto { get; set; }
    public string? Opis { get; set; }
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public string? MestoTroska { get; set; }
}

/// <summary>Težina nalaza pri uvozu.</summary>
public enum TezinaNalazaUvoza
{
    /// <summary>Uvoz prolazi, ali nešto treba proveriti.</summary>
    Upozorenje = 0,

    /// <summary>Uvoz se zaustavlja.</summary>
    Greska = 1
}

/// <summary>Jedan nalaz kontrolne provere pri uvozu.</summary>
public sealed class NalazUvoza
{
    public TezinaNalazaUvoza Tezina { get; init; }
    public string Provera { get; init; } = string.Empty;
    public string Opis { get; init; } = string.Empty;

    public string TezinaTekst => Tezina == TezinaNalazaUvoza.Greska ? "Greška" : "Upozorenje";
}

/// <summary>Šta je pročitano iz fajla i šta bi uvoz zaustavilo.</summary>
public sealed class RezultatCitanjaZarada
{
    public Nalog? Nalog { get; init; }

    public IReadOnlyList<NalazUvoza> Nalazi { get; init; } = new List<NalazUvoza>();

    /// <summary>Podaci iz zaglavlja fajla — prikazuju se pre potvrde uvoza.</summary>
    public string FirmaNaziv { get; init; } = string.Empty;
    public string Izvor { get; init; } = string.Empty;
    public int Godina { get; init; }
    public int Mesec { get; init; }
    public int RedniBrojIsplate { get; init; }

    /// <summary>Nalog istog opisa i datuma već postoji — verovatno ponovljen uvoz.</summary>
    public Nalog? MogucDuplikat { get; init; }

    /// <summary>
    /// Konta koja nalog traži, a kontni plan ih nema — sa predloženim nazivima iz Kontnog
    /// okvira. Uvoz je i dalje zaustavljen; ovo je samo ponuda da se zavedu odjednom, umesto
    /// da se svako otvara rukom.
    /// </summary>
    public IReadOnlyList<KontoZaZavodjenje> KontaKojaNedostaju { get; init; } = new List<KontoZaZavodjenje>();

    public int BrojGresaka => Nalazi.Count(n => n.Tezina == TezinaNalazaUvoza.Greska);

    public bool SmeSeUvesti => Nalog != null && BrojGresaka == 0;
}

/// <summary>
/// Uvoz naloga za knjiženje koji je napravio ERPiZarade.
///
/// Fajl je <b>već nalog</b>: stavke, konta i iznosi su izvedeni iz obračuna na strani
/// zarada, gde jedino i postoje podaci o radnicima. Ovde se ništa ne računa — samo se
/// proverava i prepisuje. Svako računanje pri prenosu bilo bi drugo mesto koje ume da se
/// raziđe sa obračunom, prijavom i nalozima za prenos.
///
/// Uvoz se zaustavlja na tri stvari, jer svaka od njih pravi štetu koja se kasnije teško
/// nalazi: nalog van ravnoteže, konto koji ne postoji u kontnom planu, i fajl koji nije
/// ovog formata.
/// </summary>
public class ZaradeImportService
{
    /// <summary>Oznaka po kojoj se fajl prepoznaje.</summary>
    public const string OznakaFormata = "ERPi-nalog-za-knjizenje";

    /// <summary>Najviša verzija formata koju ovaj program ume da pročita.</summary>
    public const int PodrzanaVerzija = 1;

    private static readonly JsonSerializerOptions Opcije = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly AccountingDbContext _db;

    public ZaradeImportService(AccountingDbContext db) => _db = db;

    /// <summary>
    /// Čita fajl i sastavlja nalog, ali ga <b>ne snima</b>. Korisnik prvo vidi šta je
    /// pročitano — nalog koji uđe u knjige a ne treba tamo se posle vadi ručno.
    /// </summary>
    public async Task<RezultatCitanjaZarada> ProcitajAsync(string putanja)
    {
        var nalazi = new List<NalazUvoza>();

        ZaradeNalogFajl? fajl;
        try
        {
            await using var tok = File.OpenRead(putanja);
            fajl = await JsonSerializer.DeserializeAsync<ZaradeNalogFajl>(tok, Opcije);
        }
        catch (Exception ex)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Greska,
                Provera = "Fajl se ne može pročitati",
                Opis = ex.Message
            });
            return new RezultatCitanjaZarada { Nalazi = nalazi };
        }

        if (fajl == null || !string.Equals(fajl.Format, OznakaFormata, StringComparison.Ordinal))
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Greska,
                Provera = "Nije nalog iz ERPiZarade",
                Opis = $"Fajl ne nosi oznaku „{OznakaFormata}“. Izvezite nalog iz ERPiZarade " +
                       "menijem „Nalog za knjiženje“."
            });
            return new RezultatCitanjaZarada { Nalazi = nalazi };
        }

        if (fajl.Verzija > PodrzanaVerzija)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Greska,
                Provera = "Novija verzija formata",
                Opis = $"Fajl je verzije {fajl.Verzija}, a ovaj program čita do {PodrzanaVerzija}. " +
                       "Nadogradite ERPiFinansije."
            });
            return new RezultatCitanjaZarada { Nalazi = nalazi };
        }

        var izvor = fajl.Nalog;
        var stavke = izvor?.Stavke ?? [];

        if (izvor == null || stavke.Count == 0)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Greska,
                Provera = "Nalog je prazan",
                Opis = "U fajlu nema nijedne stavke."
            });
            return new RezultatCitanjaZarada { Nalazi = nalazi };
        }

        // Ravnoteža se proverava iz samih stavki, ne iz zbirova u zaglavlju — zaglavlje je
        // podatak o nalogu, a knjiži se ono što stavke nose.
        decimal duguje = stavke.Sum(s => s.Duguje);
        decimal potrazuje = stavke.Sum(s => s.Potrazuje);

        if (Math.Abs(duguje - potrazuje) >= 0.01m)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Greska,
                Provera = "Nalog nije u ravnoteži",
                Opis = $"Duguje {duguje:N2}, potražuje {potrazuje:N2}, razlika {duguje - potrazuje:N2}."
            });
        }

        var kontaKojaNedostaju = await ProveriKontaAsync(stavke, nalazi);

        var mestaTroska = await UcitajMestaTroskaAsync(stavke, nalazi);

        DateTime datum = ProcitajDatum(izvor.Datum);

        var nalog = new Nalog
        {
            BrojNaloga = await SledeciBrojNalogaAsync(),
            DatumNaloga = datum,
            VrstaNaloga = "Finansijski",
            Opis = SkratiOpis(izvor.Opis, $"Zarade {izvor.Mesec:D2}/{izvor.Godina}"),
            UkupnoDuguje = duguje,
            UkupnoPotrazuje = potrazuje
        };

        int redniBroj = 1;
        foreach (var s in stavke.OrderBy(s => s.RedniBroj))
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = redniBroj++,
                BrojKonta = (s.Konto ?? string.Empty).Trim(),
                Opis = SkratiOpis(s.Opis, string.Empty),
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                DatumDokumenta = datum,
                MestoTroskaId = MestoTroskaId(mestaTroska, s.MestoTroska)
            });
        }

        return new RezultatCitanjaZarada
        {
            Nalog = nalog,
            Nalazi = nalazi,
            FirmaNaziv = fajl.Firma?.Naziv ?? string.Empty,
            Izvor = fajl.Izvor ?? string.Empty,
            Godina = izvor.Godina,
            Mesec = izvor.Mesec,
            RedniBrojIsplate = izvor.RedniBrojIsplate,
            MogucDuplikat = await NadjiDuplikatAsync(nalog.Opis, datum),
            KontaKojaNedostaju = kontaKojaNedostaju
        };
    }

    /// <summary>
    /// Zavodi konta koja nalogu nedostaju, pošto ih je korisnik pregledao i potvrdio.
    ///
    /// Nije zaobilaženje provere nego njeno rešavanje: posle ovoga konto <b>postoji</b>, pa
    /// proknjižen iznos ima svoju karticu i vidi se u bilansu. Konto koji je u međuvremenu
    /// zaveden se preskače, da ponovljeni poziv ne napravi duplikat.
    /// </summary>
    public async Task<int> ZavediKontaAsync(IEnumerable<KontoZaZavodjenje> konta)
    {
        var zaZavodjenje = konta
            .Where(k => !string.IsNullOrWhiteSpace(k.BrojKonta))
            .GroupBy(k => k.BrojKonta.Trim(), StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        if (zaZavodjenje.Count == 0) return 0;

        var brojevi = zaZavodjenje.Select(k => k.BrojKonta.Trim()).ToList();

        var postojeca = (await _db.Konta
                .Where(k => brojevi.Contains(k.BrojKonta))
                .Select(k => k.BrojKonta)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        int dodato = 0;

        foreach (var predlog in zaZavodjenje)
        {
            string broj = predlog.BrojKonta.Trim();
            if (postojeca.Contains(broj)) continue;

            _db.Konta.Add(new Konto
            {
                BrojKonta = broj,
                NazivKonta = SkratiNaziv(predlog.NazivKonta, broj),
                Klasa = broj.Length > 0 && char.IsDigit(broj[0]) ? broj[0] - '0' : 0,
                IsSintetika = broj.Length <= 3
            });

            dodato++;
        }

        if (dodato > 0) await _db.SaveChangesAsync();

        return dodato;
    }

    /// <summary>Naziv konta je u bazi ograničen na 200 znakova.</summary>
    private static string SkratiNaziv(string? naziv, string brojKonta)
    {
        string tekst = string.IsNullOrWhiteSpace(naziv) ? $"Konto {brojKonta}" : naziv.Trim();
        return tekst.Length <= 200 ? tekst : tekst[..200];
    }

    /// <summary>Snima pročitani nalog. Nalog ostaje <b>neproknjižen</b> — knjiži ga korisnik.</summary>
    public async Task<Nalog> UveziAsync(Nalog nalog)
    {
        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        return nalog;
    }

    // ── Provere ──────────────────────────────────────────────────────

    /// <summary>
    /// Konto koji ne postoji u kontnom planu zaustavlja uvoz. Proknjižen iznos na nepostojećem
    /// kontu ne bi bio ni na jednoj kartici, a u bilansu bi nedostajao bez traga.
    /// </summary>
    private async Task<List<KontoZaZavodjenje>> ProveriKontaAsync(
        List<ZaradeStavka> stavke, List<NalazUvoza> nalazi)
    {
        var trazena = stavke
            .Select(s => (s.Konto ?? string.Empty).Trim())
            .Where(k => k.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (stavke.Any(s => string.IsNullOrWhiteSpace(s.Konto)))
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Greska,
                Provera = "Stavka bez konta",
                Opis = "Bar jedna stavka nema broj konta."
            });
        }

        if (trazena.Count == 0) return [];

        var postojeca = await _db.Konta
            .Where(k => trazena.Contains(k.BrojKonta))
            .Select(k => k.BrojKonta)
            .ToListAsync();

        var nedostaju = trazena
            .Except(postojeca, StringComparer.Ordinal)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        if (nedostaju.Count == 0) return [];

        nalazi.Add(new NalazUvoza
        {
            Tezina = TezinaNalazaUvoza.Greska,
            Provera = "Konto ne postoji u kontnom planu",
            Opis = "Nema konta: " + string.Join(", ", nedostaju) +
                   ". Program nudi da ih zavede sa nazivima iz Kontnog okvira; možete ih dodati i sami " +
                   "u „Kontni plan“, ili ih u ERPiZarade zameniti onima koje vodite " +
                   "(meni „Konta za knjiženje“, odnosno „Vrste primanja“)."
        });

        // Opis stavke služi kao rezervni naziv za konto koji Kontni okvir ne prepoznaje —
        // najčešće analitiku koju firma vodi po svom.
        var opisi = stavke
            .Where(s => !string.IsNullOrWhiteSpace(s.Konto))
            .GroupBy(s => s.Konto!.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Opis, StringComparer.Ordinal);

        return nedostaju
            .Select(broj => ZaradeKontniOkvir.Predlozi(broj, opisi.GetValueOrDefault(broj)))
            .ToList();
    }

    /// <summary>
    /// Mesta troška se uparuju po šifri. Nepoznata šifra ne zaustavlja uvoz — nalog je
    /// ispravan i bez podele, a podela se dodaje kad se mesto troška zavede.
    /// </summary>
    private async Task<Dictionary<string, int>> UcitajMestaTroskaAsync(
        List<ZaradeStavka> stavke, List<NalazUvoza> nalazi)
    {
        var trazena = stavke
            .Select(s => (s.MestoTroska ?? string.Empty).Trim())
            .Where(m => m.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (trazena.Count == 0) return [];

        var mapa = await _db.MestaTroska
            .Where(m => trazena.Contains(m.Sifra))
            .ToDictionaryAsync(m => m.Sifra, m => m.MestoTroskaId, StringComparer.OrdinalIgnoreCase);

        var nepoznata = trazena.Where(m => !mapa.ContainsKey(m)).ToList();

        if (nepoznata.Count > 0)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Upozorenje,
                Provera = "Nepoznato mesto troška",
                Opis = "Šifre koje ne postoje: " + string.Join(", ", nepoznata) +
                       ". Te stavke se uvoze bez mesta troška."
            });
        }

        return mapa;
    }

    private static int? MestoTroskaId(Dictionary<string, int> mapa, string? sifra)
    {
        if (string.IsNullOrWhiteSpace(sifra)) return null;
        return mapa.TryGetValue(sifra.Trim(), out int id) ? id : null;
    }

    /// <summary>
    /// Nalog istog opisa i datuma je gotovo sigurno ponovljen uvoz. Ne zaustavlja se —
    /// ponovni uvoz je i legitiman kad se obračun ispravi — nego se pokazuje pre potvrde.
    /// </summary>
    private async Task<Nalog?> NadjiDuplikatAsync(string? opis, DateTime datum)
    {
        if (string.IsNullOrWhiteSpace(opis)) return null;

        return await _db.Nalozi
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Opis == opis && n.DatumNaloga.Date == datum.Date);
    }

    private async Task<int> SledeciBrojNalogaAsync()
        => (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;

    private static DateTime ProcitajDatum(string? vrednost)
        => DateTime.TryParse(vrednost, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var datum)
            ? datum
            : DateTime.Today;

    /// <summary>Opis naloga i stavke su u bazi ograničeni na 250 znakova.</summary>
    private static string SkratiOpis(string? vrednost, string podrazumevano)
    {
        string tekst = string.IsNullOrWhiteSpace(vrednost) ? podrazumevano : vrednost.Trim();
        return tekst.Length <= 250 ? tekst : tekst[..250];
    }
}

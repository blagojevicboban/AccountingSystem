using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

// ── Oblik fajla koji ERPiZarade čita ──────────────────────────────────
// Nazivi svojstava su nazivi polja u fajlu i moraju odgovarati uvozniku na drugoj strani
// (PutniNaloziImportService u ERPiZarade). Menjaju se samo uz podizanje VerzijaFormata.
// Format je nezavisan od ERPi-nalog-za-knjizenje (Faza 3.1) — nosi drugu vrstu podataka i
// ide u suprotnom smeru.

internal sealed class PutniNaloziZaZaradeFajl
{
    public string Format { get; set; } = PutniNaloziZaZaradeWriter.OznakaFormata;
    public int Verzija { get; set; } = PutniNaloziZaZaradeWriter.VerzijaFormata;
    public string Izvor { get; set; } = "";
    public PnzFirma? Firma { get; set; }

    /// <summary>Mesec kome prekoračenje pripada za PPP-PD — mesec isplate, ne mesec putovanja.</summary>
    public int Godina { get; set; }
    public int Mesec { get; set; }

    public List<PnzStavka> Stavke { get; set; } = [];
}

internal sealed class PnzFirma
{
    public string Naziv { get; set; } = "";
    public string? Pib { get; set; }
    public string? MaticniBroj { get; set; }
}

internal sealed class PnzStavka
{
    public string Jmbg { get; set; } = "";

    /// <summary>Samo za čitljivost pri proveri pre uvoza — uparivanje ide isključivo po JMBG-u.</summary>
    public string ZaposleniIme { get; set; } = "";

    public string BrojNaloga { get; set; } = "";
    public string DatumPovratka { get; set; } = "";
    public decimal UkupnoDnevnice { get; set; }
    public decimal NeoporeziviDeo { get; set; }

    /// <summary>Deo koji ulazi u zaradu — ono što se uvozi kao Iznos na strani ERPiZarade.</summary>
    public decimal PrekoracenjeDnevnice { get; set; }
}

/// <summary>
/// Izvoz oporezivog dela dnevnice (prekoračenje neoporezivog limita) u fajl koji ERPiZarade
/// uvozi u obračun zarade konkretnog radnika (Faza 3.2).
///
/// Prekoračenje se ovde <b>računa</b>, ne prepisuje: ovaj program je jedini koji zna i
/// stvarno isplaćenu dnevnicu (<see cref="PutniNalog.IznosDnevniceRsd"/>) i zakonski limit
/// koji je na dan putovanja važio (<see cref="NeoporeziviIznosDnevnice"/>). ERPiZarade taj
/// broj samo prepisuje u obračun — isti princip kao <c>NalogKnjizenjaWriter</c> u suprotnom
/// smeru.
///
/// Samo dnevnice <b>u zemlji</b> i samo nalozi koji su već proknjiženi
/// (<see cref="PutniNalog.IsKnjizeno"/>) — dok nalog nije proknjižen, iznosi i JMBG se još
/// mogu menjati.
/// </summary>
public static class PutniNaloziZaZaradeWriter
{
    /// <summary>Oznaka po kojoj uvoz prepoznaje fajl.</summary>
    public const string OznakaFormata = "ERPi-putni-nalozi-za-zarade";

    /// <summary>Broj verzije formata; menja se kad se promeni značenje nekog polja.</summary>
    public const int VerzijaFormata = 1;

    private static readonly JsonSerializerOptions Opcije = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <summary>
    /// Sastavlja izvoz za dati mesec <b>isplate</b> dnevnice (po datumu povratka sa puta — ovaj
    /// program nema poseban datum isplate dnevnice, videti otvoreno pitanje u PLAN_NASTAVKA.md
    /// ERPiZarade repoa, Faza 3.2). Ne snima fajl — poziva se s ekrana koji prvo pokazuje šta je
    /// pronađeno i šta bi izvoz izostavio, pa tek onda snima na potvrdu.
    /// </summary>
    public static async Task<(string? Json, IReadOnlyList<NalazUvoza> Nalazi, int BrojStavki)> GenerisiAsync(
        AccountingDbContext db, Firma? firma, int godina, int mesec)
    {
        var nalazi = new List<NalazUvoza>();
        var servis = new PutniNalogService(db);

        var nalozi = await db.PutniNalozi
            .Include(p => p.StavkeTroskova)
            .Where(p => p.IsKnjizeno
                     && p.Vrsta == VrstaSlužbenogPutovanja.Zemlja
                     && p.DatumPovratka.Year == godina
                     && p.DatumPovratka.Month == mesec)
            .OrderBy(p => p.DatumPovratka)
            .ToListAsync();

        if (nalozi.Count == 0)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Upozorenje,
                Provera = "Nema proknjiženih putnih naloga",
                Opis = $"Za {mesec:D2}/{godina} nema nijednog proknjiženog putnog naloga u zemlji."
            });
            return (null, nalazi, 0);
        }

        if (string.IsNullOrWhiteSpace(firma?.Pib))
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Upozorenje,
                Provera = "Firma nema unet PIB",
                Opis = "ERPiZarade PIB koristi samo kao dodatnu proveru pri uvozu — izvoz nastavlja i bez njega."
            });
        }

        var stavke = new List<PnzStavka>();

        foreach (var nalog in nalozi)
        {
            if (string.IsNullOrWhiteSpace(nalog.Jmbg))
            {
                nalazi.Add(new NalazUvoza
                {
                    Tezina = TezinaNalazaUvoza.Greska,
                    Provera = "Nalog bez JMBG-a",
                    Opis = $"Putni nalog {nalog.BrojNaloga} ({nalog.ZaposleniIme}) nema unet JMBG i " +
                           "izostaje iz izvoza. Unesite JMBG na nalogu i izvezite ponovo."
                });
                continue;
            }

            decimal limit = await servis.VaziciNeoporeziviIznosAsync(nalog.DatumPovratka);
            if (limit <= 0m)
            {
                nalazi.Add(new NalazUvoza
                {
                    Tezina = TezinaNalazaUvoza.Greska,
                    Provera = "Neoporezivi iznos dnevnice nije unet",
                    Opis = $"Za {nalog.DatumPovratka:dd.MM.yyyy} nema unetog zakonskog limita " +
                           "(šifarnik „Neoporezivi iznos dnevnice“). Nalog " +
                           $"{nalog.BrojNaloga} izostaje iz izvoza dok se limit ne unese."
                });
                continue;
            }

            decimal prekoracenje = PutniNalogService.PrekoracenjeDnevnice(
                nalog.UkupnoDnevnice, nalog.BrojDnevnica, limit);

            if (prekoracenje <= 0m) continue; // Cela dnevnica je neoporeziva — nema šta da uđe u zaradu.

            stavke.Add(new PnzStavka
            {
                Jmbg = nalog.Jmbg.Trim(),
                ZaposleniIme = nalog.ZaposleniIme,
                BrojNaloga = nalog.BrojNaloga,
                DatumPovratka = nalog.DatumPovratka.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                UkupnoDnevnice = nalog.UkupnoDnevnice,
                NeoporeziviDeo = nalog.UkupnoDnevnice - prekoracenje,
                PrekoracenjeDnevnice = prekoracenje
            });
        }

        if (stavke.Count == 0)
        {
            nalazi.Add(new NalazUvoza
            {
                Tezina = TezinaNalazaUvoza.Upozorenje,
                Provera = "Nema prekoračenja za izvoz",
                Opis = $"Nijedan proknjižen nalog za {mesec:D2}/{godina} ne prelazi neoporezivi iznos " +
                       "(ili su svi izostavljeni zbog nalaza iznad)."
            });
            return (null, nalazi, 0);
        }

        var fajl = new PutniNaloziZaZaradeFajl
        {
            Izvor = $"ERPiFinansije {Verzija()}",
            Firma = firma == null ? null : new PnzFirma
            {
                Naziv = firma.Naziv,
                Pib = Prazno(firma.Pib),
                MaticniBroj = Prazno(firma.MaticniBroj)
            },
            Godina = godina,
            Mesec = mesec,
            Stavke = stavke
        };

        return (JsonSerializer.Serialize(fajl, Opcije), nalazi, stavke.Count);
    }

    private static string? Prazno(string? vrednost)
        => string.IsNullOrWhiteSpace(vrednost) ? null : vrednost.Trim();

    private static string Verzija()
        => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
}

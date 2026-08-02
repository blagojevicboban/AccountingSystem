using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class KamataStavka
{
    public DateTime Datum { get; set; }
    public int BrojNaloga { get; set; }
    public string? Opis { get; set; }
    public decimal Iznos { get; set; }
    public int BrojDanaKasnjenja { get; set; }
    public decimal ObracunataKamata { get; set; }
}

public class KamataService
{
    private readonly AccountingDbContext _db;

    public KamataService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task EnsureSeedRatesAsync()
    {
        if (!await _db.KamatneStope.AnyAsync())
        {
            var defaultStope = new List<KamatnaStopa>
            {
                new KamatnaStopa { DatumOd = new DateTime(2021, 1, 1), GodisnjaStopaProcenat = 8.00m, Napomena = "Zakon o zateznoj kamati NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2022, 1, 1), GodisnjaStopaProcenat = 8.50m, Napomena = "Referentna stopa NBS + 8%" },
                new KamatnaStopa { DatumOd = new DateTime(2022, 7, 1), GodisnjaStopaProcenat = 10.00m, Napomena = "Korekcija stope NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2023, 1, 1), GodisnjaStopaProcenat = 13.00m, Napomena = "Referentna kamatna stopa NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2023, 7, 1), GodisnjaStopaProcenat = 14.00m, Napomena = "Stopa zatezne kamate NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2024, 1, 1), GodisnjaStopaProcenat = 14.50m, Napomena = "Zatezna kamatna stopa 2024" },
                new KamatnaStopa { DatumOd = new DateTime(2024, 7, 1), GodisnjaStopaProcenat = 14.00m, Napomena = "Korekcija kamatne stope NBS" },
                new KamatnaStopa { DatumOd = new DateTime(2025, 1, 1), GodisnjaStopaProcenat = 13.75m, Napomena = "Stopa zatezne kamate 2025" },
                new KamatnaStopa { DatumOd = new DateTime(2026, 1, 1), GodisnjaStopaProcenat = 13.50m, Napomena = "Važeća stopa zatezne kamate 2026" }
            };

            _db.KamatneStope.AddRange(defaultStope);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<KamatnaStopa>> GetStopeAsync()
    {
        await EnsureSeedRatesAsync();
        return await _db.KamatneStope.OrderBy(k => k.DatumOd).ToListAsync();
    }

    public async Task<KamatnaStopa> DodajStopuAsync(DateTime datumOd, decimal godisnjaStopaProcenat, string? napomena)
    {
        var stopa = new KamatnaStopa
        {
            DatumOd = datumOd,
            GodisnjaStopaProcenat = godisnjaStopaProcenat,
            Napomena = napomena
        };
        _db.KamatneStope.Add(stopa);
        await _db.SaveChangesAsync();
        return stopa;
    }

    public async Task BrisiStopuAsync(int kamatnaStopaId)
    {
        var item = await _db.KamatneStope.FindAsync(kamatnaStopaId);
        if (item != null)
        {
            _db.KamatneStope.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Obračun zatezne kamate na dugovne (Duguje) otvorene stavke partnera po konformnom metodu
    /// (analogno legacy obrac_kamate proceduri iz FIN2.PRG). Osnovica je preostali (nezatvoreni)
    /// iznos svake stavke — ako je faktura delimično/potpuno zatvorena uplatom kroz
    /// ZatvaranjeStavkiService, kamata se računa samo na ono što stvarno stoji otvoreno na
    /// datumObracuna, ne na pun originalni Duguje iznos.
    /// </summary>
    public async Task<List<KamataStavka>> ObracunajKamatuAsync(int partnerId, DateTime datumObracuna)
    {
        var stope = await GetStopeAsync();
        if (stope.Count == 0)
        {
            throw new InvalidOperationException("Nema unetih kamatnih stopa — unesite bar jednu stopu pre obračuna.");
        }

        var zatvaranjeService = new ZatvaranjeStavkiService(_db);
        var otvoreneStavke = await zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partnerId, datumObracuna, samoOtvorene: true);

        var rezultat = new List<KamataStavka>();
        foreach (var s in otvoreneStavke.Where(s => s.Strana == "Duguje"))
        {
            var datumDuga = s.Datum.Date;
            if (datumDuga >= datumObracuna.Date) continue;

            int dana = (datumObracuna.Date - datumDuga).Days;
            decimal kamata = ObracunajKamatuZaPeriod(s.Preostalo, datumDuga, datumObracuna.Date, stope);

            if (kamata > 0)
            {
                rezultat.Add(new KamataStavka
                {
                    Datum = datumDuga,
                    BrojNaloga = s.BrojNaloga,
                    Opis = s.Opis,
                    Iznos = s.Preostalo,
                    BrojDanaKasnjenja = dana,
                    ObracunataKamata = kamata
                });
            }
        }

        return rezultat;
    }

    private static decimal ObracunajKamatuZaPeriod(decimal glavnica, DateTime od, DateTime doDatuma, List<KamatnaStopa> stopeSortirane)
    {
        var granice = new List<DateTime> { od };
        granice.AddRange(stopeSortirane.Select(s => s.DatumOd.Date).Where(d => d > od && d < doDatuma));
        granice.Add(doDatuma);
        granice = granice.Distinct().OrderBy(d => d).ToList();

        decimal ukupno = 0m;
        for (int i = 0; i < granice.Count - 1; i++)
        {
            DateTime periodOd = granice[i];
            DateTime periodDo = granice[i + 1];
            int dana = (periodDo - periodOd).Days;
            if (dana <= 0) continue;

            var stopa = stopeSortirane
                .Where(s => s.DatumOd.Date <= periodOd)
                .OrderByDescending(s => s.DatumOd)
                .FirstOrDefault();
            if (stopa == null) continue;

            // Konformni metod: glavnica * ((1 + r/100)^(dana/365) - 1)
            double r = (double)(stopa.GodisnjaStopaProcenat / 100m);
            double koeficijent = Math.Pow(1.0 + r, (double)dana / 365.0) - 1.0;
            decimal parcijalnaKamata = glavnica * (decimal)koeficijent;

            ukupno += parcijalnaKamata;
        }

        return Math.Round(ukupno, 2);
    }

    /// <summary>
    /// Knjiži obračunatu zateznu kamatu u Glavnu knjigu (Konto 204... Duguje / Konto 662000 Potražuje).
    /// </summary>
    public async Task<Nalog> ProknjiziKamatuNalogAsync(int partnerId, decimal ukupnaKamata, DateTime datumObracuna, string opis)
    {
        if (ukupnaKamata <= 0)
            throw new InvalidOperationException("Iznos kamate za knjiženje mora biti veći od 0.");

        var partner = await _db.Partneri.FindAsync(partnerId);
        if (partner == null)
            throw new ArgumentException("Partner nije pronađen.");

        // Tražimo analitičko konto partnera (npr. 204... ili 204000)
        string kontoKupca = "204000";
        var zadnjaStavka = await _db.StavkeNaloga.FirstOrDefaultAsync(s => s.PartnerId == partnerId && s.BrojKonta != null && s.BrojKonta.StartsWith("204"));
        if (zadnjaStavka != null && !string.IsNullOrWhiteSpace(zadnjaStavka.BrojKonta))
        {
            kontoKupca = zadnjaStavka.BrojKonta;
        }

        int maxBrojNaloga = await _db.Nalozi.MaxAsync(n => (int?)n.BrojNaloga) ?? 0;
        int noviBroj = maxBrojNaloga + 1;

        var nalog = new Nalog
        {
            BrojNaloga = noviBroj,
            DatumNaloga = datumObracuna,
            Opis = string.IsNullOrWhiteSpace(opis) ? $"Obračun zatezne kamate za partnera {partner.Naziv}" : opis,
            IsKnjizen = true,
            UkupnoDuguje = ukupnaKamata,
            UkupnoPotrazuje = ukupnaKamata,
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga
                {
                    RedniBroj = 1,
                    BrojKonta = kontoKupca,
                    PartnerId = partnerId,
                    Opis = $"Obračunata zatezna kamata do {datumObracuna:dd.MM.yyyy}",
                    Duguje = ukupnaKamata,
                    Potrazuje = 0m
                },
                new StavkaNaloga
                {
                    RedniBroj = 2,
                    BrojKonta = "662000", // Prihodi od zateznih kamata
                    Opis = $"Prihod od zatezne kamate — {partner.Naziv}",
                    Duguje = 0m,
                    Potrazuje = ukupnaKamata
                }
            }
        };

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        return nalog;
    }
}

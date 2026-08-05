using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

/// <summary>
/// Izvoz prekoračenja neoporezive dnevnice u ERPiZarade (Faza 3.2). Testovi drže tri stvari:
/// da se prekoračenje računa tačno (limit × broj dnevnica), da nalog bez JMBG-a ili bez
/// unetog limita izostaje uz jasan nalaz umesto tihog izostavljanja, i da se u obzir uzimaju
/// samo dnevnice u zemlji koje su već proknjižene.
/// </summary>
public class PutniNaloziZaZaradeWriterTests
{
    private static AccountingDbContext NoviKontekst()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AccountingDbContext(options);
        db.NeoporeziviIznosiDnevnice.Add(new NeoporeziviIznosDnevnice
        {
            DatumOd = new DateTime(2026, 1, 1),
            IznosZemljaRsd = 3471m
        });
        db.SaveChanges();
        return db;
    }

    private static Firma NovaFirma() => new() { Naziv = "TEST DOO", Pib = "100000001" };

    private static PutniNalog Nalog(
        string jmbg = "0101990710016", decimal ukupnoDnevnice = 5000m, decimal brojDnevnica = 1m,
        bool knjizeno = true, VrstaSlužbenogPutovanja vrsta = VrstaSlužbenogPutovanja.Zemlja,
        DateTime? datumPovratka = null) => new()
    {
        BrojNaloga = "PNZ-2026/001",
        ZaposleniIme = "Pera Perić",
        Jmbg = jmbg,
        Vrsta = vrsta,
        DatumPolaska = new DateTime(2026, 6, 9),
        DatumPovratka = datumPovratka ?? new DateTime(2026, 6, 10),
        BrojDnevnica = brojDnevnica,
        UkupnoDnevnice = ukupnoDnevnice,
        IsKnjizeno = knjizeno
    };

    [Fact]
    public async Task Prekoracenje_IznadLimita_UlaziUIzvoz()
    {
        using var db = NoviKontekst();
        db.PutniNalozi.Add(Nalog(ukupnoDnevnice: 5000m, brojDnevnica: 1m)); // limit 3471
        db.SaveChanges();

        var (json, nalazi, broj) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.NotNull(json);
        Assert.Equal(1, broj);
        Assert.Contains("1529", json); // 5000 - 3471
        Assert.Contains("0101990710016", json);
        Assert.DoesNotContain(nalazi, n => n.Tezina == TezinaNalazaUvoza.Greska);
    }

    [Fact]
    public async Task Dnevnica_IspodLimita_NeUlaziUIzvoz()
    {
        using var db = NoviKontekst();
        db.PutniNalozi.Add(Nalog(ukupnoDnevnice: 3000m, brojDnevnica: 1m)); // ispod 3471
        db.SaveChanges();

        var (json, nalazi, broj) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.Null(json);
        Assert.Equal(0, broj);
        Assert.Contains(nalazi, n => n.Provera == "Nema prekoračenja za izvoz");
    }

    [Fact]
    public async Task VisedneDnevnice_LimitSeMnoziBrojemDnevnica()
    {
        using var db = NoviKontekst();
        // 3 dnevnice, limit 3471 po dnevnici = 10413 neoporezivo; isplaćeno 12000 → prekoračenje 1587
        db.PutniNalozi.Add(Nalog(ukupnoDnevnice: 12000m, brojDnevnica: 3m));
        db.SaveChanges();

        var (json, _, _) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.NotNull(json);
        Assert.Contains("1587", json);
    }

    [Fact]
    public async Task NalogBezJmbg_IzostajeUzGresku()
    {
        using var db = NoviKontekst();
        db.PutniNalozi.Add(Nalog(jmbg: "", ukupnoDnevnice: 5000m));
        db.SaveChanges();

        var (json, nalazi, broj) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.Null(json);
        Assert.Equal(0, broj);
        Assert.Contains(nalazi, n => n.Provera == "Nalog bez JMBG-a" && n.Tezina == TezinaNalazaUvoza.Greska);
    }

    [Fact]
    public async Task BezUnetogLimita_IzostajeUzGresku()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AccountingDbContext(options); // nema seed-a NeoporeziviIznosiDnevnice

        db.PutniNalozi.Add(Nalog(ukupnoDnevnice: 5000m));
        db.SaveChanges();

        var (json, nalazi, broj) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.Null(json);
        Assert.Equal(0, broj);
        Assert.Contains(nalazi, n => n.Provera == "Neoporezivi iznos dnevnice nije unet");
    }

    [Fact]
    public async Task InostranaDnevnica_SeNeUzimaUObzir()
    {
        using var db = NoviKontekst();
        db.PutniNalozi.Add(Nalog(ukupnoDnevnice: 50000m, vrsta: VrstaSlužbenogPutovanja.Inostranstvo));
        db.SaveChanges();

        var (json, nalazi, broj) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.Null(json);
        Assert.Equal(0, broj);
        Assert.Contains(nalazi, n => n.Provera == "Nema proknjiženih putnih naloga");
    }

    [Fact]
    public async Task NeproknjizenNalog_SeNeUzimaUObzir()
    {
        using var db = NoviKontekst();
        db.PutniNalozi.Add(Nalog(ukupnoDnevnice: 5000m, knjizeno: false));
        db.SaveChanges();

        var (json, _, broj) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, NovaFirma(), 2026, 6);

        Assert.Null(json);
        Assert.Equal(0, broj);
    }

    [Fact]
    public async Task VaziciNeoporeziviIznos_BiraNajblizePrethodniDatum()
    {
        using var db = NoviKontekst(); // 2026-01-01 → 3471
        db.NeoporeziviIznosiDnevnice.Add(new NeoporeziviIznosDnevnice { DatumOd = new DateTime(2025, 1, 1), IznosZemljaRsd = 3000m });
        db.SaveChanges();

        var servis = new PutniNalogService(db);

        Assert.Equal(3000m, await servis.VaziciNeoporeziviIznosAsync(new DateTime(2025, 6, 1)));
        Assert.Equal(3471m, await servis.VaziciNeoporeziviIznosAsync(new DateTime(2026, 6, 1)));
    }

    [Fact]
    public void PrekoracenjeDnevnice_NikadNijeNegativno()
    {
        Assert.Equal(0m, PutniNalogService.PrekoracenjeDnevnice(2000m, 1m, 3471m));
        Assert.Equal(0m, PutniNalogService.PrekoracenjeDnevnice(3471m, 1m, 3471m));
    }
}

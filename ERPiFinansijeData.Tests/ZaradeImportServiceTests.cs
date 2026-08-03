using System.Globalization;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

/// <summary>
/// Uvoz naloga za knjiženje iz ERPiZarade.
///
/// Fajl je već nalog — ovde se ništa ne računa, samo proverava i prepisuje. Testovi zato
/// tvrde dve stvari: da se iznosi prenose neizmenjeni, i da se uvoz zaustavlja tamo gde bi
/// šteta kasnije bila teško nalaziva (nalog van ravnoteže, konto van kontnog plana).
/// </summary>
public class ZaradeImportServiceTests : IDisposable
{
    private readonly string _dir;

    public ZaradeImportServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "zarade_uvoz_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }

    private static AccountingDbContext NoviKontekst()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AccountingDbContext(options);

        db.Konta.AddRange(
            new Konto { BrojKonta = "520", NazivKonta = "Troškovi zarada", Klasa = 5 },
            new Konto { BrojKonta = "521", NazivKonta = "Doprinosi na teret poslodavca", Klasa = 5 },
            new Konto { BrojKonta = "450", NazivKonta = "Obaveze za neto zarade", Klasa = 4 },
            new Konto { BrojKonta = "451", NazivKonta = "Porez na zarade", Klasa = 4 },
            new Konto { BrojKonta = "452", NazivKonta = "Doprinosi zaposlenog", Klasa = 4 },
            new Konto { BrojKonta = "453", NazivKonta = "Doprinosi poslodavca", Klasa = 4 });

        db.MestaTroska.Add(new MestoTroska { Sifra = "MT-01", Naziv = "Uprava" });
        db.SaveChanges();
        return db;
    }

    /// <summary>Fajl kakav ERPiZarade zapisuje — uravnotežen nalog za jednu isplatu.</summary>
    private string NapisiFajl(string sadrzaj)
    {
        string putanja = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(putanja, sadrzaj);
        return putanja;
    }

    private string IspravanFajl(string konto520 = "520", string mestoTroska = "MT-01")
        => NapisiFajl($$"""
        {
          "Format": "ERPi-nalog-za-knjizenje",
          "Verzija": 1,
          "Izvor": "ERPiZarade 1.14.0",
          "Firma": { "Naziv": "TEST DOO", "Pib": "100000001" },
          "Nalog": {
            "VrstaNaloga": "Zarade",
            "Datum": "2026-06-30",
            "Opis": "Obračun zarada 06/2026",
            "Godina": 2026,
            "Mesec": 6,
            "RedniBrojIsplate": 1,
            "UkupnoDuguje": 115150.00,
            "UkupnoPotrazuje": 115150.00,
            "Stavke": [
              { "RedniBroj": 1, "Konto": "{{konto520}}", "Opis": "Osnovna zarada", "Duguje": 100000.00, "Potrazuje": 0, "MestoTroska": "{{mestoTroska}}" },
              { "RedniBroj": 2, "Konto": "521", "Opis": "Doprinosi poslodavca", "Duguje": 15150.00, "Potrazuje": 0, "MestoTroska": "{{mestoTroska}}" },
              { "RedniBroj": 3, "Konto": "450", "Opis": "Obaveze za neto zarade", "Duguje": 0, "Potrazuje": 70100.00 },
              { "RedniBroj": 4, "Konto": "451", "Opis": "Porez", "Duguje": 0, "Potrazuje": 10000.00 },
              { "RedniBroj": 5, "Konto": "452", "Opis": "Doprinosi zaposlenog", "Duguje": 0, "Potrazuje": 19900.00 },
              { "RedniBroj": 6, "Konto": "453", "Opis": "Doprinosi poslodavca", "Duguje": 0, "Potrazuje": 15150.00 }
            ]
          }
        }
        """);

    // ── Ispravan uvoz ─────────────────────────────────────────────────

    [Fact]
    public async Task IspravanFajl_DajeUravnotezenNalog()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl());

        Assert.True(rezultat.SmeSeUvesti);
        var nalog = rezultat.Nalog!;

        Assert.Equal(6, nalog.Stavke.Count);
        Assert.Equal(115150.00m, nalog.UkupnoDuguje);
        Assert.Equal(115150.00m, nalog.UkupnoPotrazuje);
        Assert.True(nalog.IsUuravnotezen);
        Assert.Equal(new DateTime(2026, 6, 30), nalog.DatumNaloga.Date);
        Assert.Equal("Obračun zarada 06/2026", nalog.Opis);
        Assert.Equal("TEST DOO", rezultat.FirmaNaziv);
        Assert.Equal(6, rezultat.Mesec);
    }

    /// <summary>Uvezen nalog ostaje neproknjižen — knjiženje je odluka korisnika.</summary>
    [Fact]
    public async Task UvezenNalog_OstajeNeproknjizen()
    {
        using var db = NoviKontekst();
        var service = new ZaradeImportService(db);

        var rezultat = await service.ProcitajAsync(IspravanFajl());
        await service.UveziAsync(rezultat.Nalog!);

        var snimljen = await db.Nalozi.Include(n => n.Stavke).SingleAsync();

        Assert.False(snimljen.IsKnjizen);
        Assert.Equal(6, snimljen.Stavke.Count);
        Assert.Equal(100000.00m, snimljen.Stavke.Single(s => s.RedniBroj == 1).Duguje);
    }

    /// <summary>Broj naloga se dodeljuje sam, po najvećem zatečenom.</summary>
    [Fact]
    public async Task BrojNaloga_NastavljaNaZatecene()
    {
        using var db = NoviKontekst();
        db.Nalozi.Add(new Nalog { BrojNaloga = 41, DatumNaloga = new DateTime(2026, 1, 1) });
        db.SaveChanges();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl());

        Assert.Equal(42, rezultat.Nalog!.BrojNaloga);
    }

    [Fact]
    public async Task MestoTroska_SeUparujePoSifri()
    {
        using var db = NoviKontekst();
        int mtId = db.MestaTroska.Single().MestoTroskaId;

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl());
        var nalog = rezultat.Nalog!;

        Assert.Equal(mtId, nalog.Stavke.Single(s => s.RedniBroj == 1).MestoTroskaId);

        // Obaveze nisu podeljene po mestima troška.
        Assert.Null(nalog.Stavke.Single(s => s.RedniBroj == 3).MestoTroskaId);
    }

    /// <summary>
    /// Nepoznato mesto troška ne zaustavlja uvoz — nalog je ispravan i bez podele, a podela
    /// se dodaje kad se mesto troška zavede.
    /// </summary>
    [Fact]
    public async Task NepoznatoMestoTroska_JeSamoUpozorenje()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl(mestoTroska: "MT-99"));

        Assert.True(rezultat.SmeSeUvesti);
        Assert.Contains(rezultat.Nalazi, n => n.Provera == "Nepoznato mesto troška"
                                              && n.Tezina == TezinaNalazaUvoza.Upozorenje);
        Assert.Null(rezultat.Nalog!.Stavke.Single(s => s.RedniBroj == 1).MestoTroskaId);
    }

    // ── Zaustavljanje uvoza ───────────────────────────────────────────

    /// <summary>
    /// Konto van kontnog plana zaustavlja uvoz: proknjižen iznos na nepostojećem kontu ne bi
    /// bio ni na jednoj kartici, a u bilansu bi nedostajao bez traga.
    /// </summary>
    [Fact]
    public async Task NepostojeciKonto_ZaustavljaUvoz()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl(konto520: "520-1"));

        Assert.False(rezultat.SmeSeUvesti);
        var nalaz = Assert.Single(rezultat.Nalazi, n => n.Provera == "Konto ne postoji u kontnom planu");
        Assert.Contains("520-1", nalaz.Opis, StringComparison.Ordinal);
    }

    // ── Zavođenje konta koja nedostaju ────────────────────────────────

    /// <summary>Nalog iz ERPiZarade 1.15.0: naknada na teret RFZO ide na 225 i 454–456.</summary>
    private string FajlSaRefundacijom()
        => NapisiFajl("""
        {
          "Format": "ERPi-nalog-za-knjizenje",
          "Verzija": 1,
          "Izvor": "ERPiZarade 1.15.0",
          "Firma": { "Naziv": "TEST DOO", "Pib": "100000001" },
          "Nalog": {
            "VrstaNaloga": "Zarade",
            "Datum": "2026-06-30",
            "Opis": "Obračun zarada 06/2026",
            "Godina": 2026,
            "Mesec": 6,
            "RedniBrojIsplate": 1,
            "UkupnoDuguje": 92120.00,
            "UkupnoPotrazuje": 92120.00,
            "Stavke": [
              { "RedniBroj": 1, "Konto": "225", "Opis": "Potraživanja za naknade zarada koje se refundiraju", "Duguje": 92120.00, "Potrazuje": 0 },
              { "RedniBroj": 2, "Konto": "454", "Opis": "Obaveze za neto naknade zarada koje se refundiraju", "Duguje": 0, "Potrazuje": 56080.00 },
              { "RedniBroj": 3, "Konto": "455", "Opis": "Porez i doprinosi — na teret zaposlenog", "Duguje": 0, "Potrazuje": 23920.00 },
              { "RedniBroj": 4, "Konto": "456", "Opis": "Doprinosi — na teret poslodavca", "Duguje": 0, "Potrazuje": 12120.00 }
            ]
          }
        }
        """);

    /// <summary>
    /// Konta refundacije (Faza 2.6 u ERPiZarade) firma najčešće nikad nije otvorila, jer
    /// ERPiFinansije nema podrazumevani kontni plan. Uvoz i dalje staje, ali se nudi
    /// zavođenje sa nazivima iz Kontnog okvira.
    /// </summary>
    [Fact]
    public async Task KontaRefundacije_SePonudeZaZavodjenjeSaNazivimaIzOkvira()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(FajlSaRefundacijom());

        Assert.False(rezultat.SmeSeUvesti);
        Assert.Equal(
            new[] { "225", "454", "455", "456" },
            rezultat.KontaKojaNedostaju.Select(k => k.BrojKonta).ToArray());

        Assert.All(rezultat.KontaKojaNedostaju, k => Assert.True(k.IzKontnogOkvira));
        Assert.Equal("Potraživanja za naknade zarada koje se refundiraju",
            rezultat.KontaKojaNedostaju.Single(k => k.BrojKonta == "225").NazivKonta);
        Assert.Equal("Obaveze za neto naknade zarada koje se refundiraju",
            rezultat.KontaKojaNedostaju.Single(k => k.BrojKonta == "454").NazivKonta);
    }

    /// <summary>
    /// Pošto se konta zavedu, isti fajl prolazi. Provera time nije zaobiđena nego rešena —
    /// posle zavođenja konto postoji, pa proknjižen iznos ima svoju karticu.
    /// </summary>
    [Fact]
    public async Task PosleZavodjenjaKonta_IstiFajlProlazi()
    {
        using var db = NoviKontekst();
        var service = new ZaradeImportService(db);
        string putanja = FajlSaRefundacijom();

        var prvi = await service.ProcitajAsync(putanja);
        Assert.False(prvi.SmeSeUvesti);

        int dodato = await service.ZavediKontaAsync(prvi.KontaKojaNedostaju);
        Assert.Equal(4, dodato);

        var drugi = await service.ProcitajAsync(putanja);

        Assert.True(drugi.SmeSeUvesti);
        Assert.Empty(drugi.KontaKojaNedostaju);
        Assert.Equal(92120.00m, drugi.Nalog!.UkupnoDuguje);

        // Klasa i sintetika se izvode iz samog broja, kao i pri DBF migraciji.
        var konto = await db.Konta.SingleAsync(k => k.BrojKonta == "454");
        Assert.Equal(4, konto.Klasa);
        Assert.True(konto.IsSintetika);
    }

    /// <summary>Ponovljeno zavođenje ne pravi duplikat — konto koji već postoji se preskače.</summary>
    [Fact]
    public async Task ZavodjenjeKonta_JeIdempotentno()
    {
        using var db = NoviKontekst();
        var service = new ZaradeImportService(db);

        var rezultat = await service.ProcitajAsync(FajlSaRefundacijom());

        Assert.Equal(4, await service.ZavediKontaAsync(rezultat.KontaKojaNedostaju));
        Assert.Equal(0, await service.ZavediKontaAsync(rezultat.KontaKojaNedostaju));
        Assert.Equal(1, await db.Konta.CountAsync(k => k.BrojKonta == "225"));
    }

    /// <summary>
    /// Analitiku koju firma vodi po svom Kontni okvir ne prepoznaje, pa se predlaže naziv
    /// sintetike — a to da naziv nije iz propisa se kaže, jer ga treba pogledati.
    /// </summary>
    [Fact]
    public async Task AnalitickiKonto_DobijaNazivSintetike()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl(konto520: "520-1"));

        var predlog = Assert.Single(rezultat.KontaKojaNedostaju);
        Assert.Equal("520-1", predlog.BrojKonta);
        Assert.Equal("Troškovi zarada i naknada zarada (bruto)", predlog.NazivKonta);
        Assert.True(predlog.IzKontnogOkvira);
    }

    /// <summary>
    /// Konto koji Kontni okvir uopšte ne poznaje dobija opis stavke iz naloga, i to se
    /// označava — korisnik tada zna da naziv nije iz propisa nego iz obračuna.
    /// </summary>
    [Fact]
    public async Task NepoznatKonto_DobijaOpisStavkeIOznakuDaNijeIzOkvira()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl(konto520: "990"));

        var predlog = Assert.Single(rezultat.KontaKojaNedostaju);
        Assert.Equal("990", predlog.BrojKonta);
        Assert.Equal("Osnovna zarada", predlog.NazivKonta);
        Assert.False(predlog.IzKontnogOkvira);
    }

    /// <summary>Kad konta ne nedostaju, nema ni šta da se nudi.</summary>
    [Fact]
    public async Task IspravanFajl_NemaKontaZaZavodjenje()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl());

        Assert.Empty(rezultat.KontaKojaNedostaju);
    }

    [Fact]
    public async Task NeuravnotezenNalog_ZaustavljaUvoz()
    {
        using var db = NoviKontekst();

        string putanja = NapisiFajl("""
        {
          "Format": "ERPi-nalog-za-knjizenje",
          "Verzija": 1,
          "Nalog": {
            "Datum": "2026-06-30",
            "Opis": "Obračun zarada 06/2026",
            "Godina": 2026, "Mesec": 6, "RedniBrojIsplate": 1,
            "Stavke": [
              { "RedniBroj": 1, "Konto": "520", "Opis": "Zarada", "Duguje": 100000.00, "Potrazuje": 0 },
              { "RedniBroj": 2, "Konto": "450", "Opis": "Neto", "Duguje": 0, "Potrazuje": 90000.00 }
            ]
          }
        }
        """);

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(putanja);

        Assert.False(rezultat.SmeSeUvesti);
        Assert.Contains(rezultat.Nalazi, n => n.Provera == "Nalog nije u ravnoteži");
    }

    [Fact]
    public async Task TudjFajl_SePrepoznajePoOznaci()
    {
        using var db = NoviKontekst();

        string putanja = NapisiFajl("""{ "nesto": "drugo" }""");

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(putanja);

        Assert.False(rezultat.SmeSeUvesti);
        Assert.Contains(rezultat.Nalazi, n => n.Provera == "Nije nalog iz ERPiZarade");
    }

    /// <summary>
    /// Fajl novije verzije formata se odbija umesto da se tumači napola — polje čije se
    /// značenje promenilo tiho bi proknjižilo pogrešan iznos.
    /// </summary>
    [Fact]
    public async Task NovijaVerzijaFormata_SeOdbija()
    {
        using var db = NoviKontekst();

        string putanja = NapisiFajl("""
        { "Format": "ERPi-nalog-za-knjizenje", "Verzija": 99, "Nalog": { "Stavke": [] } }
        """);

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(putanja);

        Assert.False(rezultat.SmeSeUvesti);
        Assert.Contains(rezultat.Nalazi, n => n.Provera == "Novija verzija formata");
    }

    [Fact]
    public async Task NeispravanJson_PrijavljujeGresku()
    {
        using var db = NoviKontekst();

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(NapisiFajl("{ ovo nije json"));

        Assert.False(rezultat.SmeSeUvesti);
        Assert.Contains(rezultat.Nalazi, n => n.Provera == "Fajl se ne može pročitati");
    }

    [Fact]
    public async Task PrazanNalog_SeOdbija()
    {
        using var db = NoviKontekst();

        string putanja = NapisiFajl("""
        { "Format": "ERPi-nalog-za-knjizenje", "Verzija": 1, "Nalog": { "Stavke": [] } }
        """);

        var rezultat = await new ZaradeImportService(db).ProcitajAsync(putanja);

        Assert.False(rezultat.SmeSeUvesti);
        Assert.Contains(rezultat.Nalazi, n => n.Provera == "Nalog je prazan");
    }

    // ── Ponovljen uvoz ────────────────────────────────────────────────

    /// <summary>
    /// Ponovljen uvoz se ne zabranjuje — legitiman je kad se obračun ispravi — nego se
    /// pokazuje pre potvrde, da isti nalog ne uđe u knjige dvaput nezapaženo.
    /// </summary>
    [Fact]
    public async Task PonovljenUvoz_SePrijavljujeKaoMogucDuplikat()
    {
        using var db = NoviKontekst();
        var service = new ZaradeImportService(db);

        var prvi = await service.ProcitajAsync(IspravanFajl());
        Assert.Null(prvi.MogucDuplikat);

        await service.UveziAsync(prvi.Nalog!);

        var drugi = await service.ProcitajAsync(IspravanFajl());

        Assert.NotNull(drugi.MogucDuplikat);
        Assert.Equal(prvi.Nalog!.BrojNaloga, drugi.MogucDuplikat!.BrojNaloga);

        // Uvoz i dalje sme da prođe — odluka je korisnikova.
        Assert.True(drugi.SmeSeUvesti);
    }

    /// <summary>Datum se čita po ISO zapisu, nezavisno od regionalnih podešavanja.</summary>
    [Fact]
    public async Task Datum_SeCitaNezavisnoOdRegionalnihPodesavanja()
    {
        var zatecena = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("sr-Latn-RS");

            using var db = NoviKontekst();
            var rezultat = await new ZaradeImportService(db).ProcitajAsync(IspravanFajl());

            Assert.Equal(new DateTime(2026, 6, 30), rezultat.Nalog!.DatumNaloga.Date);
        }
        finally
        {
            CultureInfo.CurrentCulture = zatecena;
        }
    }
}

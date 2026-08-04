using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class KalkulacijaServiceTests
{
    private AccountingDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public void Izracunaj_ShouldComputeSvegaTroskoviISvegaNabavno()
    {
        var k = new Kalkulacija
        {
            NabavnaVrednost = 100000m,
            TransportniTroskovi = 5000m,
            TroskoviUskladistenja = 1000m,
            UtovarIstovar = 2000m,
            TransportnoOsiguranje = 500m,
            OstaliTroskovi = 1500m,
            MarzaProcenat = 0m,
            PoreskaStopaProcenat = 0m
        };

        KalkulacijaService.Izracunaj(k);

        Assert.Equal(10000m, k.SvegaTroskovi);
        Assert.Equal(110000m, k.SvegaNabavno);
    }

    [Fact]
    public void Izracunaj_ShouldApplyMarzuIPorezNaSvegaNabavno()
    {
        var k = new Kalkulacija
        {
            NabavnaVrednost = 100000m,
            MarzaProcenat = 20m,   // 20% marža
            PoreskaStopaProcenat = 20m // 20% PDV
        };

        KalkulacijaService.Izracunaj(k);

        // SvegaNabavno = 100000 (nema troskova)
        // Razlika = 100000 * 0.20 = 20000
        // Porez = (100000 + 20000) * 0.20 = 24000
        // ProdajnaVrednost = 100000 + 20000 + 24000 = 144000
        Assert.Equal(100000m, k.SvegaNabavno);
        Assert.Equal(20000m, k.Razlika);
        Assert.Equal(24000m, k.Porez);
        Assert.Equal(144000m, k.ProdajnaVrednost);
    }

    [Fact]
    public void Izracunaj_BezMarzeIPoreza_ProdajnaJednakaSvegaNabavnom()
    {
        var k = new Kalkulacija { NabavnaVrednost = 5000m, TransportniTroskovi = 500m };

        KalkulacijaService.Izracunaj(k);

        Assert.Equal(0m, k.Razlika);
        Assert.Equal(0m, k.Porez);
        Assert.Equal(k.SvegaNabavno, k.ProdajnaVrednost);
    }

    [Fact]
    public async Task SaveKalkulaciju_ShouldPersistIzracunateVrednosti()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija
        {
            BrojKalkulacije = 1,
            NabavnaVrednost = 10000m,
            MarzaProcenat = 10m,
            PoreskaStopaProcenat = 20m
        };

        var saved = await service.SaveKalkulacijuAsync(k);

        Assert.Equal(1000m, saved.Razlika); // 10000*0.10
        Assert.Equal(2200m, saved.Porez);   // (10000+1000)*0.20
        Assert.Equal(13200m, saved.ProdajnaVrednost);
        Assert.False(saved.IsKnjizen);
    }

    [Fact]
    public async Task KnjiziKalkulaciju_ShouldThrow_WhenAlreadyKnjizena()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija { BrojKalkulacije = 2, NabavnaVrednost = 1000m };
        await service.SaveKalkulacijuAsync(k);

        await service.KnjiziKalkulacijuAsync(k.KalkulacijaId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.KnjiziKalkulacijuAsync(k.KalkulacijaId));
    }

    [Fact]
    public void IzracunajSaStavkama_RaspodeljujeTroskoveSrazmerno()
    {
        var k = new Kalkulacija
        {
            TransportniTroskovi = 1000m,
            MarzaProcenat = 10m,
            PoreskaStopaProcenat = 20m,
            Stavke = new List<KalkulacijaStavka>
            {
                new() { Kolicina = 10m, NabavnaCena = 600m }, // Iznos = 6000 (60%)
                new() { Kolicina = 10m, NabavnaCena = 400m }  // Iznos = 4000 (40%)
            }
        };

        KalkulacijaService.IzracunajSaStavkama(k);

        Assert.Equal(600m, k.Stavke[0].Troskovi);
        Assert.Equal(400m, k.Stavke[1].Troskovi);
        Assert.Equal(6600m, k.Stavke[0].NabavnaVrednost);
        Assert.Equal(4400m, k.Stavke[1].NabavnaVrednost);
        Assert.Equal(660m, k.Stavke[0].RazlikaIznos);
        Assert.Equal(440m, k.Stavke[1].RazlikaIznos);
        Assert.Equal(1452m, k.Stavke[0].PorezIznos);
        Assert.Equal(968m, k.Stavke[1].PorezIznos);
        Assert.Equal(871.2m, k.Stavke[0].ProdajnaCena);
        Assert.Equal(580.8m, k.Stavke[1].ProdajnaCena);

        Assert.Equal(10000m, k.NabavnaVrednost);
        Assert.Equal(11000m, k.SvegaNabavno);
        Assert.Equal(1100m, k.Razlika);
        Assert.Equal(2420m, k.Porez);
        Assert.Equal(14520m, k.ProdajnaVrednost);
    }

    [Fact]
    public void IzracunajSaStavkama_OstatakZaokruzivanjaIdeNaPoslednjuStavku()
    {
        var k = new Kalkulacija
        {
            TransportniTroskovi = 100m,
            Stavke = new List<KalkulacijaStavka>
            {
                new() { Kolicina = 1m, NabavnaCena = 1000m },
                new() { Kolicina = 1m, NabavnaCena = 1000m },
                new() { Kolicina = 1m, NabavnaCena = 1000m }
            }
        };

        KalkulacijaService.IzracunajSaStavkama(k);

        Assert.Equal(33.33m, k.Stavke[0].Troskovi);
        Assert.Equal(33.33m, k.Stavke[1].Troskovi);
        Assert.Equal(33.34m, k.Stavke[2].Troskovi);
        Assert.Equal(100m, k.Stavke.Sum(s => s.Troskovi));
    }

    [Fact]
    public async Task KnjiziKalkulaciju_SaStavkama_KnjiziURobnuKarticu()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija
        {
            BrojKalkulacije = 3,
            Datum = new DateTime(2026, 7, 26),
            SifraMagacina = "001",
            MarzaProcenat = 10m,
            PoreskaStopaProcenat = 20m,
            Stavke = new List<KalkulacijaStavka>
            {
                new() { SifraArtikla = "A1", Kolicina = 10m, NabavnaCena = 600m },
                new() { SifraArtikla = "A2", Kolicina = 10m, NabavnaCena = 400m }
            }
        };
        var saved = await service.SaveKalkulacijuAsync(k);
        decimal prodajnaCenaA1 = saved.Stavke[0].ProdajnaCena;
        decimal prodajnaCenaA2 = saved.Stavke[1].ProdajnaCena;

        await service.KnjiziKalkulacijuAsync(saved.KalkulacijaId);

        var karticaA1 = await db.MaterijalneKartice.SingleAsync(m => m.SifraMagacina == "001" && m.SifraArtikla == "A1");
        Assert.Equal(10m, karticaA1.Ulaz);
        Assert.Equal(10m, karticaA1.Stanje);
        Assert.Equal(prodajnaCenaA1, karticaA1.Cena);

        var karticaA2 = await db.MaterijalneKartice.SingleAsync(m => m.SifraMagacina == "001" && m.SifraArtikla == "A2");
        Assert.Equal(10m, karticaA2.Ulaz);
        Assert.Equal(prodajnaCenaA2, karticaA2.Cena);

        Assert.True(saved.IsKnjizen);
    }

    [Fact]
    public async Task KnjiziKalkulaciju_SaStavkamaBezMagacina_Baca()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija
        {
            BrojKalkulacije = 4,
            Stavke = new List<KalkulacijaStavka> { new() { SifraArtikla = "A1", Kolicina = 5m, NabavnaCena = 100m } }
        };
        var saved = await service.SaveKalkulacijuAsync(k);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.KnjiziKalkulacijuAsync(saved.KalkulacijaId));
    }

    /// <summary>
    /// Obrazac iz stvarnog naloga 410 / „KALK.3 OD 04.12.02" (ARHIBEL 2002):
    /// 1320 duguje 78.170,00 / 1329 potražuje 7.475,75 / 432209 potražuje 70.694,25.
    /// </summary>
    [Fact]
    public async Task KnjiziKalkulaciju_KnjiziRobuRazlikuIDobavljaca()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija
        {
            BrojKalkulacije = 3,
            Datum = new DateTime(2002, 12, 4),
            SifraDobavljaca = "432209",
            BrojRacuna = "RN-3",
            NabavnaVrednost = 70694.25m,
            MarzaProcenat = 10.5747m,
            PoreskaStopaProcenat = 0m
        };
        var saved = await service.SaveKalkulacijuAsync(k);

        await service.KnjiziKalkulacijuAsync(saved.KalkulacijaId);

        var proknjizena = await db.Kalkulacije.SingleAsync(x => x.KalkulacijaId == saved.KalkulacijaId);
        Assert.NotNull(proknjizena.NalogId);

        var nalog = await db.Nalozi.Include(n => n.Stavke).SingleAsync(n => n.NalogId == proknjizena.NalogId);
        Assert.Equal(nalog.UkupnoDuguje, nalog.UkupnoPotrazuje);

        var roba = nalog.Stavke.Single(s => s.BrojKonta == "1320");
        var razlika = nalog.Stavke.Single(s => s.BrojKonta == "1329");
        var dobavljac = nalog.Stavke.Single(s => s.BrojKonta == "432209");

        // Roba u veleprodaji ide po prodajnoj vrednosti BEZ poreza — nabavno + razlika.
        Assert.Equal(saved.SvegaNabavno + saved.Razlika, roba.Duguje);
        Assert.Equal(saved.Razlika, razlika.Potrazuje);
        Assert.Equal(saved.SvegaNabavno, dobavljac.Potrazuje);
        Assert.Equal("RN-3", dobavljac.BrojDokumenta);
    }

    [Fact]
    public async Task KnjiziKalkulaciju_VeleprodajaNeKnjiziUkalkulisaniPdv()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija
        {
            BrojKalkulacije = 7,
            SifraDobavljaca = "435082",
            NabavnaVrednost = 1000m,
            MarzaProcenat = 20m,
            PoreskaStopaProcenat = 20m
        };
        var saved = await service.SaveKalkulacijuAsync(k);
        Assert.Equal(240m, saved.Porez);   // porez postoji na dokumentu...

        await service.KnjiziKalkulacijuAsync(saved.KalkulacijaId);

        var nalog = await db.Nalozi.Include(n => n.Stavke).SingleAsync();
        // ...ali se ne ukalkuliše: veleprodaja nema 1344, a roba ide bez poreza.
        Assert.DoesNotContain(nalog.Stavke, s => s.BrojKonta == RobnaKonta.UkalkulisaniPdvMaloprodaja);
        Assert.Equal(1200m, nalog.Stavke.Single(s => s.BrojKonta == "1320").Duguje);
        Assert.Equal(nalog.UkupnoDuguje, nalog.UkupnoPotrazuje);
    }

    [Fact]
    public async Task RasknjiziKalkulaciju_UklanjaNalog()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        var k = new Kalkulacija { BrojKalkulacije = 9, SifraDobavljaca = "435082", NabavnaVrednost = 5000m, MarzaProcenat = 10m };
        var saved = await service.SaveKalkulacijuAsync(k);
        await service.KnjiziKalkulacijuAsync(saved.KalkulacijaId);
        Assert.Equal(1, await db.Nalozi.CountAsync());

        await service.RasknjiziKalkulacijuAsync(saved.KalkulacijaId);

        Assert.Equal(0, await db.Nalozi.CountAsync());
        Assert.Equal(0, await db.StavkeNaloga.CountAsync());
        var vracena = await db.Kalkulacije.SingleAsync(x => x.KalkulacijaId == saved.KalkulacijaId);
        Assert.Null(vracena.NalogId);
        Assert.False(vracena.IsKnjizen);
    }

    [Fact]
    public async Task KnjiziKalkulaciju_BezKontaDobavljaca_NePraviNalog()
    {
        using var db = CreateInMemoryDb();
        var service = new KalkulacijaService(db);

        // Kalkulacije iz starijeg DBF uvoza ume da nemaju dobavljača — bez protivstavke
        // nalog ne bi bio u ravnoteži, pa se knjiženje u GK preskače, a dokument se svejedno knjiži.
        var k = new Kalkulacija { BrojKalkulacije = 11, SifraDobavljaca = null, NabavnaVrednost = 5000m, MarzaProcenat = 10m };
        var saved = await service.SaveKalkulacijuAsync(k);

        await service.KnjiziKalkulacijuAsync(saved.KalkulacijaId);

        Assert.Equal(0, await db.Nalozi.CountAsync());
        var proknjizena = await db.Kalkulacije.SingleAsync(x => x.KalkulacijaId == saved.KalkulacijaId);
        Assert.True(proknjizena.IsKnjizen);
        Assert.Null(proknjizena.NalogId);
    }
}

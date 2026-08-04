using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

/// <summary>
/// Knjiženje maloprodajne kalkulacije u glavnu knjigu. Obrazac je preuzet iz stvarnih naloga
/// firme (opis stavke „KALKULACIJA NA MALO"), a ne iz opšteg Kontnog okvira — vidi
/// <see cref="RobnaKonta"/>.
/// </summary>
public class MaloprodajnaKalkulacijaKnjizenjeTests
{
    private static AccountingDbContext CreateInMemoryDb()
        => new(new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>
    /// Nalog 31 / „KALK 1 OD 02.02.2026": 1340 duguje 97.500,00, 1344 potražuje 16.250,00,
    /// 1348 potražuje 15.259,08, dobavljač potražuje ostatak (svega nabavno).
    /// </summary>
    [Fact]
    public async Task Knjizenje_PratiObrazacIzStvarnihNaloga()
    {
        using var db = CreateInMemoryDb();
        var service = new MaloprodajnaKalkulacijaService(db);

        var k = new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 1,
            Datum = new DateTime(2026, 2, 2),
            SifraDobavljaca = "435082",
            BrojRacuna = "ifvp-14",
            NabavnaVrednost = 65992.50m,
            MarzaProcenat = 23.12m,
            PoreskaStopaProcenat = 20m
        };
        var saved = await service.SaveKalkulacijuAsync(k);

        await service.KnjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);

        var proknjizena = await db.MaloprodajneKalkulacije.SingleAsync();
        Assert.NotNull(proknjizena.NalogId);

        var nalog = await db.Nalozi.Include(n => n.Stavke).SingleAsync(n => n.NalogId == proknjizena.NalogId);

        // Roba u prodavnici ide po ceni SA porezom — to je razlika u odnosu na veleprodaju.
        Assert.Equal(saved.ProdajnaVrednost, nalog.Stavke.Single(s => s.BrojKonta == "1340").Duguje);
        Assert.Equal(saved.Porez, nalog.Stavke.Single(s => s.BrojKonta == "1344").Potrazuje);
        Assert.Equal(saved.Razlika, nalog.Stavke.Single(s => s.BrojKonta == "1348").Potrazuje);
        Assert.Equal(saved.SvegaNabavno, nalog.Stavke.Single(s => s.BrojKonta == "435082").Potrazuje);

        Assert.Equal(nalog.UkupnoDuguje, nalog.UkupnoPotrazuje);
    }

    [Fact]
    public async Task Knjizenje_RazlikaIdeNa1348_a_ne_1349()
    {
        // U kontnom planu postoje i 1348 i 1349, ali sva zatečena knjiženja idu na 1348.
        using var db = CreateInMemoryDb();
        var service = new MaloprodajnaKalkulacijaService(db);

        var saved = await service.SaveKalkulacijuAsync(new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 2,
            SifraDobavljaca = "435082",
            NabavnaVrednost = 1000m,
            MarzaProcenat = 25m,
            PoreskaStopaProcenat = 20m
        });

        await service.KnjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);

        var nalog = await db.Nalozi.Include(n => n.Stavke).SingleAsync();
        Assert.Contains(nalog.Stavke, s => s.BrojKonta == "1348");
        Assert.DoesNotContain(nalog.Stavke, s => s.BrojKonta == "1349");
        Assert.DoesNotContain(nalog.Stavke, s => s.BrojKonta == "1329");
    }

    [Fact]
    public async Task Rasknjizavanje_UklanjaNalog()
    {
        using var db = CreateInMemoryDb();
        var service = new MaloprodajnaKalkulacijaService(db);

        var saved = await service.SaveKalkulacijuAsync(new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 3,
            SifraDobavljaca = "435082",
            NabavnaVrednost = 2000m,
            MarzaProcenat = 10m,
            PoreskaStopaProcenat = 20m
        });
        await service.KnjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);
        Assert.Equal(1, await db.Nalozi.CountAsync());

        await service.RasknjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);

        Assert.Equal(0, await db.Nalozi.CountAsync());
        Assert.Equal(0, await db.StavkeNaloga.CountAsync());
        var vracena = await db.MaloprodajneKalkulacije.SingleAsync();
        Assert.Null(vracena.NalogId);
        Assert.False(vracena.IsKnjizen);
    }

    [Fact]
    public async Task Knjizenje_BezKontaDobavljaca_NePraviNalog()
    {
        using var db = CreateInMemoryDb();
        var service = new MaloprodajnaKalkulacijaService(db);

        var saved = await service.SaveKalkulacijuAsync(new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 4,
            SifraDobavljaca = null,
            NabavnaVrednost = 2000m,
            MarzaProcenat = 10m,
            PoreskaStopaProcenat = 20m
        });

        await service.KnjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);

        Assert.Equal(0, await db.Nalozi.CountAsync());
        Assert.True((await db.MaloprodajneKalkulacije.SingleAsync()).IsKnjizen);
    }

    [Fact]
    public async Task Nivelacija_UMaloprodaji_KnjiziRazlikuNaMaloprodajniKonto()
    {
        // Ranije je konto razlike bio zakucan na 1329 i za maloprodajne magacine, pa je
        // razlika iz prodavnice završavala na veleprodajnom kontu.
        using var db = CreateInMemoryDb();

        var magacin = new Magacin { SifraMagacina = "030", NazivMagacina = "Prodavnica", VrstaMagacina = "Maloprodaja" };
        db.Magacini.Add(magacin);
        await db.SaveChangesAsync();

        var niv = new NivelacijaCena
        {
            BrojNivelacije = 1,
            DatumNivelacije = new DateTime(2026, 2, 2),
            SifraMagacina = "030",
            MagacinId = magacin.MagacinId,
            UkupnoRazlika = 1500m
        };
        db.NivelacijeCena.Add(niv);
        await db.SaveChangesAsync();

        await NivelacijaService.KnjiziNivelacijuAsync(db, niv.NivelacijaCenaId);

        var nalog = await db.Nalozi.Include(n => n.Stavke).SingleAsync();
        Assert.Equal(1500m, nalog.Stavke.Single(s => s.BrojKonta == "1340").Duguje);
        Assert.Equal(1500m, nalog.Stavke.Single(s => s.BrojKonta == "1348").Potrazuje);
        Assert.DoesNotContain(nalog.Stavke, s => s.BrojKonta == "1329");
    }

    [Fact]
    public async Task Knjizenje_NabavkaPravoUProdavnicu_ZaduzujeMagacinKojiPrima()
    {
        // Bez magacina koji daje (roba stiže od dobavljača, ne iz veleprodaje) knjiženje je
        // ranije bacalo grešku. Sada roba ULAZI u prodavnicu po maloprodajnoj ceni.
        using var db = CreateInMemoryDb();
        var service = new MaloprodajnaKalkulacijaService(db);

        var saved = await service.SaveKalkulacijuAsync(new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 5,
            SifraMagacinaPrima = "030",
            SifraMagacinaDaje = null,
            SifraDobavljaca = "432509",
            PoreskaStopaProcenat = 20m,
            MarzaProcenat = 23.12m,
            Stavke = new List<MaloprodajnaKalkulacijaStavka>
            {
                new() { RedniBroj = 1, SifraArtikla = "02060", Kolicina = 750m, NabavnaCena = 87.99m }
            }
        });

        await service.KnjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);

        var red = await db.MaterijalneKartice.SingleAsync();
        Assert.Equal("030", red.SifraMagacina);
        Assert.Equal(750m, red.Ulaz);
        Assert.Equal(0m, red.Izlaz);
        Assert.Equal(saved.Stavke[0].ProdajnaCena, red.Cena);
    }

    [Fact]
    public async Task Rasknjizavanje_NabavkeUProdavnicu_UklanjaRedIzMagacinaKojiPrima()
    {
        using var db = CreateInMemoryDb();
        var service = new MaloprodajnaKalkulacijaService(db);

        var saved = await service.SaveKalkulacijuAsync(new MaloprodajnaKalkulacija
        {
            BrojKalkulacije = 6,
            SifraMagacinaPrima = "030",
            SifraDobavljaca = "432509",
            PoreskaStopaProcenat = 20m,
            Stavke = new List<MaloprodajnaKalkulacijaStavka>
            {
                new() { RedniBroj = 1, SifraArtikla = "02060", Kolicina = 10m, NabavnaCena = 100m }
            }
        });
        await service.KnjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);
        Assert.Equal(1, await db.MaterijalneKartice.CountAsync());

        await service.RasknjiziKalkulacijuAsync(saved.MaloprodajnaKalkulacijaId);

        Assert.Equal(0, await db.MaterijalneKartice.CountAsync());
        Assert.False((await db.MaloprodajneKalkulacije.SingleAsync()).IsKnjizen);
    }

    [Fact]
    public async Task PrebaciUMaloprodaju_PrenosiZaglavljeIStavke()
    {
        using var db = CreateInMemoryDb();
        var vpService = new KalkulacijaService(db);

        var vp = await vpService.SaveKalkulacijuAsync(new Kalkulacija
        {
            BrojKalkulacije = 1,
            Datum = new DateTime(2026, 2, 2),
            SifraMagacina = "030",
            SifraDobavljaca = "432509",
            BrojRacuna = "ifvp-14",
            MarzaProcenat = 23.12m,
            PoreskaStopaProcenat = 20m,
            Stavke = new List<KalkulacijaStavka>
            {
                new() { RedniBroj = 1, SifraArtikla = "02060", Kolicina = 750m, NabavnaCena = 87.99m }
            }
        });
        decimal prodajnaPre = vp.ProdajnaVrednost;

        var mp = await vpService.PrebaciUMaloprodajuAsync(vp.KalkulacijaId);

        Assert.Equal(0, await db.Kalkulacije.CountAsync());
        Assert.Equal(0, await db.KalkulacijaStavke.CountAsync());

        var prebacena = await db.MaloprodajneKalkulacije.Include(k => k.Stavke).SingleAsync();
        Assert.Equal(1, prebacena.BrojKalkulacije);
        Assert.Equal("030", prebacena.SifraMagacinaPrima);   // MAG_PRIMA je prodavnica
        Assert.Null(prebacena.SifraMagacinaDaje);            // KALKULAC nema MAG_DAJE
        Assert.Equal("432509", prebacena.SifraDobavljaca);
        Assert.Equal("ifvp-14", prebacena.BrojRacuna);
        Assert.Equal(prodajnaPre, prebacena.ProdajnaVrednost);
        Assert.Single(prebacena.Stavke);
        Assert.Equal("02060", prebacena.Stavke[0].SifraArtikla);
        Assert.Equal(750m, prebacena.Stavke[0].Kolicina);
    }

    [Fact]
    public async Task PrebaciUMaloprodaju_UzimaMagacinIzRobneKarticeKadZaglavljeNema()
    {
        // Baze uvezene starijom verzijom nemaju MAG_PRIMA u zaglavlju, ali redovi kartice nose magacin.
        using var db = CreateInMemoryDb();
        var vpService = new KalkulacijaService(db);

        var vp = await vpService.SaveKalkulacijuAsync(new Kalkulacija
        {
            BrojKalkulacije = 7,
            SifraMagacina = null,
            SifraDobavljaca = "432509",
            Stavke = new List<KalkulacijaStavka> { new() { RedniBroj = 1, SifraArtikla = "02060", Kolicina = 5m, NabavnaCena = 100m } }
        });

        db.MaterijalneKartice.Add(new MaterijalnaKartica
        {
            SifraMagacina = "R-030",
            SifraArtikla = "02060",
            RedniBroj = 1,
            OpisPromene = "Kalkulacija7",   // stariji zapis, bez razmaka
            Ulaz = 5m
        });
        await db.SaveChangesAsync();

        var mp = await vpService.PrebaciUMaloprodajuAsync(vp.KalkulacijaId);

        Assert.Equal("R-030", mp.SifraMagacinaPrima);
    }

    [Fact]
    public async Task PrebaciSveUMaloprodaju_PrebacujeIOneBezMagacina()
    {
        using var db = CreateInMemoryDb();
        var vpService = new KalkulacijaService(db);

        foreach (int broj in new[] { 1, 2, 3 })
        {
            await vpService.SaveKalkulacijuAsync(new Kalkulacija
            {
                BrojKalkulacije = broj,
                SifraMagacina = null,          // zatečeno stanje: zaglavlje bez magacina
                SifraDobavljaca = "432509",
                NabavnaVrednost = 1000m * broj
            });
        }

        var (prebaceno, preskoceno) = await vpService.PrebaciSveUMaloprodajuAsync();

        Assert.Equal(3, prebaceno);
        Assert.Empty(preskoceno);
        Assert.Equal(0, await db.Kalkulacije.CountAsync());
        Assert.Equal(3, await db.MaloprodajneKalkulacije.CountAsync());
    }

    [Fact]
    public async Task PrebaciSveUMaloprodaju_PreskaceProknjizeneUGlavnojKnjizi()
    {
        using var db = CreateInMemoryDb();
        var vpService = new KalkulacijaService(db);

        var slobodna = await vpService.SaveKalkulacijuAsync(new Kalkulacija { BrojKalkulacije = 1, SifraDobavljaca = "432509", NabavnaVrednost = 1000m });
        var proknjizena = await vpService.SaveKalkulacijuAsync(new Kalkulacija { BrojKalkulacije = 2, SifraDobavljaca = "432509", NabavnaVrednost = 2000m });
        await vpService.KnjiziKalkulacijuAsync(proknjizena.KalkulacijaId);

        var (prebaceno, preskoceno) = await vpService.PrebaciSveUMaloprodajuAsync();

        Assert.Equal(1, prebaceno);
        Assert.Single(preskoceno);
        Assert.Equal(1, await db.Kalkulacije.CountAsync());   // proknjižena ostaje
        Assert.Equal(slobodna.BrojKalkulacije, (await db.MaloprodajneKalkulacije.SingleAsync()).BrojKalkulacije);
    }

    [Fact]
    public async Task PrebaciUMaloprodaju_OdbijaProknjizenuUGlavnojKnjizi()
    {
        using var db = CreateInMemoryDb();
        var vpService = new KalkulacijaService(db);

        var vp = await vpService.SaveKalkulacijuAsync(new Kalkulacija
        {
            BrojKalkulacije = 2,
            SifraDobavljaca = "432509",
            NabavnaVrednost = 1000m,
            MarzaProcenat = 10m
        });
        await vpService.KnjiziKalkulacijuAsync(vp.KalkulacijaId);

        // Nalog na veleprodajnim kontima ne sme da ostane iza prebačenog dokumenta.
        await Assert.ThrowsAsync<InvalidOperationException>(() => vpService.PrebaciUMaloprodajuAsync(vp.KalkulacijaId));
    }

    [Fact]
    public void VrstaMagacina_SeCitaIzNaziva()
    {
        // MAGACIN.DBF nema polje za vrstu — jedini trag je naziv (RACUNOPOL).
        Assert.Equal("Maloprodaja", DbfImportService.VrstaIzNaziva("Magacin maloprodaje"));
        Assert.Equal("Maloprodaja", DbfImportService.VrstaIzNaziva("PRODAVNICA BR.1"));
        Assert.Equal("Veleprodaja", DbfImportService.VrstaIzNaziva("Magacin VELEPRODAJE"));
        Assert.Equal("Veleprodaja", DbfImportService.VrstaIzNaziva("SUMSKO"));
    }

    [Fact]
    public async Task Nivelacija_UVeleprodaji_ZadrzavaVeleprodajnaKonta()
    {
        using var db = CreateInMemoryDb();

        var magacin = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište", VrstaMagacina = "Veleprodaja" };
        db.Magacini.Add(magacin);
        await db.SaveChangesAsync();

        var niv = new NivelacijaCena
        {
            BrojNivelacije = 2,
            SifraMagacina = "001",
            MagacinId = magacin.MagacinId,
            UkupnoRazlika = 800m
        };
        db.NivelacijeCena.Add(niv);
        await db.SaveChangesAsync();

        await NivelacijaService.KnjiziNivelacijuAsync(db, niv.NivelacijaCenaId);

        var nalog = await db.Nalozi.Include(n => n.Stavke).SingleAsync();
        Assert.Equal(800m, nalog.Stavke.Single(s => s.BrojKonta == "1320").Duguje);
        Assert.Equal(800m, nalog.Stavke.Single(s => s.BrojKonta == "1329").Potrazuje);
    }
}

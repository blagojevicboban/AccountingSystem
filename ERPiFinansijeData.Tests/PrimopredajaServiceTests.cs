using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class PrimopredajaServiceTests
{
    private static DbContextOptions<AccountingDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

    [Fact]
    public async Task KnjiziPrimopredajuAsync_IstaVrstaMagacina_NePraviNalogUGlavnojKnjizi()
    {
        string dbName = Guid.NewGuid().ToString();
        using var db = new AccountingDbContext(CreateOptions(dbName));

        var magDaje = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište A", VrstaMagacina = "Veleprodaja" };
        var magPrima = new Magacin { SifraMagacina = "002", NazivMagacina = "Stovarište B", VrstaMagacina = "Veleprodaja" };
        var artikal = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        db.Magacini.AddRange(magDaje, magPrima);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magDaje.SifraMagacina, artikal.SifraArtikla, new DateTime(2026, 1, 1), "Kalkulacija 1", 10m, 100m);

        var service = new PrimopredajaService(db);
        var nalog = new PrimopredajaNalog
        {
            BrojNaloga = 1,
            Datum = new DateTime(2026, 1, 5),
            SifraMagacinaDaje = magDaje.SifraMagacina,
            SifraMagacinaPrima = magPrima.SifraMagacina,
            Stavke = new List<PrimopredajaStavka> { new() { SifraArtikla = artikal.SifraArtikla, Kolicina = 4 } }
        };
        await service.SavePrimopredajuAsync(nalog);

        await service.KnjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);

        Assert.Null(nalog.NalogId);
        Assert.Empty(await db.Nalozi.ToListAsync());

        // Vrednost je prenesena 1:1 (bez PDV preračuna) jer su oba magacina iste vrste.
        var karticaPrima = await kartice.GetKarticaAsync(magPrima.SifraMagacina, artikal.SifraArtikla);
        var ulazniRed = Assert.Single(karticaPrima);
        Assert.Equal(400m, ulazniRed.Duguje); // 4 kom * 100 RSD
    }

    [Fact]
    public async Task KnjiziPrimopredajuAsync_VeleprodajaUMaloprodaju_DodajePdvIPraviNalog1320_1340()
    {
        string dbName = Guid.NewGuid().ToString();
        using var db = new AccountingDbContext(CreateOptions(dbName));

        var magVP = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište", VrstaMagacina = "Veleprodaja" };
        var magMP = new Magacin { SifraMagacina = "010", NazivMagacina = "Prodavnica", VrstaMagacina = "Maloprodaja" };
        var artikal = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        db.Magacini.AddRange(magVP, magMP);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        // VP magacin vodi robu bez PDV, po prodajnoj (već ukalkulisanoj) ceni 100 RSD/kom.
        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magVP.SifraMagacina, artikal.SifraArtikla, new DateTime(2026, 1, 1), "Kalkulacija 1", 10m, 100m);

        var service = new PrimopredajaService(db);
        var nalog = new PrimopredajaNalog
        {
            BrojNaloga = 1,
            VrstaDokumenta = "Zaduženje",
            Datum = new DateTime(2026, 1, 5),
            SifraMagacinaDaje = magVP.SifraMagacina,
            SifraMagacinaPrima = magMP.SifraMagacina,
            StopaPdv = 20m,
            Stavke = new List<PrimopredajaStavka> { new() { SifraArtikla = artikal.SifraArtikla, Kolicina = 4 } }
        };
        await service.SavePrimopredajuAsync(nalog);

        await service.KnjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);

        // 4 kom * 100 RSD = 400 RSD bez PDV → 480 RSD sa 20% PDV u prodavnici.
        var karticaMP = await kartice.GetKarticaAsync(magMP.SifraMagacina, artikal.SifraArtikla);
        var ulazniRed = Assert.Single(karticaMP);
        Assert.Equal(480m, ulazniRed.Duguje);

        Assert.NotNull(nalog.NalogId);
        var glavniNalog = await db.Nalozi.Include(n => n.Stavke).FirstAsync(n => n.NalogId == nalog.NalogId);

        var stavka1320 = Assert.Single(glavniNalog.Stavke, s => s.BrojKonta == RobnaKonta.RobaVeleprodaja);
        Assert.Equal(400m, stavka1320.Potrazuje);

        var stavka1340 = Assert.Single(glavniNalog.Stavke, s => s.BrojKonta == RobnaKonta.RobaMaloprodaja);
        Assert.Equal(480m, stavka1340.Duguje);

        var stavkaPdv = Assert.Single(glavniNalog.Stavke, s => s.BrojKonta == RobnaKonta.UkalkulisaniPdvMaloprodaja);
        Assert.Equal(80m, stavkaPdv.Potrazuje);

        Assert.Equal(glavniNalog.Stavke.Sum(s => s.Duguje), glavniNalog.Stavke.Sum(s => s.Potrazuje));
    }

    [Fact]
    public async Task RasknjiziPrimopredajuAsync_UklanjaKarticeINalogUGlavnojKnjizi()
    {
        string dbName = Guid.NewGuid().ToString();
        using var db = new AccountingDbContext(CreateOptions(dbName));

        var magVP = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište", VrstaMagacina = "Veleprodaja" };
        var magMP = new Magacin { SifraMagacina = "010", NazivMagacina = "Prodavnica", VrstaMagacina = "Maloprodaja" };
        var artikal = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        db.Magacini.AddRange(magVP, magMP);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magVP.SifraMagacina, artikal.SifraArtikla, new DateTime(2026, 1, 1), "Kalkulacija 1", 10m, 100m);

        var service = new PrimopredajaService(db);
        var nalog = new PrimopredajaNalog
        {
            BrojNaloga = 1,
            VrstaDokumenta = "Zaduženje",
            Datum = new DateTime(2026, 1, 5),
            SifraMagacinaDaje = magVP.SifraMagacina,
            SifraMagacinaPrima = magMP.SifraMagacina,
            StopaPdv = 20m,
            Stavke = new List<PrimopredajaStavka> { new() { SifraArtikla = artikal.SifraArtikla, Kolicina = 4 } }
        };
        await service.SavePrimopredajuAsync(nalog);
        await service.KnjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);

        await service.RasknjiziPrimopredajuAsync(nalog.PrimopredajaNalogId);

        var karticaVP = await kartice.GetKarticaAsync(magVP.SifraMagacina, artikal.SifraArtikla);
        Assert.Single(karticaVP); // samo prvobitni ulaz
        Assert.Equal(10m, karticaVP[0].Stanje);

        var karticaMP = await kartice.GetKarticaAsync(magMP.SifraMagacina, artikal.SifraArtikla);
        Assert.Empty(karticaMP);

        Assert.False(nalog.IsKnjizen);
        Assert.Null(nalog.NalogId);
        Assert.Empty(await db.Nalozi.ToListAsync());
    }
}

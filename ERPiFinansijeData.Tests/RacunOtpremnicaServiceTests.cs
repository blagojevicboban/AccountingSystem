using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class RacunOtpremnicaServiceTests
{
    private static DbContextOptions<AccountingDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

    [Fact]
    public async Task SaveRacunAsync_ShouldPersistBrojOtpremniceRokPlacanjaNacinPlacanjaIKontoKupca_AfterReload()
    {
        string dbName = Guid.NewGuid().ToString();

        int racunId;
        using (var db = new AccountingDbContext(CreateOptions(dbName)))
        {
            var service = new RacunOtpremnicaService(db);
            var racun = new RacunOtpremnica
            {
                BrojRacuna = 1,
                BrojOtpremnice = "OTP-777",
                KontoKupca = "TESTKUPAC", // ne odgovara nijednom postojećem Partneru
                RokPlacanjaDana = 45,
                NacinPlacanja = "Gotovina",
                Stavke = new List<RacunOtpremnicaStavka>
                {
                    new() { SifraArtikla = "A1", Kolicina = 2, Cena = 100m, PdvProcenat = 20m }
                }
            };

            await service.SaveRacunAsync(racun);
            racunId = racun.RacunOtpremnicaId;
        }

        // Sveže učitavanje iz "baze" (nova instanca DbContext-a) — dokazuje da vrednosti
        // nisu samo zadržane u memoriji istog objekta, već stvarno upisane u bazu.
        using (var dbFresh = new AccountingDbContext(CreateOptions(dbName)))
        {
            var service = new RacunOtpremnicaService(dbFresh);
            var ucitan = await service.GetRacunByIdAsync(racunId);

            Assert.NotNull(ucitan);
            Assert.Equal("OTP-777", ucitan!.BrojOtpremnice);
            Assert.Equal("TESTKUPAC", ucitan.KontoKupca);
            Assert.Equal(45, ucitan.RokPlacanjaDana);
            Assert.Equal("Gotovina", ucitan.NacinPlacanja);
        }
    }

    [Fact]
    public async Task KnjiziRacunAsync_ShouldRazduziteMagacinIKnjiziNabavnuVrednost_NaKontu5010()
    {
        string dbName = Guid.NewGuid().ToString();

        using var db = new AccountingDbContext(CreateOptions(dbName));
        var magacin = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište", VrstaMagacina = "Veleprodaja" };
        var artikal = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        db.Magacini.Add(magacin);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        // Prethodni ulaz na zalihu — 10 kom po 100 RSD (nabavno), da bi imalo šta da se razduži.
        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magacin.SifraMagacina, artikal.SifraArtikla, new DateTime(2026, 1, 1), "Ulazna kalkulacija", 10m, 100m);

        var service = new RacunOtpremnicaService(db);
        var racun = new RacunOtpremnica
        {
            BrojRacuna = 1,
            DatumRacuna = new DateTime(2026, 1, 5),
            KontoKupca = "204100",
            MagacinId = magacin.MagacinId,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new() { ArtikalId = artikal.ArtikalId, SifraArtikla = artikal.SifraArtikla, Kolicina = 4, Cena = 150m, PdvProcenat = 20m }
            }
        };
        await service.SaveRacunAsync(racun);

        await service.KnjiziRacunAsync(racun.RacunOtpremnicaId);

        // Robna kartica razdužena po prosečnoj (nabavnoj) ceni — 4 kom * 100 RSD = 400 RSD, ne po prodajnoj.
        var karticaRedovi = await kartice.GetKarticaAsync(magacin.SifraMagacina, artikal.SifraArtikla);
        Assert.Equal(2, karticaRedovi.Count);
        var izlazniRed = karticaRedovi[^1];
        Assert.Equal(4m, izlazniRed.Izlaz);
        Assert.Equal(400m, izlazniRed.Potrazuje);
        Assert.Equal(6m, izlazniRed.Stanje);

        var proknjizeni = await service.GetRacunByIdAsync(racun.RacunOtpremnicaId);
        Assert.NotNull(proknjizeni!.NalogId);
        var nalog = await db.Nalozi.Include(n => n.Stavke).FirstAsync(n => n.NalogId == proknjizeni.NalogId);

        var stavka5010 = Assert.Single(nalog.Stavke, s => s.BrojKonta == "5010");
        Assert.Equal(400m, stavka5010.Duguje);

        var stavkaRobe = Assert.Single(nalog.Stavke, s => s.BrojKonta == RobnaKonta.RobaVeleprodaja);
        Assert.Equal(400m, stavkaRobe.Potrazuje);

        Assert.Equal(nalog.Stavke.Sum(s => s.Duguje), nalog.Stavke.Sum(s => s.Potrazuje));
    }

    [Fact]
    public async Task RasknjiziRacunAsync_ShouldUkloniKarticuIVratiZalihu()
    {
        string dbName = Guid.NewGuid().ToString();

        using var db = new AccountingDbContext(CreateOptions(dbName));
        var magacin = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište", VrstaMagacina = "Veleprodaja" };
        var artikal = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        db.Magacini.Add(magacin);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        var kartice = new MaterijalnaKarticaService(db);
        await kartice.DodajUlazRedAsync(magacin.SifraMagacina, artikal.SifraArtikla, new DateTime(2026, 1, 1), "Ulazna kalkulacija", 10m, 100m);

        var service = new RacunOtpremnicaService(db);
        var racun = new RacunOtpremnica
        {
            BrojRacuna = 1,
            DatumRacuna = new DateTime(2026, 1, 5),
            KontoKupca = "204100",
            MagacinId = magacin.MagacinId,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new() { ArtikalId = artikal.ArtikalId, SifraArtikla = artikal.SifraArtikla, Kolicina = 4, Cena = 150m, PdvProcenat = 20m }
            }
        };
        await service.SaveRacunAsync(racun);
        await service.KnjiziRacunAsync(racun.RacunOtpremnicaId);

        await service.RasknjiziRacunAsync(racun.RacunOtpremnicaId);

        var karticaRedovi = await kartice.GetKarticaAsync(magacin.SifraMagacina, artikal.SifraArtikla);
        Assert.Single(karticaRedovi); // samo prvobitni ulaz je ostao
        Assert.Equal(10m, karticaRedovi[0].Stanje);

        var rasknjizeni = await service.GetRacunByIdAsync(racun.RacunOtpremnicaId);
        Assert.False(rasknjizeni!.IsKnjizen);
        Assert.Null(rasknjizeni.NalogId);
        Assert.Empty(await db.Nalozi.ToListAsync());
    }
}

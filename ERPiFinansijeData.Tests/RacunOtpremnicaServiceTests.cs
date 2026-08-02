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
}

using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

public class EsirFiskalizacijaTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task FiskalizujRacunAsync_UspesnoKreiraLogIPopunjavaFiskalnaPolja()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new EsirFiskalizacijaService(db);

        var artikal = new Artikal { ArtikalId = 1, SifraArtikla = "A001", Naziv = "Artikal Test", ProdajnaCena = 1000m };
        db.Artikli.Add(artikal);

        var racun = new RacunOtpremnica
        {
            RacunOtpremnicaId = 1,
            BrojRacuna = 100,
            UkupnoZaUplatu = 1200m,
            UkupnoOsnovica = 1000m,
            UkupnoPdv = 200m,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new RacunOtpremnicaStavka
                {
                    ArtikalId = 1,
                    Kolicina = 1,
                    ProdajnaCena = 1000m,
                    StopaPdv = 20m,
                    Osnovica = 1000m,
                    IznosPdv = 200m,
                    Ukupno = 1200m
                }
            }
        };

        db.RacuniOtpremnice.Add(racun);
        await db.SaveChangesAsync();

        // Act
        var (success, message, log) = await service.FiskalizujRacunAsync(1, "Cash");

        // Assert
        Assert.True(success);
        Assert.NotNull(log);
        Assert.NotEmpty(log.InvoiceNumber);
        Assert.NotEmpty(log.VerificationUrl);

        var azurirani = await db.RacuniOtpremnice.FindAsync(1);
        Assert.NotNull(azurirani);
        Assert.Equal(FiskalniStatus.Fiskalizovan, azurirani.FiskalniStatus);
        Assert.Equal(log.InvoiceNumber, azurirani.FiskalniBroj);
    }
}

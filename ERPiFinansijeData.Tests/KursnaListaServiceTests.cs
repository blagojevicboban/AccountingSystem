using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class KursnaListaServiceTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task PretvoriDevizeURsdAsync_RacunaTacnuDinarskuProtivvrednost()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new KursnaListaService(db);

        var datum = new DateTime(2026, 8, 1);
        db.KursneListeStavke.Add(new KursnaListaStavka
        {
            Datum = datum.Date,
            ValutaOznaka = "EUR",
            NazivValute = "Evro",
            Jedinica = 1,
            SrednjiKurs = 117.1850m
        });
        await db.SaveChangesAsync();

        // Act
        decimal rsd = await service.PretvoriDevizeURsdAsync(100m, "EUR", datum);

        // Assert
        Assert.Equal(11718.50m, rsd);
    }
}

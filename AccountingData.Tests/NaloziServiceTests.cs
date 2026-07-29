using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

public class NaloziServiceTests
{
    private AccountingDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task SaveNalog_ShouldCalculateTotalsCorrectly()
    {
        using var db = CreateInMemoryDb();
        var service = new NaloziService(db);

        var nalog = new Nalog
        {
            BrojNaloga = 1001,
            DatumNaloga = DateTime.Now,
            VrstaNaloga = "Finansijski",
            Opis = "Test Nalog",
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { RedniBroj = 1, BrojKonta = "2413", Duguje = 1000m, Potrazuje = 0m, Opis = "Uplata" },
                new StavkaNaloga { RedniBroj = 2, BrojKonta = "2040", Duguje = 0m, Potrazuje = 1000m, Opis = "Zaduženje kupca" }
            }
        };

        var saved = await service.SaveNalogAsync(nalog);

        Assert.Equal(1000m, saved.UkupnoDuguje);
        Assert.Equal(1000m, saved.UkupnoPotrazuje);
        Assert.Equal(0m, saved.Saldo);
        Assert.True(saved.IsUuravnotezen);
    }

    [Fact]
    public async Task KnjiziNalog_ShouldThrow_WhenNalogIsNotBalanced()
    {
        using var db = CreateInMemoryDb();
        var service = new NaloziService(db);

        var nalog = new Nalog
        {
            BrojNaloga = 1002,
            DatumNaloga = DateTime.Now,
            Opis = "Neuravnotežen nalog",
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { RedniBroj = 1, BrojKonta = "2413", Duguje = 1500m, Potrazuje = 0m }
            }
        };

        await service.SaveNalogAsync(nalog);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.KnjiziNalogAsync(nalog.NalogId));
    }
}

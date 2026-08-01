using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

public class PoreskiBilansTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task GenerisiPoreskiBilansPb1Async_UspesnoUskladjujeDobitIPorezNaDobit15Odsto()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new PoreskiBilansService(db);

        var nalog = new Nalog { NalogId = 1, DatumNaloga = new DateTime(2026, 6, 1), IsKnjizen = true };
        db.Nalozi.Add(nalog);

        // Prihodi (Konto 602 = 100.000 RSD)
        db.StavkeNaloga.Add(new StavkaNaloga { NalogId = 1, BrojKonta = "6020", Potrazuje = 100000m });
        // Rashodi (Konto 501 = 40.000 RSD, Kazne 5560 = 5.000 RSD nepriznato)
        db.StavkeNaloga.Add(new StavkaNaloga { NalogId = 1, BrojKonta = "5010", Duguje = 40000m });
        db.StavkeNaloga.Add(new StavkaNaloga { NalogId = 1, BrojKonta = "5560", Duguje = 5000m });

        await db.SaveChangesAsync();

        // Act
        var (pb1, oporezivaDobit, obracunatiPorez) = await service.GenerisiPoreskiBilansPb1Async(2026);

        // Assert
        Assert.NotEmpty(pb1);
        Assert.True(oporezivaDobit > 0m);
        Assert.Equal(Math.Round(oporezivaDobit * 0.15m, 2), obracunatiPorez);
    }

    [Fact]
    public async Task GenerisiAprProsireneIzvestaje_UspesnoKreirajuSIiCashFlow()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new AprProsireniIzvestajiService(db);

        // Act
        var si = await service.GenerisiStatistickiIzvestajAsync(2026);
        var cashFlow = await service.GenerisiCashFlowAsync(2026);
        var kapital = await service.GenerisiPromeneNaKapitaluAsync(2026);

        // Assert
        Assert.NotEmpty(si);
        Assert.NotEmpty(cashFlow);
        Assert.NotEmpty(kapital);
    }
}

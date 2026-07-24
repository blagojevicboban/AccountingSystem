using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

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
            BrojKalkulacije = "K-1",
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

        var k = new Kalkulacija { BrojKalkulacije = "K-2", NabavnaVrednost = 1000m };
        await service.SaveKalkulacijuAsync(k);

        await service.KnjiziKalkulacijuAsync(k.KalkulacijaId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.KnjiziKalkulacijuAsync(k.KalkulacijaId));
    }
}

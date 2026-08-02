using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class BilansServiceTests
{
    private AccountingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task GetBilansStanja_UravnotezeniNalozi_VracaRavnotezuAktiveIPasive()
    {
        using var db = CreateInMemoryDbContext();

        // Nalog 1: Početno stanje - Osnovna sredstva (0200) Duguje 100.000, Kapital (3000) Potražuje 100.000
        var nalog1 = new Nalog
        {
            BrojNaloga = 1,
            DatumNaloga = new DateTime(2026, 1, 1),
            IsKnjizen = true,
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { BrojKonta = "0200", Duguje = 100000m, Potrazuje = 0m },
                new StavkaNaloga { BrojKonta = "3000", Duguje = 0m, Potrazuje = 100000m }
            }
        };

        // Nalog 2: Kupovina robe (1300) Duguje 50.000, Dobavljači (4350) Potražuje 50.000
        var nalog2 = new Nalog
        {
            BrojNaloga = 2,
            DatumNaloga = new DateTime(2026, 1, 15),
            IsKnjizen = true,
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { BrojKonta = "1300", Duguje = 50000m, Potrazuje = 0m },
                new StavkaNaloga { BrojKonta = "4350", Duguje = 0m, Potrazuje = 50000m }
            }
        };

        db.Nalozi.AddRange(nalog1, nalog2);
        await db.SaveChangesAsync();

        var service = new BilansService(db);
        var bilansStanja = await service.GetBilansStanjaAsync();

        var ukAktiva = bilansStanja.First(p => p.AopCode == "0010").IznosTekucaGodina;
        var ukPasiva = bilansStanja.First(p => p.AopCode == "0410").IznosTekucaGodina;

        Assert.Equal(150000m, ukAktiva);
        Assert.Equal(150000m, ukPasiva);
        Assert.Equal(ukAktiva, ukPasiva);
    }

    [Fact]
    public async Task GetBilansUspeha_PrihodiIRashodi_TacnoRacunaNetoRezultat()
    {
        using var db = CreateInMemoryDbContext();

        // Nalog 1: Prihod od prodaje (6120) Potražuje 120.000, Kupci (2040) Duguje 120.000
        // Nalog 2: Nabavna vrednost robe (5010) Duguje 70.000, Roba (1300) Potražuje 70.000
        var nalog = new Nalog
        {
            BrojNaloga = 10,
            DatumNaloga = new DateTime(2026, 2, 1),
            IsKnjizen = true,
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { BrojKonta = "2040", Duguje = 120000m, Potrazuje = 0m },
                new StavkaNaloga { BrojKonta = "6120", Duguje = 0m, Potrazuje = 120000m },
                new StavkaNaloga { BrojKonta = "5010", Duguje = 70000m, Potrazuje = 0m },
                new StavkaNaloga { BrojKonta = "1300", Duguje = 0m, Potrazuje = 70000m }
            }
        };

        db.Nalozi.Add(nalog);
        await db.SaveChangesAsync();

        var service = new BilansService(db);
        var bilansUspeha = await service.GetBilansUspehaAsync();

        var ukPrihodi = bilansUspeha.First(p => p.AopCode == "1005").IznosTekucaGodina;
        var ukRashodi = bilansUspeha.First(p => p.AopCode == "1018").IznosTekucaGodina;
        var netoDobitak = bilansUspeha.First(p => p.AopCode == "1030").IznosTekucaGodina;

        Assert.Equal(120000m, ukPrihodi);
        Assert.Equal(70000m, ukRashodi);
        Assert.Equal(50000m, netoDobitak);
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

public class OtvoreneStavkeServiceTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task GetIosIzvestajAsync_FiltersByKontoRangeAndCalculatesBalances()
    {
        using var db = GetInMemoryDbContext();

        var p1 = new Partner { PartnerId = 1, SifraPartnera = "P001", Naziv = "Kupac Alpha", KontoPartnera = "2020" };
        var p2 = new Partner { PartnerId = 2, SifraPartnera = "P002", Naziv = "Dobavljač Beta", KontoPartnera = "4350" };

        db.Partneri.AddRange(p1, p2);

        var n1 = new Nalog { NalogId = 1, BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 15), IsKnjizen = true, Opis = "Faktura Alpha" };
        var n2 = new Nalog { NalogId = 2, BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 20), IsKnjizen = true, Opis = "Uplata Alpha" };
        var n3 = new Nalog { NalogId = 3, BrojNaloga = 103, DatumNaloga = new DateTime(2026, 1, 22), IsKnjizen = true, Opis = "Faktura Beta" };

        db.Nalozi.AddRange(n1, n2, n3);

        db.StavkeNaloga.AddRange(
            new StavkaNaloga { StavkaNalogaId = 1, NalogId = 1, RedniBroj = 1, BrojKonta = "2020", PartnerId = 1, Duguje = 10000m, Potrazuje = 0m, Opis = "Račun 101" },
            new StavkaNaloga { StavkaNalogaId = 2, NalogId = 2, RedniBroj = 1, BrojKonta = "2020", PartnerId = 1, Duguje = 0m, Potrazuje = 4000m, Opis = "Izvod 5" },
            new StavkaNaloga { StavkaNalogaId = 3, NalogId = 3, RedniBroj = 1, BrojKonta = "4350", PartnerId = 2, Duguje = 0m, Potrazuje = 25000m, Opis = "Ulazni račun 4" }
        );

        await db.SaveChangesAsync();

        var service = new OtvoreneStavkeService(db);

        // Test 1: Fetch IOS for range 202 to 2029999 (default as in legacy gk91)
        var resultKupci = await service.GetIosIzvestajAsync(odKonta: "202", doKonta: "2029999", samoSaSaldom: true);

        Assert.Single(resultKupci);
        var grupa1 = resultKupci.First();
        Assert.Equal("2020", grupa1.Konto);
        Assert.Equal("P001", grupa1.SifraPartnera);
        Assert.Equal(2, grupa1.Stavke.Count);
        Assert.Equal(6000m, grupa1.Saldo);

        // Test 2: Fetch IOS for all konta
        var resultSvi = await service.GetIosIzvestajAsync(odKonta: null, doKonta: null, samoSaSaldom: true);
        Assert.Equal(2, resultSvi.Count);
    }
}

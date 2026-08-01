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

    [Fact]
    public async Task RasknjiziNalog_ShouldClearKnjizenFlag_AndWriteAuditEntry()
    {
        using var db = CreateInMemoryDb();
        var service = new NaloziService(db);

        var nalog = new Nalog
        {
            BrojNaloga = 1003,
            DatumNaloga = DateTime.Now,
            Opis = "Za rasknjizavanje",
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { RedniBroj = 1, BrojKonta = "2413", Duguje = 500m, Potrazuje = 0m },
                new StavkaNaloga { RedniBroj = 2, BrojKonta = "2040", Duguje = 0m, Potrazuje = 500m }
            }
        };
        await service.SaveNalogAsync(nalog);
        await service.KnjiziNalogAsync(nalog.NalogId);

        var rezultat = await service.RasknjiziNalogAsync(nalog.NalogId, korisnikId: 7, korisnickoIme: "petar");

        Assert.True(rezultat);
        var osvezeni = await service.GetNalogByIdAsync(nalog.NalogId);
        Assert.False(osvezeni!.IsKnjizen);
        Assert.Null(osvezeni.DatumKnjiženja);

        var audit = Assert.Single(db.NalogAuditi);
        Assert.Equal(nalog.NalogId, audit.NalogId);
        Assert.Equal("Rasknjizavanje", audit.Akcija);
        Assert.Equal(7, audit.KorisnikId);
        Assert.Equal("petar", audit.KorisnickoIme);
    }

    [Fact]
    public async Task RasknjiziNalog_ShouldThrow_WhenNarednaGodinaImaPrenosPocetnogStanja()
    {
        using var db = CreateInMemoryDb();
        var service = new NaloziService(db);

        var stariNalog = new Nalog
        {
            BrojNaloga = 2001,
            DatumNaloga = new DateTime(2025, 6, 15),
            Opis = "Nalog iz zaključene godine",
            Stavke = new List<StavkaNaloga>
            {
                new StavkaNaloga { RedniBroj = 1, BrojKonta = "2413", Duguje = 200m, Potrazuje = 0m },
                new StavkaNaloga { RedniBroj = 2, BrojKonta = "2040", Duguje = 0m, Potrazuje = 200m }
            }
        };
        await service.SaveNalogAsync(stariNalog);
        await service.KnjiziNalogAsync(stariNalog.NalogId);

        db.Nalozi.Add(new Nalog
        {
            BrojNaloga = 3001,
            DatumNaloga = new DateTime(2026, 1, 1),
            VrstaNaloga = "PrenosPocetnogStanja",
            Opis = "Prenos početnog stanja 2026",
            IsKnjizen = true
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RasknjiziNalogAsync(stariNalog.NalogId));
    }
}

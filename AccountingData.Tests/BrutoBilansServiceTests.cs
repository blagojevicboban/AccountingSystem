using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace AccountingData.Tests;

public class BrutoBilansServiceTests
{
    private readonly ITestOutputHelper _output;

    public BrutoBilansServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestBrutoBilansCalculations()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: "BrutoBilansTestDb")
            .Options;

        using (var db = new AccountingDbContext(options))
        {
            db.Konta.Add(new Konto { BrojKonta = "1010/35", NazivKonta = "TIGAR TYRES" });
            db.Konta.Add(new Konto { BrojKonta = "1010/47", NazivKonta = "MAGACIN ZONA" });
            db.Konta.Add(new Konto { BrojKonta = "1033", NazivKonta = "ALAT I INVENTAR" });

            var nalog = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 15), IsKnjizen = true };
            nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 1, BrojKonta = "1010/35", Duguje = 529057.02m, Potrazuje = 0m });
            nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 2, BrojKonta = "1010/47", Duguje = 2664864.09m, Potrazuje = 0m });
            nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 3, BrojKonta = "1033", Duguje = 6566210.23m, Potrazuje = 0m });
            nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 4, BrojKonta = "1010/35", Duguje = 0m, Potrazuje = 529057.02m });

            db.Nalozi.Add(nalog);
            await db.SaveChangesAsync();

            var service = new BrutoBilansService(db);
            var result = await service.GetBrutoBilansSaTotalimaAsync();

            Assert.NotEmpty(result);

            var r1010_35 = result.FirstOrDefault(r => r.BrojKonta == "1010/35");
            Assert.NotNull(r1010_35);
            Assert.Equal(529057.02m, r1010_35.Duguje);
            Assert.Equal(529057.02m, r1010_35.Potrazuje);
            Assert.Equal(0m, r1010_35.SaldoDuguje);
            Assert.Equal(0m, r1010_35.SaldoPotrazuje);

            var total101 = result.FirstOrDefault(r => r.Tip == BrutoBilansRedTip.SintetikaTotal && r.NazivKonta.Contains("101"));
            Assert.NotNull(total101);
            Assert.Equal(2664864.09m, total101.SaldoDuguje);
        }
    }
}



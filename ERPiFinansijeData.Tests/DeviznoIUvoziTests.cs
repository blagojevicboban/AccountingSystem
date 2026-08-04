using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class DeviznoIUvoziTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task DeviznoService_ValviranjeKonta_UspesnoRacunaKursneRazlike()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var service = new DeviznoKnjigovodstvoService(db);

        var nalog = new Nalog { NalogId = 1, DatumNaloga = new DateTime(2026, 12, 1), IsKnjizen = true };
        db.Nalozi.Add(nalog);

        // Knjiženje deviznog potraživanja: 1.000 EUR po kursu 117.00 RSD = 117.000 RSD
        db.StavkeNaloga.Add(new StavkaNaloga
        {
            NalogId = 1,
            BrojKonta = "2040",
            Valuta = "EUR",
            KursValute = 117.00m,
            DevizniDuguje = 1000m,
            Duguje = 117000m
        });

        await db.SaveChangesAsync();

        // Act — Valviranje na dan 31.12. sa novim kursom 117.50 RSD
        var rezultati = await service.ObracunajValviranjeAsync(new DateTime(2026, 12, 31), tekuciKursEur: 117.50m);

        // Assert
        Assert.Single(rezultati);
        var r = rezultati.First();
        Assert.Equal("2040", r.BrojKonta);
        Assert.Equal(1000m, r.DevizniSaldo);
        Assert.Equal(117000m, r.KnjigovodstveniSaldoRsd);
        Assert.Equal(117500m, r.ValviraniSaldoRsd);
        Assert.Equal(500m, r.KursnaRazlikaRsd); // Pozitivna kursna razlika 500 RSD
    }

    [Fact]
    public void UvoznaKalkulacijaService_ProracunZavisnihTroskova_UspesnoRasporedjujeCarinuITroskove()
    {
        // Arrange
        var service = new UvoznaKalkulacijaService(null!);
        var kalkulacija = new UvoznaKalkulacija
        {
            Valuta = "EUR",
            KursValute = 117.00m,
            SpedicijaRsd = 10000m,
            PrevozRsd = 20000m,
            Stavke = new List<UvoznaStavka>
            {
                new UvoznaStavka { ArtikalId = 1, Kolicina = 100, InoCenaDevize = 10m, CarinaProcenat = 10m }, // 1000 EUR
                new UvoznaStavka { ArtikalId = 2, Kolicina = 100, InoCenaDevize = 30m, CarinaProcenat = 5m }   // 3000 EUR
            }
        };

        // Act
        service.ProracunajUvoznuKalkulaciju(kalkulacija);

        // Assert
        Assert.Equal(4000m, kalkulacija.UkupnoDevize);
        Assert.Equal(468000m, kalkulacija.UkupnoFakturaRsd); // 4000 * 117
        Assert.Equal(30000m, kalkulacija.SpedicijaRsd + kalkulacija.PrevozRsd);

        // Udeo prve stavke (1000/4000 = 25%) -> 7.500 RSD zavisnih troškova, carina 10% na 117.000 = 11.700 RSD
        var s1 = kalkulacija.Stavke[0];
        Assert.Equal(11700m, s1.CarinaIznosRsd);
        Assert.Equal(7500m, s1.RasporedjeniZavisniTroskoviRsd);
        Assert.Equal(136200m, s1.UkupnaNabavnaVrednostRsd); // 117000 + 11700 + 7500
        Assert.Equal(1362m, s1.NabavnaCenaPoJediniciRsd);   // 136.200 / 100
    }

    [Fact]
    public async Task SacuvajIKnjiziUvozAsync_ShouldPostaviNalogId_IRasknjiziUvozAsync_ShouldGaUkloniti()
    {
        using var db = GetInMemoryDbContext();
        var partner = new Partner { SifraPartnera = "INO1", Naziv = "Ino dobavljač" };
        var magacin = new Magacin { SifraMagacina = "001", NazivMagacina = "Stovarište" };
        var artikal = new Artikal { SifraArtikla = "A1", Naziv = "Artikal 1" };
        db.Partneri.Add(partner);
        db.Magacini.Add(magacin);
        db.Artikli.Add(artikal);
        await db.SaveChangesAsync();

        var service = new UvoznaKalkulacijaService(db);
        var kalkulacija = new UvoznaKalkulacija
        {
            BrojKalkulacije = "UV-1",
            InoPartnerId = partner.PartnerId,
            InoBrojFakture = "F-1",
            Valuta = "EUR",
            KursValute = 117.00m,
            MagacinId = magacin.MagacinId,
            Stavke = new List<UvoznaStavka>
            {
                new() { ArtikalId = artikal.ArtikalId, Kolicina = 10, InoCenaDevize = 10m, CarinaProcenat = 10m }
            }
        };

        var (success, _, sacuvana) = await service.SacuvajIKnjiziUvozAsync(kalkulacija);

        Assert.True(success);
        Assert.NotNull(sacuvana!.NalogId);
        int nalogId = sacuvana.NalogId!.Value;
        Assert.True(await db.Nalozi.AnyAsync(n => n.NalogId == nalogId));

        await service.RasknjiziUvozAsync(sacuvana.UvoznaKalkulacijaId);

        var rasknjizena = await db.UvozneKalkulacije.FirstAsync(k => k.UvoznaKalkulacijaId == sacuvana.UvoznaKalkulacijaId);
        Assert.False(rasknjizena.IsKnjizeno);
        Assert.Null(rasknjizena.NalogId);
        Assert.False(await db.Nalozi.AnyAsync(n => n.NalogId == nalogId));
    }
}

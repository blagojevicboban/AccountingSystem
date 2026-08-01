using System;
using System.Linq;
using System.Threading.Tasks;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

public class ZatvaranjeStavkiServiceTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AccountingDbContext(options);
    }

    private static async Task<(Partner Partner, StavkaNaloga Faktura, StavkaNaloga Uplata)> PripremiFakturuIUplatuAsync(
        AccountingDbContext db, decimal iznosFakture, decimal iznosUplate, DateTime? valutaDospela = null)
    {
        var partner = new Partner { SifraPartnera = "P001", Naziv = "Kupac Alpha", KontoPartnera = "2020" };
        db.Partneri.Add(partner);
        await db.SaveChangesAsync();

        var nalogFaktura = new Nalog { BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 15), IsKnjizen = true, Opis = "Faktura 101" };
        var nalogUplata = new Nalog { BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 25), IsKnjizen = true, Opis = "Izvod 5" };
        db.Nalozi.AddRange(nalogFaktura, nalogUplata);
        await db.SaveChangesAsync();

        var faktura = new StavkaNaloga
        {
            NalogId = nalogFaktura.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId,
            BrojDokumenta = "F-101", Duguje = iznosFakture, Potrazuje = 0m, Opis = "Račun 101", ValutaDospela = valutaDospela
        };
        var uplata = new StavkaNaloga
        {
            NalogId = nalogUplata.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId,
            BrojDokumenta = "IZ-5", Duguje = 0m, Potrazuje = iznosUplate, Opis = "Uplata po izvodu 5"
        };
        db.StavkeNaloga.AddRange(faktura, uplata);
        await db.SaveChangesAsync();

        return (partner, faktura, uplata);
    }

    [Fact]
    public async Task ZatvoriAsync_PunoZatvaranje_PostajePreostaloNula()
    {
        using var db = GetInMemoryDbContext();
        var (partner, faktura, uplata) = await PripremiFakturuIUplatuAsync(db, 10000m, 10000m);

        var service = new ZatvaranjeStavkiService(db);
        await service.ZatvoriAsync(faktura.StavkaNalogaId, uplata.StavkaNalogaId, 10000m, DateTime.Now);

        var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, samoOtvorene: true);
        Assert.Empty(otvorene);
    }

    [Fact]
    public async Task ZatvoriAsync_DelimicnoZatvaranje_PreostaloTacno()
    {
        using var db = GetInMemoryDbContext();
        var (partner, faktura, uplata) = await PripremiFakturuIUplatuAsync(db, 10000m, 4000m);

        var service = new ZatvaranjeStavkiService(db);
        await service.ZatvoriAsync(faktura.StavkaNalogaId, uplata.StavkaNalogaId, 4000m, DateTime.Now);

        var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, samoOtvorene: true);
        var fakturaRed = Assert.Single(otvorene, r => r.Strana == "Duguje");
        Assert.Equal(6000m, fakturaRed.Preostalo);
        Assert.Equal("Delimično zatvoreno", fakturaRed.Status);
    }

    [Fact]
    public async Task ZatvoriAsync_PrekoracenjeIznosa_BacaException()
    {
        using var db = GetInMemoryDbContext();
        var (_, faktura, uplata) = await PripremiFakturuIUplatuAsync(db, 10000m, 10000m);

        var service = new ZatvaranjeStavkiService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ZatvoriAsync(faktura.StavkaNalogaId, uplata.StavkaNalogaId, 15000m, DateTime.Now));
    }

    [Fact]
    public async Task ZatvoriGrupnoAsync_MPremaN_KorektnoRasporedi()
    {
        using var db = GetInMemoryDbContext();
        var partner = new Partner { SifraPartnera = "P001", Naziv = "Kupac Alpha", KontoPartnera = "2020" };
        db.Partneri.Add(partner);
        await db.SaveChangesAsync();

        var nalog1 = new Nalog { BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 10), IsKnjizen = true };
        var nalog2 = new Nalog { BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 12), IsKnjizen = true };
        var nalogUplata = new Nalog { BrojNaloga = 103, DatumNaloga = new DateTime(2026, 1, 20), IsKnjizen = true };
        db.Nalozi.AddRange(nalog1, nalog2, nalogUplata);
        await db.SaveChangesAsync();

        var faktura1 = new StavkaNaloga { NalogId = nalog1.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId, Duguje = 6000m, Potrazuje = 0m };
        var faktura2 = new StavkaNaloga { NalogId = nalog2.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId, Duguje = 4000m, Potrazuje = 0m };
        var uplata = new StavkaNaloga { NalogId = nalogUplata.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId, Duguje = 0m, Potrazuje = 10000m };
        db.StavkeNaloga.AddRange(faktura1, faktura2, uplata);
        await db.SaveChangesAsync();

        var service = new ZatvaranjeStavkiService(db);
        var rezultat = await service.ZatvoriGrupnoAsync(
            new List<(int, decimal)> { (faktura1.StavkaNalogaId, 6000m), (faktura2.StavkaNalogaId, 4000m) },
            new List<(int, decimal)> { (uplata.StavkaNalogaId, 10000m) },
            DateTime.Now);

        Assert.Equal(2, rezultat.Count);
        var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, samoOtvorene: true);
        Assert.Empty(otvorene);
    }

    [Fact]
    public async Task ZatvoriGrupnoAsync_NejednakiZbirovi_BacaException()
    {
        using var db = GetInMemoryDbContext();
        var (_, faktura, uplata) = await PripremiFakturuIUplatuAsync(db, 10000m, 5000m);

        var service = new ZatvaranjeStavkiService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ZatvoriGrupnoAsync(
            new List<(int, decimal)> { (faktura.StavkaNalogaId, 10000m) },
            new List<(int, decimal)> { (uplata.StavkaNalogaId, 5000m) },
            DateTime.Now));
    }

    [Fact]
    public async Task GetOtvoreneStavkeZaPartneraAsync_NaDan_IgnorisePoznijaZatvaranja()
    {
        using var db = GetInMemoryDbContext();
        var (partner, faktura, uplata) = await PripremiFakturuIUplatuAsync(db, 10000m, 10000m);

        var service = new ZatvaranjeStavkiService(db);
        await service.ZatvoriAsync(faktura.StavkaNalogaId, uplata.StavkaNalogaId, 10000m, new DateTime(2026, 2, 1));

        // Na dan pre zatvaranja, stavka mora i dalje biti otvorena.
        var otvoreneRanije = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, naDan: new DateTime(2026, 1, 26), samoOtvorene: true);
        Assert.Single(otvoreneRanije, r => r.Strana == "Duguje");

        // Na dan posle zatvaranja, stavka je zatvorena.
        var otvoreneKasnije = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, naDan: new DateTime(2026, 2, 5), samoOtvorene: true);
        Assert.Empty(otvoreneKasnije);
    }

    [Fact]
    public async Task OtkaziZatvaranjeAsync_VracaStavkuUOtvoreno()
    {
        using var db = GetInMemoryDbContext();
        var (partner, faktura, uplata) = await PripremiFakturuIUplatuAsync(db, 10000m, 10000m);

        var service = new ZatvaranjeStavkiService(db);
        var zatvaranje = await service.ZatvoriAsync(faktura.StavkaNalogaId, uplata.StavkaNalogaId, 10000m, DateTime.Now);

        var ok = await service.OtkaziZatvaranjeAsync(zatvaranje.ZatvaranjeStavkeId);
        Assert.True(ok);

        var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, samoOtvorene: true);
        Assert.Equal(2, otvorene.Count);
    }

    [Fact]
    public async Task GetOtvoreneStavkeZaPartneraAsync_DospelaNeplacenaStavka_ImaDaneKasnjenja()
    {
        using var db = GetInMemoryDbContext();
        var (partner, _, _) = await PripremiFakturuIUplatuAsync(db, 10000m, 0m, valutaDospela: new DateTime(2026, 1, 20));

        var service = new ZatvaranjeStavkiService(db);
        var otvorene = await service.GetOtvoreneStavkeZaPartneraAsync(partner.PartnerId, naDan: new DateTime(2026, 2, 1), samoOtvorene: true);

        var fakturaRed = Assert.Single(otvorene, r => r.Strana == "Duguje");
        Assert.True(fakturaRed.JeDospelo);
        Assert.Equal(12, fakturaRed.DanaKasnjenja);
    }
}

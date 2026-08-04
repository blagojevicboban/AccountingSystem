using System;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class KamataServiceTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task ObracunajKamatuAsync_DelimicnoZatvorenaFaktura_KamataSeRacunaSamoNaPreostalo()
    {
        using var db = GetInMemoryDbContext();

        var partner = new Partner { SifraPartnera = "P001", Naziv = "Kupac Alpha", KontoPartnera = "2040" };
        db.Partneri.Add(partner);
        await db.SaveChangesAsync();

        var nalogFaktura = new Nalog { BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true, Opis = "Faktura 101" };
        var nalogUplata = new Nalog { BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 5), IsKnjizen = true, Opis = "Uplata" };
        db.Nalozi.AddRange(nalogFaktura, nalogUplata);
        await db.SaveChangesAsync();

        var faktura = new StavkaNaloga { NalogId = nalogFaktura.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = partner.PartnerId, Duguje = 10000m, Potrazuje = 0m, Opis = "Račun 101" };
        var uplata = new StavkaNaloga { NalogId = nalogUplata.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = partner.PartnerId, Duguje = 0m, Potrazuje = 6000m, Opis = "Delimična uplata" };
        db.StavkeNaloga.AddRange(faktura, uplata);
        await db.SaveChangesAsync();

        var zatvaranjeService = new ZatvaranjeStavkiService(db);
        await zatvaranjeService.ZatvoriAsync(faktura.StavkaNalogaId, uplata.StavkaNalogaId, 6000m, new DateTime(2026, 1, 5));

        db.KamatneStope.Add(new KamatnaStopa { DatumOd = new DateTime(2025, 1, 1), GodisnjaStopaProcenat = 10.00m });
        await db.SaveChangesAsync();

        var kamataService = new KamataService(db);
        var datumObracuna = new DateTime(2026, 7, 1);
        var rezultat = await kamataService.ObracunajKamatuAsync(partner.PartnerId, datumObracuna);

        var stavka = Assert.Single(rezultat);
        Assert.Equal(4000m, stavka.Iznos);

        // Kamata na 4000 mora biti manja od kamate koja bi se dobila na puni originalni iznos od 10000.
        decimal ocekivanaNaPuno = 10000m * ((decimal)Math.Pow(1.10, (datumObracuna - new DateTime(2026, 1, 1)).Days / 365.0) - 1m);
        Assert.True(stavka.ObracunataKamata < ocekivanaNaPuno);
        Assert.True(stavka.ObracunataKamata > 0);
    }

    [Fact]
    public async Task ObracunajKamatuAsync_PartnerJeIKupacIDobavljac_KamataSeRacunaSamoNaKontuKupca()
    {
        using var db = GetInMemoryDbContext();

        // Isti partner ima i otvoren dug prema nama (204, Duguje) i našu obavezu prema njemu
        // koju smo delimično platili (435, Duguje = uplata koja umanjuje obavezu) — ta 435-Duguje
        // stavka ne sme ući u osnovicu za kamatu, jer nije njegov dug prema nama.
        var partner = new Partner { SifraPartnera = "P001", Naziv = "Partner Alpha" };
        db.Partneri.Add(partner);
        await db.SaveChangesAsync();

        var nalogFaktura = new Nalog { BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true, Opis = "Faktura kupcu" };
        var nalogUplataDobavljacu = new Nalog { BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 5), IsKnjizen = true, Opis = "Naša uplata dobavljaču" };
        db.Nalozi.AddRange(nalogFaktura, nalogUplataDobavljacu);
        await db.SaveChangesAsync();

        db.StavkeNaloga.AddRange(
            new StavkaNaloga { NalogId = nalogFaktura.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = partner.PartnerId, Duguje = 10000m, Potrazuje = 0m, Opis = "Račun 101" },
            new StavkaNaloga { NalogId = nalogUplataDobavljacu.NalogId, RedniBroj = 1, BrojKonta = "4350", PartnerId = partner.PartnerId, Duguje = 50000m, Potrazuje = 0m, Opis = "Uplata dobavljaču" }
        );
        await db.SaveChangesAsync();

        db.KamatneStope.Add(new KamatnaStopa { DatumOd = new DateTime(2025, 1, 1), GodisnjaStopaProcenat = 10.00m });
        await db.SaveChangesAsync();

        var kamataService = new KamataService(db);
        var rezultat = await kamataService.ObracunajKamatuAsync(partner.PartnerId, new DateTime(2026, 7, 1));

        var stavka = Assert.Single(rezultat);
        Assert.Equal(10000m, stavka.Iznos);
    }
}

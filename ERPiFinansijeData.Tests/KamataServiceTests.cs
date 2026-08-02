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

        var partner = new Partner { SifraPartnera = "P001", Naziv = "Kupac Alpha", KontoPartnera = "2020" };
        db.Partneri.Add(partner);
        await db.SaveChangesAsync();

        var nalogFaktura = new Nalog { BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true, Opis = "Faktura 101" };
        var nalogUplata = new Nalog { BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 5), IsKnjizen = true, Opis = "Uplata" };
        db.Nalozi.AddRange(nalogFaktura, nalogUplata);
        await db.SaveChangesAsync();

        var faktura = new StavkaNaloga { NalogId = nalogFaktura.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId, Duguje = 10000m, Potrazuje = 0m, Opis = "Račun 101" };
        var uplata = new StavkaNaloga { NalogId = nalogUplata.NalogId, RedniBroj = 1, BrojKonta = "2020", PartnerId = partner.PartnerId, Duguje = 0m, Potrazuje = 6000m, Opis = "Delimična uplata" };
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
}

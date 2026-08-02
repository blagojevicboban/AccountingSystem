using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class EsirFiskalizacijaTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AccountingDbContext(options);
    }

    /// <summary>
    /// PFR nije dostupan (URL ne postoji). Bez uključenog simulator moda fiskalizacija
    /// MORA da padne - račun se ne sme evidentirati kao fiskalizovan.
    /// </summary>
    [Fact]
    public async Task FiskalizujRacunAsync_BezSimulatorModa_PrijavljujeGreskuKadPfrNijeDostupan()
    {
        using var db = GetInMemoryDbContext();
        db.Firme.Add(new Firma
        {
            FirmaId = 1,
            Sifra = "01",
            Naziv = "Test doo",
            PfrUrl = "http://localhost:59999",
            PfrSimulatorMod = false
        });
        PripremiRacun(db);
        await db.SaveChangesAsync();

        var service = new EsirFiskalizacijaService(db);

        var (success, simulacija, message, log) = await service.FiskalizujRacunAsync(1, "Cash");

        Assert.False(success);
        Assert.False(simulacija);
        Assert.Null(log);
        Assert.Contains("NIJE fiskalizovan", message);

        var azurirani = await db.RacuniOtpremnice.FindAsync(1);
        Assert.NotNull(azurirani);
        Assert.Equal(FiskalniStatus.Greska, azurirani.FiskalniStatus);
        Assert.Empty(db.FiskalniRacuniLog);
    }

    /// <summary>
    /// Sa izričito uključenim simulator modom račun se izdaje, ali se označava kao
    /// SIMULACIJA - nikada kao Fiskalizovan - i nema verifikacioni URL.
    /// </summary>
    [Fact]
    public async Task FiskalizujRacunAsync_SaSimulatorModom_OznacavaRacunKaoSimulaciju()
    {
        using var db = GetInMemoryDbContext();
        db.Firme.Add(new Firma
        {
            FirmaId = 1,
            Sifra = "01",
            Naziv = "Test doo",
            PfrUrl = "http://localhost:59999",
            PfrSimulatorMod = true
        });
        PripremiRacun(db);
        await db.SaveChangesAsync();

        var service = new EsirFiskalizacijaService(db);

        var (success, simulacija, _, log) = await service.FiskalizujRacunAsync(1, "Cash");

        Assert.True(success);
        Assert.True(simulacija);
        Assert.NotNull(log);
        Assert.StartsWith("SIMULACIJA-", log.InvoiceNumber);
        Assert.True(string.IsNullOrEmpty(log.VerificationUrl));

        var azurirani = await db.RacuniOtpremnice.FindAsync(1);
        Assert.NotNull(azurirani);
        Assert.Equal(FiskalniStatus.Simulacija, azurirani.FiskalniStatus);
        Assert.NotEqual(FiskalniStatus.Fiskalizovan, azurirani.FiskalniStatus);
    }

    private static void PripremiRacun(AccountingDbContext db)
    {
        var artikal = new Artikal { ArtikalId = 1, SifraArtikla = "A001", Naziv = "Artikal Test", ProdajnaCena = 1000m };
        db.Artikli.Add(artikal);

        var racun = new RacunOtpremnica
        {
            RacunOtpremnicaId = 1,
            BrojRacuna = 100,
            UkupnoZaUplatu = 1200m,
            UkupnoOsnovica = 1000m,
            UkupnoPdv = 200m,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new RacunOtpremnicaStavka
                {
                    ArtikalId = 1,
                    Kolicina = 1,
                    ProdajnaCena = 1000m,
                    StopaPdv = 20m,
                    Osnovica = 1000m,
                    IznosPdv = 200m,
                    Ukupno = 1200m
                }
            }
        };

        db.RacuniOtpremnice.Add(racun);
    }
}

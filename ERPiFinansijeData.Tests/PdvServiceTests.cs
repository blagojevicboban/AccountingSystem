using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class PdvServiceTests
{
    private AccountingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task GetKirZapisi_ProknjizeniRacuni_RacunaStavkeIPdv()
    {
        using var db = CreateInMemoryDbContext();

        var partner = new Partner { PartnerId = 1, Naziv = "Kupac Test", Pib = "109876543" };
        db.Partneri.Add(partner);

        var racun = new RacunOtpremnica
        {
            BrojRacuna = 1,
            DatumRacuna = new DateTime(2026, 3, 1),
            PartnerId = 1,
            Partner = partner,
            IsKnjizen = true,
            UkupnoOsnovica = 1000m,
            UkupnoPdv = 200m,
            UkupnoZaUplatu = 1200m,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new RacunOtpremnicaStavka { StopaPdv = 20m, Osnovica = 1000m, IznosPdv = 200m, Ukupno = 1200m }
            }
        };

        db.RacuniOtpremnice.Add(racun);
        await db.SaveChangesAsync();

        var service = new PdvService(db);
        var kir = await service.GetKirZapisiAsync();

        Assert.Single(kir);
        Assert.Equal("1", kir[0].BrojDokumenta);
        Assert.Equal("Kupac Test", kir[0].PartnerNaziv);
        Assert.Equal(1000m, kir[0].Osnovica20);
        Assert.Equal(200m, kir[0].Pdv20);
        Assert.Equal(1200m, kir[0].UkupnaNaknadaSaPdv);
    }

    [Fact]
    public async Task GetPdvObracun_KirIKpr_RacunaRazlikuObaveze()
    {
        using var db = CreateInMemoryDbContext();

        // 1. KIR - Izlazni PDV = 200 RSD
        var racun = new RacunOtpremnica
        {
            BrojRacuna = 2,
            DatumRacuna = new DateTime(2026, 3, 5),
            IsKnjizen = true,
            UkupnoZaUplatu = 1200m,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new RacunOtpremnicaStavka { StopaPdv = 20m, Osnovica = 1000m, IznosPdv = 200m, Ukupno = 1200m }
            }
        };

        // 2. KPR - Prethodni PDV = 80 RSD
        var kalkulacija = new Kalkulacija
        {
            BrojKalkulacije = 1,
            Datum = new DateTime(2026, 3, 2),
            IsKnjizen = true,
            SvegaNabavno = 400m,
            Razlika = 0m,
            PoreskaStopaProcenat = 20m,
            Porez = 80m,
            ProdajnaVrednost = 480m
        };

        db.RacuniOtpremnice.Add(racun);
        db.Kalkulacije.Add(kalkulacija);
        await db.SaveChangesAsync();

        var service = new PdvService(db);
        var obracun = await service.GetPdvObracunAsync();

        Assert.Equal(200m, obracun.KirUkupanPdv);
        Assert.Equal(80m, obracun.KprUkupanPdv);
        Assert.Equal(120m, obracun.PdvRazlika); // 200 - 80 = 120 obaveza
    }

    [Fact]
    public async Task GetKirZapisi_RucnoUnetNalogNaKontu4700_UlaziUKirSaOsnovicomIStopom()
    {
        using var db = CreateInMemoryDbContext();

        var partner = new Partner { PartnerId = 1, Naziv = "Kupac Ručni", Pib = "111222333" };
        db.Partneri.Add(partner);

        var nalog = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 3, 10), IsKnjizen = true, VrstaNaloga = "Finansijski" };
        db.Nalozi.Add(nalog);
        await db.SaveChangesAsync();

        db.StavkeNaloga.AddRange(
            new StavkaNaloga { NalogId = nalog.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = 1, Duguje = 1200m, Potrazuje = 0m, BrojDokumenta = "RR-1" },
            new StavkaNaloga { NalogId = nalog.NalogId, RedniBroj = 2, BrojKonta = "6120", PartnerId = 1, Duguje = 0m, Potrazuje = 1000m, BrojDokumenta = "RR-1" },
            new StavkaNaloga { NalogId = nalog.NalogId, RedniBroj = 3, BrojKonta = "4700", PartnerId = 1, Duguje = 0m, Potrazuje = 200m, BrojDokumenta = "RR-1", Osnovica = 1000m, StopaPdv = 20m }
        );
        await db.SaveChangesAsync();

        var service = new PdvService(db);
        var kir = await service.GetKirZapisiAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        var zapis = Assert.Single(kir);
        Assert.Equal("RR-1", zapis.BrojDokumenta);
        Assert.Equal("Kupac Ručni", zapis.PartnerNaziv);
        Assert.Equal(1000m, zapis.Osnovica20);
        Assert.Equal(200m, zapis.Pdv20);
        Assert.Equal(1200m, zapis.UkupnaNaknadaSaPdv);
    }

    [Fact]
    public async Task GetKirZapisi_RucnaStavkaVecObuhvacenaKrozRacunOtpremnicu_NijeUbrojanaDvaputa()
    {
        using var db = CreateInMemoryDbContext();

        var nalog = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 3, 10), IsKnjizen = true, VrstaNaloga = "Prodaja" };
        db.Nalozi.Add(nalog);
        await db.SaveChangesAsync();

        // Nalog koji je automatski kreirala Trgovina za ovaj račun — StavkaNaloga na 4700 postoji,
        // ali NalogId je već povezan sa RacunOtpremnica, pa se ne sme ponovo pobrojati kao "ručni unos".
        var racun = new RacunOtpremnica
        {
            BrojRacuna = 5,
            DatumRacuna = new DateTime(2026, 3, 10),
            IsKnjizen = true,
            NalogId = nalog.NalogId,
            UkupnoOsnovica = 1000m,
            UkupnoPdv = 200m,
            UkupnoZaUplatu = 1200m,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new RacunOtpremnicaStavka { StopaPdv = 20m, Osnovica = 1000m, IznosPdv = 200m, Ukupno = 1200m }
            }
        };
        db.RacuniOtpremnice.Add(racun);

        db.StavkeNaloga.Add(new StavkaNaloga { NalogId = nalog.NalogId, RedniBroj = 1, BrojKonta = "4700", Duguje = 0m, Potrazuje = 200m, Osnovica = 1000m, StopaPdv = 20m });
        await db.SaveChangesAsync();

        var service = new PdvService(db);
        var kir = await service.GetKirZapisiAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        Assert.Single(kir); // samo zapis iz RacunOtpremnice, ne i duplikat iz ručnog uparivanja po kontu 4700
    }
}

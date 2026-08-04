using System;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

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

    [Fact]
    public async Task GetIosIzvestajAsync_KoristiZatvaranjeFalse_NePopunjavaStatusZatvaranja()
    {
        using var db = GetInMemoryDbContext();

        var p1 = new Partner { PartnerId = 1, SifraPartnera = "P001", Naziv = "Kupac Alpha", KontoPartnera = "2020" };
        db.Partneri.Add(p1);

        var n1 = new Nalog { NalogId = 1, BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 15), IsKnjizen = true, Opis = "Faktura Alpha" };
        db.Nalozi.Add(n1);

        db.StavkeNaloga.Add(new StavkaNaloga { StavkaNalogaId = 1, NalogId = 1, RedniBroj = 1, BrojKonta = "2020", PartnerId = 1, Duguje = 10000m, Potrazuje = 0m, Opis = "Račun 101" });
        await db.SaveChangesAsync();

        var service = new OtvoreneStavkeService(db);

        // Podrazumevani poziv (koristiZatvaranje:false) mora ostati identičan starom ponašanju — bez statusa zatvaranja.
        var rezultatBezZatvaranja = await service.GetIosIzvestajAsync(samoSaSaldom: true);
        Assert.All(rezultatBezZatvaranja.SelectMany(g => g.Stavke), s => Assert.Null(s.StatusZatvaranja));

        // Kad se eksplicitno traži, polja moraju biti popunjena.
        var rezultatSaZatvaranjem = await service.GetIosIzvestajAsync(samoSaSaldom: true, koristiZatvaranje: true);
        Assert.All(rezultatSaZatvaranjem.SelectMany(g => g.Stavke), s => Assert.Equal("Otvoreno", s.StatusZatvaranja));
    }

    [Fact]
    public async Task GetOtvoreneStavkeAsync_SaKontoPrefiksom_RacunaSaldoSamoZaTajKonto()
    {
        using var db = GetInMemoryDbContext();

        // Partner je istovremeno i kupac (204) i dobavljač (435) — kartica ne sme mešati ta dva konta u jedan saldo.
        var p1 = new Partner { PartnerId = 1, SifraPartnera = "P001", Naziv = "Partner Alpha" };
        db.Partneri.Add(p1);

        var n1 = new Nalog { NalogId = 1, BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 15), IsKnjizen = true, Opis = "Faktura kupcu" };
        var n2 = new Nalog { NalogId = 2, BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 20), IsKnjizen = true, Opis = "Ulazni račun dobavljača" };
        db.Nalozi.AddRange(n1, n2);

        db.StavkeNaloga.AddRange(
            new StavkaNaloga { StavkaNalogaId = 1, NalogId = 1, RedniBroj = 1, BrojKonta = "2040", PartnerId = 1, Duguje = 10000m, Potrazuje = 0m },
            new StavkaNaloga { StavkaNalogaId = 2, NalogId = 2, RedniBroj = 1, BrojKonta = "4350", PartnerId = 1, Duguje = 0m, Potrazuje = 25000m }
        );
        await db.SaveChangesAsync();

        var service = new OtvoreneStavkeService(db);

        var kupacKartica = await service.GetOtvoreneStavkeAsync(1, "204");
        Assert.Single(kupacKartica);
        Assert.Equal(10000m, kupacKartica[0].Saldo);

        var dobavljacKartica = await service.GetOtvoreneStavkeAsync(1, "435");
        Assert.Single(dobavljacKartica);
        Assert.Equal(-25000m, dobavljacKartica[0].Saldo);

        var svaKarticaBezFiltera = await service.GetOtvoreneStavkeAsync(1);
        Assert.Equal(2, svaKarticaBezFiltera.Count);
    }

    [Fact]
    public async Task GetPartnerKontaAsync_VracaDistinktneKonteSaBrojemStavkiIOpadajucimPoretkom()
    {
        using var db = GetInMemoryDbContext();

        var p1 = new Partner { PartnerId = 1, SifraPartnera = "P001", Naziv = "Partner Alpha" };
        db.Partneri.Add(p1);
        db.Konta.Add(new Konto { KontoId = 1, BrojKonta = "2040", NazivKonta = "Kupci u zemlji" });

        var n1 = new Nalog { NalogId = 1, BrojNaloga = 101, DatumNaloga = new DateTime(2026, 1, 15), IsKnjizen = true };
        var n2 = new Nalog { NalogId = 2, BrojNaloga = 102, DatumNaloga = new DateTime(2026, 1, 20), IsKnjizen = true };
        var n3 = new Nalog { NalogId = 3, BrojNaloga = 103, DatumNaloga = new DateTime(2026, 1, 22), IsKnjizen = true };
        db.Nalozi.AddRange(n1, n2, n3);

        db.StavkeNaloga.AddRange(
            new StavkaNaloga { StavkaNalogaId = 1, NalogId = 1, RedniBroj = 1, BrojKonta = "2040", PartnerId = 1, Duguje = 10000m },
            new StavkaNaloga { StavkaNalogaId = 2, NalogId = 2, RedniBroj = 1, BrojKonta = "2040", PartnerId = 1, Duguje = 5000m },
            new StavkaNaloga { StavkaNalogaId = 3, NalogId = 3, RedniBroj = 1, BrojKonta = "4350", PartnerId = 1, Potrazuje = 25000m }
        );
        await db.SaveChangesAsync();

        var service = new OtvoreneStavkeService(db);
        var konta = await service.GetPartnerKontaAsync(1);

        Assert.Equal(2, konta.Count);
        Assert.Equal("2040", konta[0].BrojKonta);
        Assert.Equal(2, konta[0].BrojStavki);
        Assert.Equal("Kupci u zemlji", konta[0].NazivKonta);
        Assert.Equal("4350", konta[1].BrojKonta);
        Assert.Equal(1, konta[1].BrojStavki);
    }

    [Fact]
    public async Task GetPartneriAsync_UkljucujeSintetickePartnereIzKontaBezPartnerIdVeze()
    {
        using var db = GetInMemoryDbContext();

        // Legacy DBF uvoz ne popunjava StavkaNaloga.PartnerId — kupac/dobavljač je tamo
        // predstavljen samo svojom analitičkom podšifrom (204xxx/435xxx). GetPartneriAsync
        // mora takve konte da prikaže kao "sintetičke" partnere (PartnerId=0), inače su nevidljivi.
        var pravi = new Partner { PartnerId = 1, SifraPartnera = "P001", Naziv = "Pravi Partner" };
        db.Partneri.Add(pravi);
        db.Konta.AddRange(
            new Konto { KontoId = 1, BrojKonta = "204015", NazivKonta = "USLUZNO PREDUZECE KAI" },
            new Konto { KontoId = 2, BrojKonta = "435002", NazivKonta = "Telekom Srbija A.D. Beograd" }
        );

        var n1 = new Nalog { NalogId = 1, BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true };
        var n2 = new Nalog { NalogId = 2, BrojNaloga = 2, DatumNaloga = new DateTime(2026, 1, 2), IsKnjizen = true };
        var n3 = new Nalog { NalogId = 3, BrojNaloga = 3, DatumNaloga = new DateTime(2026, 1, 3), IsKnjizen = true };
        db.Nalozi.AddRange(n1, n2, n3);

        db.StavkeNaloga.AddRange(
            // Bez PartnerId — samo konto (legacy uvoz)
            new StavkaNaloga { StavkaNalogaId = 1, NalogId = 1, RedniBroj = 1, BrojKonta = "204015", PartnerId = null, Duguje = 1000m },
            new StavkaNaloga { StavkaNalogaId = 2, NalogId = 2, RedniBroj = 1, BrojKonta = "435002", PartnerId = null, Potrazuje = 500m },
            // Stavka koja NIJE u opsegu kupaca/dobavljača (npr. banka) — ne sme se pojaviti kao "partner"
            new StavkaNaloga { StavkaNalogaId = 3, NalogId = 3, RedniBroj = 1, BrojKonta = "2410", PartnerId = null, Duguje = 200m }
        );
        await db.SaveChangesAsync();

        var service = new OtvoreneStavkeService(db);
        var partneri = await service.GetPartneriAsync();

        Assert.Equal(3, partneri.Count); // 1 pravi + 2 sintetička (204015, 435002) — 2410 nije partnerski konto

        var sinteticki204 = Assert.Single(partneri, p => p.SifraPartnera == "204015");
        Assert.Equal(0, sinteticki204.PartnerId);
        Assert.Equal("USLUZNO PREDUZECE KAI", sinteticki204.Naziv);
        Assert.Equal("204015", sinteticki204.KontoPartnera);

        var sinteticki435 = Assert.Single(partneri, p => p.SifraPartnera == "435002");
        Assert.Equal(0, sinteticki435.PartnerId);
        Assert.Equal("Telekom Srbija A.D. Beograd", sinteticki435.Naziv);

        Assert.Contains(partneri, p => p.PartnerId == 1 && p.Naziv == "Pravi Partner");
    }

    [Fact]
    public async Task GetOtvoreneStavkeZaKontoAsync_RacunaSaldoZaSintetickogPartneraPoTacnomKontu()
    {
        using var db = GetInMemoryDbContext();

        var n1 = new Nalog { NalogId = 1, BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true };
        var n2 = new Nalog { NalogId = 2, BrojNaloga = 2, DatumNaloga = new DateTime(2026, 1, 5), IsKnjizen = true };
        db.Nalozi.AddRange(n1, n2);

        db.StavkeNaloga.AddRange(
            new StavkaNaloga { StavkaNalogaId = 1, NalogId = 1, RedniBroj = 1, BrojKonta = "204015", PartnerId = null, Duguje = 1000m },
            new StavkaNaloga { StavkaNalogaId = 2, NalogId = 2, RedniBroj = 1, BrojKonta = "204015", PartnerId = null, Duguje = 0m, Potrazuje = 400m }
        );
        await db.SaveChangesAsync();

        var service = new OtvoreneStavkeService(db);
        var kartica = await service.GetOtvoreneStavkeZaKontoAsync("204015");

        Assert.Equal(2, kartica.Count);
        Assert.Equal(600m, kartica[^1].Saldo);
    }
}

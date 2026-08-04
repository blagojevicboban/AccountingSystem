using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class KompenzacijaServiceTests
{
    private AccountingDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AccountingDbContext(options);
    }

    [Fact]
    public async Task KnjiziIZatvoriKompenzacijuAsync_TrojnaCesija_ZatvaraAnalitikuSvakogPartneraPoOdvojenojLiniji()
    {
        using var db = GetInMemoryDbContext();

        // Cedent (Partner A) nam duguje 5000 na kontu kupca (204). Cesijom se to potraživanje
        // ustupa Cesionaru (Partner B) da njime pokrije NAŠU obavezu prema njemu od 5000 (435).
        // Pre generalizacije Kompenzacija je mogla da prebija samo 204 i 435 ISTOG partnera —
        // ovo je tačno slučaj koji je bio nemoguć: dva RAZLIČITA partnera u jednoj kompenzaciji.
        var partnerA = new Partner { SifraPartnera = "A", Naziv = "Partner A (Cedent)" };
        var partnerB = new Partner { SifraPartnera = "B", Naziv = "Partner B (Cesionar)" };
        db.Partneri.AddRange(partnerA, partnerB);
        await db.SaveChangesAsync();

        var nalogA = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true, Opis = "Faktura A" };
        var nalogB = new Nalog { BrojNaloga = 2, DatumNaloga = new DateTime(2026, 1, 2), IsKnjizen = true, Opis = "Ulazni račun B" };
        db.Nalozi.AddRange(nalogA, nalogB);
        await db.SaveChangesAsync();

        var stavkaA = new StavkaNaloga { NalogId = nalogA.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = partnerA.PartnerId, Duguje = 5000m, Potrazuje = 0m };
        var stavkaB = new StavkaNaloga { NalogId = nalogB.NalogId, RedniBroj = 1, BrojKonta = "4350", PartnerId = partnerB.PartnerId, Duguje = 0m, Potrazuje = 5000m };
        db.StavkeNaloga.AddRange(stavkaA, stavkaB);
        await db.SaveChangesAsync();

        var kompenzacija = new Kompenzacija
        {
            Vrsta = VrstaKompenzacije.Cesija,
            PartnerId = partnerA.PartnerId,
            NazivPartnera = partnerA.Naziv,
            Partner2Id = partnerB.PartnerId,
            NazivPartnera2 = partnerB.Naziv,
            Datum = new DateTime(2026, 2, 1),
            Stavke = new List<KompenzacijaStavka>
            {
                new KompenzacijaStavka { RedniBroj = 1, StavkaNalogaId = stavkaA.StavkaNalogaId, PartnerId = partnerA.PartnerId, Strana = "Duguje", BrojKonta = "2040", IznosFakture = 5000m, IznosPreostalo = 5000m, IznosZaKompenzaciju = 5000m },
                new KompenzacijaStavka { RedniBroj = 2, StavkaNalogaId = stavkaB.StavkaNalogaId, PartnerId = partnerB.PartnerId, Strana = "Potražuje", BrojKonta = "4350", IznosFakture = 5000m, IznosPreostalo = 5000m, IznosZaKompenzaciju = 5000m }
            }
        };

        var service = new KompenzacijaService(db);
        var sacuvana = await service.SacuvajKompenzacijuAsync(kompenzacija);
        Assert.Equal(5000m, sacuvana.UkupanIznosKompenzacije);

        var (success, message, nalogId) = await service.KnjiziIZatvoriKompenzacijuAsync(sacuvana.KompenzacijaId);
        Assert.True(success, message);

        var nalog = await db.Nalozi.Include(n => n.Stavke).FirstAsync(n => n.NalogId == nalogId);
        Assert.Equal(5000m, nalog.UkupnoDuguje);
        Assert.Equal(5000m, nalog.UkupnoPotrazuje);
        Assert.Equal(2, nalog.Stavke.Count);
        Assert.Contains(nalog.Stavke, s => s.PartnerId == partnerA.PartnerId && s.BrojKonta == "2040" && s.Potrazuje == 5000m);
        Assert.Contains(nalog.Stavke, s => s.PartnerId == partnerB.PartnerId && s.BrojKonta == "4350" && s.Duguje == 5000m);

        var zatvaranjeService = new ZatvaranjeStavkiService(db);
        var otvoreneA = await zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partnerA.PartnerId, new DateTime(2026, 3, 1));
        var otvoreneB = await zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(partnerB.PartnerId, new DateTime(2026, 3, 1));
        Assert.Empty(otvoreneA);
        Assert.Empty(otvoreneB);
    }

    [Fact]
    public async Task KnjiziIZatvoriKompenzacijuAsync_NejednakiZbiroviPotrazivanjaIObaveza_VracaGreskuBezKnjizenja()
    {
        using var db = GetInMemoryDbContext();

        var partnerA = new Partner { SifraPartnera = "A", Naziv = "Partner A" };
        var partnerB = new Partner { SifraPartnera = "B", Naziv = "Partner B" };
        db.Partneri.AddRange(partnerA, partnerB);
        await db.SaveChangesAsync();

        var nalogA = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true };
        var nalogB = new Nalog { BrojNaloga = 2, DatumNaloga = new DateTime(2026, 1, 2), IsKnjizen = true };
        db.Nalozi.AddRange(nalogA, nalogB);
        await db.SaveChangesAsync();

        var stavkaA = new StavkaNaloga { NalogId = nalogA.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = partnerA.PartnerId, Duguje = 5000m, Potrazuje = 0m };
        var stavkaB = new StavkaNaloga { NalogId = nalogB.NalogId, RedniBroj = 1, BrojKonta = "4350", PartnerId = partnerB.PartnerId, Duguje = 0m, Potrazuje = 3000m };
        db.StavkeNaloga.AddRange(stavkaA, stavkaB);
        await db.SaveChangesAsync();

        var kompenzacija = new Kompenzacija
        {
            Vrsta = VrstaKompenzacije.Asignacija,
            PartnerId = partnerA.PartnerId,
            NazivPartnera = partnerA.Naziv,
            Partner2Id = partnerB.PartnerId,
            NazivPartnera2 = partnerB.Naziv,
            Datum = new DateTime(2026, 2, 1),
            Stavke = new List<KompenzacijaStavka>
            {
                new KompenzacijaStavka { RedniBroj = 1, StavkaNalogaId = stavkaA.StavkaNalogaId, PartnerId = partnerA.PartnerId, Strana = "Duguje", BrojKonta = "2040", IznosFakture = 5000m, IznosPreostalo = 5000m, IznosZaKompenzaciju = 5000m },
                new KompenzacijaStavka { RedniBroj = 2, StavkaNalogaId = stavkaB.StavkaNalogaId, PartnerId = partnerB.PartnerId, Strana = "Potražuje", BrojKonta = "4350", IznosFakture = 3000m, IznosPreostalo = 3000m, IznosZaKompenzaciju = 3000m }
            }
        };

        var service = new KompenzacijaService(db);
        var sacuvana = await service.SacuvajKompenzacijuAsync(kompenzacija);

        var (success, message, nalogId) = await service.KnjiziIZatvoriKompenzacijuAsync(sacuvana.KompenzacijaId);

        Assert.False(success);
        Assert.Null(nalogId);
        Assert.False((await db.Kompenzacije.FindAsync(sacuvana.KompenzacijaId))!.IsKnjizeno);
    }

    [Fact]
    public async Task KnjiziIZatvoriKompenzacijuAsync_LegacyKontoBezPartnera_ZatvarajucaLinijaKoristiTacanBrojKontaNeGenericki()
    {
        using var db = GetInMemoryDbContext();

        // Legacy analitički konto (204900) bez zapisa u šifarniku Partneri — PartnerId=0 (sentinel).
        // Kompenzira se sa pravim partnerom B na dobavljačkoj strani (4350).
        var partnerB = new Partner { SifraPartnera = "B", Naziv = "Partner B" };
        db.Partneri.Add(partnerB);
        await db.SaveChangesAsync();

        var nalogA = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true };
        var nalogB = new Nalog { BrojNaloga = 2, DatumNaloga = new DateTime(2026, 1, 2), IsKnjizen = true };
        db.Nalozi.AddRange(nalogA, nalogB);
        await db.SaveChangesAsync();

        var stavkaA = new StavkaNaloga { NalogId = nalogA.NalogId, RedniBroj = 1, BrojKonta = "204900", PartnerId = null, Duguje = 4000m, Potrazuje = 0m };
        var stavkaB = new StavkaNaloga { NalogId = nalogB.NalogId, RedniBroj = 1, BrojKonta = "4350", PartnerId = partnerB.PartnerId, Duguje = 0m, Potrazuje = 4000m };
        db.StavkeNaloga.AddRange(stavkaA, stavkaB);
        await db.SaveChangesAsync();

        var kompenzacija = new Kompenzacija
        {
            Vrsta = VrstaKompenzacije.Dvojna,
            PartnerId = 0,
            NazivPartnera = "Konto 204900",
            KontoPartnera1 = "204900",
            Partner2Id = partnerB.PartnerId,
            NazivPartnera2 = partnerB.Naziv,
            Datum = new DateTime(2026, 2, 1),
            Stavke = new List<KompenzacijaStavka>
            {
                new KompenzacijaStavka { RedniBroj = 1, StavkaNalogaId = stavkaA.StavkaNalogaId, PartnerId = 0, Strana = "Duguje", BrojKonta = "204900", IznosFakture = 4000m, IznosPreostalo = 4000m, IznosZaKompenzaciju = 4000m },
                new KompenzacijaStavka { RedniBroj = 2, StavkaNalogaId = stavkaB.StavkaNalogaId, PartnerId = partnerB.PartnerId, Strana = "Potražuje", BrojKonta = "4350", IznosFakture = 4000m, IznosPreostalo = 4000m, IznosZaKompenzaciju = 4000m }
            }
        };

        var service = new KompenzacijaService(db);
        var sacuvana = await service.SacuvajKompenzacijuAsync(kompenzacija);
        var (success, message, nalogId) = await service.KnjiziIZatvoriKompenzacijuAsync(sacuvana.KompenzacijaId);
        Assert.True(success, message);

        var nalog = await db.Nalozi.Include(n => n.Stavke).FirstAsync(n => n.NalogId == nalogId);
        // Linija koja zatvara legacy konto MORA nositi tačan broj konta (204900), ne generički "2040" —
        // inače se ne bi videla na kartici tog konkretnog konta (GetOtvoreneStavkeZaKontoAsync filtrira tačno).
        Assert.Contains(nalog.Stavke, s => s.BrojKonta == "204900" && s.PartnerId == null && s.Potrazuje == 4000m);
        Assert.DoesNotContain(nalog.Stavke, s => s.BrojKonta == "2040");

        var kartica = await new OtvoreneStavkeService(db).GetOtvoreneStavkeZaKontoAsync("204900");
        Assert.Equal(0m, kartica[^1].Saldo);
    }

    [Fact]
    public async Task KnjiziIZatvoriKompenzacijuAsync_DvaRazlicitaLegacyKontaNaIstojStrani_OstajuOdvojeneLinije()
    {
        using var db = GetInMemoryDbContext();

        // Trojna kompenzacija: jedan pravi kupac (partnerA, Duguje 10000) prebija se sa DVA RAZLIČITA
        // legacy dobavljačka konta (435900: 6000, 435950: 4000) — oba imaju PartnerId=0, pa grupisanje
        // SAMO po PartnerId-ju bi ih pogrešno stopilo u jednu zajedničku zatvarajuću liniju od 10000.
        var partnerA = new Partner { SifraPartnera = "A", Naziv = "Partner A" };
        db.Partneri.Add(partnerA);
        await db.SaveChangesAsync();

        var nalogA = new Nalog { BrojNaloga = 1, DatumNaloga = new DateTime(2026, 1, 1), IsKnjizen = true };
        var nalogB1 = new Nalog { BrojNaloga = 2, DatumNaloga = new DateTime(2026, 1, 2), IsKnjizen = true };
        var nalogB2 = new Nalog { BrojNaloga = 3, DatumNaloga = new DateTime(2026, 1, 3), IsKnjizen = true };
        db.Nalozi.AddRange(nalogA, nalogB1, nalogB2);
        await db.SaveChangesAsync();

        var stavkaA = new StavkaNaloga { NalogId = nalogA.NalogId, RedniBroj = 1, BrojKonta = "2040", PartnerId = partnerA.PartnerId, Duguje = 10000m, Potrazuje = 0m };
        var stavkaB1 = new StavkaNaloga { NalogId = nalogB1.NalogId, RedniBroj = 1, BrojKonta = "435900", PartnerId = null, Duguje = 0m, Potrazuje = 6000m };
        var stavkaB2 = new StavkaNaloga { NalogId = nalogB2.NalogId, RedniBroj = 1, BrojKonta = "435950", PartnerId = null, Duguje = 0m, Potrazuje = 4000m };
        db.StavkeNaloga.AddRange(stavkaA, stavkaB1, stavkaB2);
        await db.SaveChangesAsync();

        var kompenzacija = new Kompenzacija
        {
            Vrsta = VrstaKompenzacije.Asignacija,
            PartnerId = partnerA.PartnerId,
            NazivPartnera = partnerA.Naziv,
            Partner2Id = 0,
            KontoPartnera2 = "435900",
            Partner3Id = 0,
            KontoPartnera3 = "435950",
            Datum = new DateTime(2026, 2, 1),
            Stavke = new List<KompenzacijaStavka>
            {
                new KompenzacijaStavka { RedniBroj = 1, StavkaNalogaId = stavkaA.StavkaNalogaId, PartnerId = partnerA.PartnerId, Strana = "Duguje", BrojKonta = "2040", IznosFakture = 10000m, IznosPreostalo = 10000m, IznosZaKompenzaciju = 10000m },
                new KompenzacijaStavka { RedniBroj = 2, StavkaNalogaId = stavkaB1.StavkaNalogaId, PartnerId = 0, Strana = "Potražuje", BrojKonta = "435900", IznosFakture = 6000m, IznosPreostalo = 6000m, IznosZaKompenzaciju = 6000m },
                new KompenzacijaStavka { RedniBroj = 3, StavkaNalogaId = stavkaB2.StavkaNalogaId, PartnerId = 0, Strana = "Potražuje", BrojKonta = "435950", IznosFakture = 4000m, IznosPreostalo = 4000m, IznosZaKompenzaciju = 4000m }
            }
        };

        var service = new KompenzacijaService(db);
        var sacuvana = await service.SacuvajKompenzacijuAsync(kompenzacija);
        var (success, message, nalogId) = await service.KnjiziIZatvoriKompenzacijuAsync(sacuvana.KompenzacijaId);
        Assert.True(success, message);

        var nalog = await db.Nalozi.Include(n => n.Stavke).FirstAsync(n => n.NalogId == nalogId);
        Assert.Equal(3, nalog.Stavke.Count); // ne 2 — 435900 i 435950 moraju ostati odvojene linije
        Assert.Contains(nalog.Stavke, s => s.BrojKonta == "435900" && s.Duguje == 6000m);
        Assert.Contains(nalog.Stavke, s => s.BrojKonta == "435950" && s.Duguje == 4000m);

        var zatvaranjeService = new ZatvaranjeStavkiService(db);
        Assert.Empty(await zatvaranjeService.GetOtvoreneStavkeZaKontoAsync("435900"));
        Assert.Empty(await zatvaranjeService.GetOtvoreneStavkeZaKontoAsync("435950"));
    }
}

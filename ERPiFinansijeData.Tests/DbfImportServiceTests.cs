using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class DbfImportServiceTests
{
    private static Dictionary<string, string> Row(params (string Key, string Value)[] fields)
        => new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)), StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void MapKonto_ReadsOpisKontaAsNaziv_AndAllAdresneKolone()
    {
        var row = Row(
            ("ST_KON", ""),
            ("KONTO", "435252"),
            ("OPIS_KONTA", "\"ROMA\" KAFE"),
            ("ULICA_I_BR", ""),
            ("MESTO_I_BR", "BELA PALANKA"),
            ("ZIRO_RACUN", "42501-685-2-21137"),
            ("TELEFON", ""));

        var konto = DbfImportService.MapKonto(row);

        Assert.NotNull(konto);
        Assert.Equal("435252", konto!.BrojKonta);
        Assert.Equal("\"ROMA\" KAFE", konto.NazivKonta);
        Assert.Equal("BELA PALANKA", konto.Mesto);
        Assert.Equal("42501-685-2-21137", konto.ZiroRacun);
        Assert.Null(konto.Ulica);
        Assert.Null(konto.Telefon);
    }

    [Fact]
    public void MapKonto_ReturnsNull_WhenKontoColumnEmpty()
    {
        // ST_KON popunjen a KONTO prazno — ovo je slučaj koji je ranije bag čitao kao broj konta.
        var row = Row(("ST_KON", "999"), ("KONTO", ""), ("OPIS_KONTA", "Nešto"));

        Assert.Null(DbfImportService.MapKonto(row));
    }

    [Fact]
    public void MapPartner_ReadsAllKolone_AndFallsBackToSequentialSifra()
    {
        var row = Row(
            ("KONTO", ""),
            ("OPIS_KONTA", "Partner DOO"),
            ("ULICA_I_BR", "Glavna 1"),
            ("MESTO_I_BR", "Niš"),
            ("ZIRO_RACUN", "160-1-1"),
            ("TELEFON", "018-123"));

        var partner = DbfImportService.MapPartner(row, fallbackBroj: 7);

        Assert.NotNull(partner);
        Assert.Equal("0007", partner!.SifraPartnera);
        Assert.Equal("Partner DOO", partner.Naziv);
        Assert.Equal("Glavna 1", partner.Adresa);
        Assert.Equal("Niš", partner.PttIMesto);
    }

    [Fact]
    public void MapPartner_ReturnsNull_WhenOpisKontaEmpty()
    {
        var row = Row(("KONTO", "123"), ("OPIS_KONTA", ""));

        Assert.Null(DbfImportService.MapPartner(row, 1));
    }

    [Fact]
    public void MapMagacin_MapsRacunopolToNazivMagacina()
    {
        var row = Row(("SIFRA", "001"), ("RACUNOPOL", "CENTRALNI MAGACIN"));

        var magacin = DbfImportService.MapMagacin(row);

        Assert.NotNull(magacin);
        Assert.Equal("001", magacin!.SifraMagacina);
        Assert.Equal("CENTRALNI MAGACIN", magacin.NazivMagacina);
        Assert.Null(magacin.OdgovornoLice);
    }

    [Fact]
    public void MapArtikal_ReadsJedMereAndSelektovan()
    {
        var row = Row(
            ("SIFRA", "A1"),
            ("KLAS_SIFRA", "12"),
            ("PAKOVANJE", "1/1"),
            ("JED_MERE", "kg"),
            ("NAZIV", "Šećer"),
            ("TAR_BROJ", "5"),
            ("SELEKTOVAN", "T"));

        var artikal = DbfImportService.MapArtikal(row);

        Assert.NotNull(artikal);
        Assert.Equal("kg", artikal!.JedinicaMere);
        Assert.Equal("1/1", artikal.Pakovanje);
        Assert.Equal("5", artikal.TarifniBroj);
        Assert.Equal("12", artikal.KlasifikacionaSifra);
        Assert.True(artikal.Selektovan);
    }

    [Fact]
    public void MapMaterijal_ReadsSifraNazivJmPakovanje_IndependentlyOfArtikli()
    {
        // M_SIFR.DBF nema TAR_BROJ/KLAS_SIFRA/SELEKTOVAN kolone — Materijal ih ni ne poseduje,
        // za razliku od Artikal (Robno) koji dolazi iz posebne ARTIKLI.DBF serije šifara.
        var row = Row(
            ("SIFRA", "03030"),
            ("PAKOVANJE", ""),
            ("JED_MERE", "vr"),
            ("NAZIV", "cement"));

        var materijal = DbfImportService.MapMaterijal(row);

        Assert.NotNull(materijal);
        Assert.Equal("03030", materijal!.SifraArtikla);
        Assert.Equal("cement", materijal.Naziv);
        Assert.Equal("vr", materijal.JedinicaMere);
        Assert.Null(materijal.Pakovanje);
    }

    [Fact]
    public void MapMaterijal_ReturnsNull_WhenSifraEmpty()
    {
        var row = Row(("NAZIV", "cement"));

        Assert.Null(DbfImportService.MapMaterijal(row));
    }

    [Fact]
    public void GroupNalogRows_GroupsByBrNaloga_AndSkipsZeroBroj()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(("BR_NALOGA", "5"), ("KONTO", "100"), ("DUGUJE", "10"), ("POTRAZUJE", "0")),
            Row(("BR_NALOGA", "5"), ("KONTO", "200"), ("DUGUJE", "0"), ("POTRAZUJE", "10")),
            Row(("BR_NALOGA", "0"), ("KONTO", "300"), ("DUGUJE", "0"), ("POTRAZUJE", "0")),
        };

        var groups = DbfImportService.GroupNalogRows(rows);

        Assert.Single(groups);
        Assert.Equal(5, groups[0].BrojNaloga);
        Assert.Equal(2, groups[0].Redovi.Count);
    }

    [Fact]
    public void MapNalogGrupa_ReadsBrNalogaDatNalogaAndKnjizen_AndBalancesTotals()
    {
        var redovi = new List<Dictionary<string, string>>
        {
            Row(("DAT_NALOGA", "20260101"), ("KNJIZEN", "1"), ("BR_DOKUM", "R-1"), ("KONTO", "100"), ("DUGUJE", "500"), ("POTRAZUJE", "0"), ("RED_BROJ", "1")),
            Row(("DAT_NALOGA", "20260101"), ("KNJIZEN", "1"), ("BR_DOKUM", "R-1"), ("KONTO", "200"), ("DUGUJE", "0"), ("POTRAZUJE", "500"), ("RED_BROJ", "2")),
        };

        var nalog = DbfImportService.MapNalogGrupa(42, redovi);

        Assert.NotNull(nalog);
        Assert.Equal(42, nalog!.BrojNaloga);
        Assert.Equal(new DateTime(2026, 1, 1), nalog.DatumNaloga);
        Assert.True(nalog.IsKnjizen);
        Assert.Equal(500m, nalog.UkupnoDuguje);
        Assert.Equal(500m, nalog.UkupnoPotrazuje);
        Assert.Equal(2, nalog.Stavke.Count);
    }

    /// <summary>Red prepisan 1:1 iz C:\FIRME\ARHSTO\Radni\kor01\KAL_NAL.DBF (kalkulacija 1, stavka 1).</summary>
    private static Dictionary<string, string> StvarniKalNalRed() => Row(
        ("BR_KALKUL", "1"), ("DATUM", "20260202"), ("MAG_PRIMA", "030"), ("RED_BROJ", "1"),
        ("ARTIKAL", "02060"), ("KOLICINA", "750.0000"), ("CENA", "87.99"), ("IZNOS", "65992.50"),
        ("TROSKOVI", "0.00"), ("NABAVNA", "65992.50"), ("RAZLIKA_PR", "23.12005152100618"),
        ("RAZLIKA_IZ", "15257.50"), ("PROD_BEZ_P", "81250.00"), ("POREZ_PR", "20.00"),
        ("POREZ_IZ", "16250.00"), ("POS_P_PR", ""), ("POS_P_IZ", ""), ("PREN_POR", "0.00"),
        ("PREN_P_POR", ""), ("PROD_SA_P", "97500.00"), ("PROD_PO_JM", "130.00"),
        ("KNJIZEN", "1"), ("STARA_CENA", "110.00"), ("POR_ZA_UPL", "16250.00"));

    [Fact]
    public void MapKalkulacijaStavka_ReadsStvarneKoloneIzKalNal()
    {
        var stavka = DbfImportService.MapKalkulacijaStavka(StvarniKalNalRed());

        Assert.NotNull(stavka);
        Assert.Equal(1, stavka!.RedniBroj);
        Assert.Equal("02060", stavka.SifraArtikla);
        Assert.Equal(750m, stavka.Kolicina);
        Assert.Equal(87.99m, stavka.NabavnaCena);
        Assert.Equal(65992.50m, stavka.Iznos);
        Assert.Equal(65992.50m, stavka.NabavnaVrednost);
        // Ove četiri su ranije uvek ostajale 0 jer je mapper tražio RAZLIKA/POREZ/PROD_VRED/PROD_CENA,
        // a KAL_NAL.DBF nosi RAZLIKA_IZ/POREZ_IZ/PROD_SA_P/PROD_PO_JM.
        Assert.Equal(15257.50m, stavka.RazlikaIznos);
        Assert.Equal(16250.00m, stavka.PorezIznos);
        Assert.Equal(97500.00m, stavka.ProdajnaVrednost);
        Assert.Equal(130.00m, stavka.ProdajnaCena);
        // Ranije uopšte nisu bile mapirane:
        Assert.Equal(23.12005152100618m, stavka.RazlikaProcenat);
        Assert.Equal(81250.00m, stavka.ProdajnaVrednostBezPoreza);
        Assert.Equal(20.00m, stavka.PorezProcenat);
        Assert.Equal(0m, stavka.PrenetiPorez);
        Assert.Equal(16250.00m, stavka.PorezZaUplatu);
        Assert.Equal(110.00m, stavka.StaraCena);
        Assert.True(stavka.IsKnjizen);
    }

    [Fact]
    public void MapKalkulacijaStavka_IzvodiIzvedeneKolone_KadaSuPrazneUDbf()
    {
        // Starije baze imaju popunjenu samo osnovu; izvedene vrednosti moraju pratiti MAT6.PRG:870-877.
        var row = Row(
            ("BR_KALKUL", "5"), ("RED_BROJ", "2"), ("ARTIKAL", "15083"),
            ("KOLICINA", "10.0000"), ("CENA", "100.00"), ("IZNOS", "1000.00"),
            ("TROSKOVI", "200.00"), ("RAZLIKA_IZ", "300.00"), ("POREZ_PR", "20.00"),
            ("POREZ_IZ", "300.00"), ("PREN_POR", "50.00"));

        var stavka = DbfImportService.MapKalkulacijaStavka(row);

        Assert.NotNull(stavka);
        Assert.Equal(1200.00m, stavka!.NabavnaVrednost);              // iznos + troskovi
        Assert.Equal(1500.00m, stavka.ProdajnaVrednostBezPoreza);     // nabavna + razlika_iz
        Assert.Equal(1800.00m, stavka.ProdajnaVrednost);              // prod_bez_p + porez_iz
        Assert.Equal(180.00m, stavka.ProdajnaCena);                   // prod_sa_p / kolicina
        Assert.Equal(250.00m, stavka.PorezZaUplatu);                  // porez_iz - pren_por
    }

    [Fact]
    public void GroupKalkulacijaStavke_PreskaceLegacyBrojacRed()
    {
        // Prvi zapis u KAL_NAL.DBF je Clipper brojač (BR_KALKUL=0), ne stavka.
        var rows = new List<Dictionary<string, string>>
        {
            Row(("BR_KALKUL", "0"), ("KOLICINA", "128.0000")),
            StvarniKalNalRed()
        };

        var grupe = DbfImportService.GroupKalkulacijaStavke(rows);

        Assert.False(grupe.ContainsKey(0));
        Assert.Single(grupe[1]);
    }

    [Fact]
    public void MapKalkulacija_IzvodiProcenteIzIznosa_IOstavljaPrazneDatumeNull()
    {
        // Red iz C:\FIRME\ARHSTO\Radni\kor01\KALKULAC.DBF (kalkulacija 1), bez datuma otpremnice.
        var row = Row(
            ("BR_KALKUL", "1"), ("DATUM", "20260202"), ("DOBAVLJAC", "432509"),
            ("OTPREM_BR", "1/26"), ("OTPREM_DAT", ""), ("RACUN_BR", "ifvp-14/"), ("RACUN_DAT", "20260130"),
            ("NAB_VRED", "65992.50"), ("TRANS_TROS", "0.00"), ("SVEGA_TROS", "0.00"),
            ("SVEGA_NAB", "65992.50"), ("RAZLIKA", "15257.50"), ("POREZ", "16250.00"),
            ("PROD_VRED", "97500.00"), ("MAG_PRIMA", "030"), ("KNJIZEN", "1"));

        var kalk = DbfImportService.MapKalkulacija(row);

        Assert.NotNull(kalk);
        Assert.Equal("030", kalk!.SifraMagacina);
        Assert.Null(kalk.DatumOtpremnice);
        Assert.Equal(new DateTime(2026, 1, 30), kalk.DatumRacuna);
        Assert.Equal(23.1201m, kalk.MarzaProcenat);        // 100 * 15257.50 / 65992.50
        Assert.Equal(20m, kalk.PoreskaStopaProcenat);      // 100 * 16250 / 81250
    }

    [Fact]
    public void MapMaloprodajnaKalkulacija_ReadsMalkulacKolone()
    {
        var row = Row(
            ("PRODAVNICA", "1"), ("BR_KALKUL", "12"), ("DATUM", "20260202"),
            ("MAG_PRIMA", "030"), ("MAG_DAJE", "010"), ("DOBAVLJAC", "432509"),
            ("OTPREM_BR", "7/26"), ("OTPREM_DAT", "20260202"), ("RACUN_BR", "IFR-01"), ("RACUN_DAT", "20260130"),
            ("TRANS_TROS", "100.00"), ("TROS_USKL", "0.00"), ("UTOV_ISTOV", "0.00"), ("TR_OSIGUR", "0.00"),
            ("OSTALI", "0.00"), ("KNJIZEN", "1"), ("T_KNJIZEN", "1"), ("SVEGA_TROS", "100.00"),
            ("RABAT_PR", "5.00"), ("NAB_VRED", "1000.00"), ("SVEGA_NAB", "1100.00"),
            ("RAZLIKA", "220.00"), ("POREZ", "264.00"), ("PROD_VRED", "1584.00"), ("RABAT_IZ", "50.00"));

        var malk = DbfImportService.MapMaloprodajnaKalkulacija(row);

        Assert.NotNull(malk);
        Assert.Equal(1, malk!.SifraProdavnice);
        Assert.Equal(12, malk.BrojKalkulacije);
        Assert.Equal("030", malk.SifraMagacinaPrima);
        Assert.Equal("010", malk.SifraMagacinaDaje);
        Assert.Equal(5.00m, malk.RabatPri);
        Assert.Equal(50.00m, malk.RabatIznos);
        Assert.Equal(20m, malk.MarzaProcenat);             // 100 * 220 / 1100
        Assert.Equal(20m, malk.PoreskaStopaProcenat);      // 100 * 264 / 1320
        Assert.True(malk.IsKnjizen);
        Assert.True(malk.IsTrgovinskiKnjizen);
    }

    [Fact]
    public void MapMaloprodajnaKalkulacijaStavka_ReadsMalNalSpecificneKolone()
    {
        var row = Row(
            ("PRODAVNICA", "1"), ("BR_KALKUL", "12"), ("MAG_PRIMA", "030"), ("RED_BROJ", "3"),
            ("ARTIKAL", "19076"), ("KOLICINA", "12.0000"), ("CENA", "343.00"), ("IZNOS", "4116.00"),
            ("TROSKOVI", "0.00"), ("NABAVNA", "4116.00"), ("RAZLIKA_PR", "21.47716229348882"),
            ("RAZLIKA_IZ", "884.00"), ("PROD_BEZ_P", "5000.00"), ("POREZ_PR", "20.00"),
            ("POREZ_IZ", "1000.00"), ("PREN_POR", "0.00"), ("POS_POR_PR", "0.00"),
            ("PROD_SA_P", "6000.00"), ("PROD_PO_JM", "500.00"), ("KNJIZEN", "1"), ("T_KNJIZEN", "0"),
            ("NAZ_ROBE", "ČOKOLADA 100g"), ("JED_MERE", "kom"), ("TARIFNI", "3"),
            ("POR_ZA_UPL", "1000.00"), ("TAKSA", "12.50"), ("BR_RAZDUZ", "45"));

        var stavka = DbfImportService.MapMaloprodajnaKalkulacijaStavka(row);

        Assert.NotNull(stavka);
        Assert.Equal(3, stavka!.RedniBroj);
        Assert.Equal(884.00m, stavka.RazlikaIznos);
        Assert.Equal(1000.00m, stavka.PorezIznos);
        Assert.Equal(6000.00m, stavka.ProdajnaVrednost);
        Assert.Equal(500.00m, stavka.ProdajnaCena);
        Assert.Equal(5000.00m, stavka.ProdajnaVrednostBezPoreza);
        Assert.Equal(12.50m, stavka.Taksa);
        Assert.Equal("3", stavka.TarifniBroj);
        Assert.Equal(45, stavka.BrojRazduzenja);
        Assert.Equal("ČOKOLADA 100g", stavka.NazivArtikla);
        Assert.Equal("kom", stavka.JedinicaMere);
        Assert.True(stavka.IsKnjizen);
        Assert.False(stavka.IsTrgovinskiKnjizen);
    }

    [Fact]
    public void DopuniZbiroveIzStavki_PopunjavaSamoNultaPolja()
    {
        // Legacy zaglavlje sa nulama (22 takva u ARHSTO\kor03), a stavke nose stvarne iznose.
        var kalk = new Kalkulacija { BrojKalkulacije = 8, SvegaTroskovi = 40m };
        kalk.Stavke.Add(new KalkulacijaStavka
        {
            RedniBroj = 1, SifraArtikla = "A", Iznos = 1000m, Troskovi = 200m,
            NabavnaVrednost = 1200m, RazlikaIznos = 300m, PorezIznos = 300m, ProdajnaVrednost = 1800m
        });

        DbfImportService.DopuniZbiroveIzStavki(kalk);

        Assert.Equal(1000m, kalk.NabavnaVrednost);
        Assert.Equal(1200m, kalk.SvegaNabavno);
        Assert.Equal(300m, kalk.Razlika);
        Assert.Equal(300m, kalk.Porez);
        Assert.Equal(1800m, kalk.ProdajnaVrednost);
        Assert.Equal(25m, kalk.MarzaProcenat);            // 100 * 300 / 1200
        Assert.Equal(20m, kalk.PoreskaStopaProcenat);     // 100 * 300 / 1500
        Assert.Equal(40m, kalk.SvegaTroskovi);            // već popunjeno u zaglavlju — ne dira se
    }

    [Fact]
    public void DopuniZbiroveIzStavki_NeDiraZaglavljeBezStavki()
    {
        var kalk = new Kalkulacija { BrojKalkulacije = 9 };

        DbfImportService.DopuniZbiroveIzStavki(kalk);

        Assert.Equal(0m, kalk.ProdajnaVrednost);
        Assert.Equal(0m, kalk.MarzaProcenat);
    }

    [Fact]
    public void GroupMaloprodajnaKalkulacijaStavke_RazdvajaIsteBrojevePoProdavnicama()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(("PRODAVNICA", "1"), ("BR_KALKUL", "7"), ("ARTIKAL", "A")),
            Row(("PRODAVNICA", "2"), ("BR_KALKUL", "7"), ("ARTIKAL", "B")),
            Row(("PRODAVNICA", "2"), ("BR_KALKUL", "7"), ("ARTIKAL", "C")),
        };

        var grupe = DbfImportService.GroupMaloprodajnaKalkulacijaStavke(rows);

        Assert.Equal(2, grupe.Count);
        Assert.Single(grupe[(1, 7)]);
        Assert.Equal(2, grupe[(2, 7)].Count);
    }
}

using AccountingData;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingData.Tests;

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


}

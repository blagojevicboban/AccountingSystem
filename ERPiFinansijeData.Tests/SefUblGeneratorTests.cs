using System.Xml.Linq;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class SefUblGeneratorTests
{
    [Fact]
    public void GenerisiUblXml_VracaValidanXmlSaObaveznimUblTagovima()
    {
        // Arrange
        var firma = new Firma
        {
            FirmaId = 1,
            Naziv = "PRODAVAC D.O.O.",
            Pib = "100000001",
            MaticniBroj = "07000001",
            Adresa = "Bulevar 1",
            PttIMesto = "11000 Beograd",
            ZiroRacun = "205-0000000012345-67"
        };

        var partner = new Partner
        {
            PartnerId = 1,
            Naziv = "KUPAC D.O.O.",
            Pib = "100000002",
            MaticniBroj = "07000002",
            Adresa = "Knez Mihailova 10",
            PttIMesto = "11000 Beograd"
        };

        var racun = new RacunOtpremnica
        {
            RacunOtpremnicaId = 100,
            BrojRacuna = 5,
            DatumRacuna = new DateTime(2026, 8, 1),
            RokPlacanjaDana = 15,
            UkupnoOsnovica = 10000m,
            UkupnoPdv = 2000m,
            UkupnoZaUplatu = 12000m,
            Stavke = new List<RacunOtpremnicaStavka>
            {
                new RacunOtpremnicaStavka
                {
                    RedniBroj = 1,
                    SifraArtikla = "A001",
                    NazivArtikla = "Test Usloga / Roba",
                    Kolicina = 2m,
                    ProdajnaCena = 5000m,
                    StopaPdv = 20m,
                    Osnovica = 10000m,
                    IznosPdv = 2000m,
                    Ukupno = 12000m
                }
            }
        };

        // Act
        string xml = SefUblGenerator.GenerisiUblXml(racun, firma, partner);

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("Invoice", xml);
        Assert.Contains("urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2021", xml);
        Assert.Contains("100000001", xml); // PIB Prodavca
        Assert.Contains("100000002", xml); // PIB Kupca
        Assert.Contains("12000.00", xml);  // Ukupan iznos za uplatu

        var xdoc = XDocument.Parse(xml);
        Assert.NotNull(xdoc.Root);
        Assert.Equal("Invoice", xdoc.Root.Name.LocalName);
    }
}

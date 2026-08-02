using System.Xml.Linq;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Xunit;

namespace ERPiFinansijeData.Tests;

public class PpPdvXmlGeneratorTests
{
    [Fact]
    public void GenerisiPpPdvXml_VracaValidanXmlSaPpPdvSekcijamaIPoljima()
    {
        // Arrange
        var firma = new Firma
        {
            FirmaId = 1,
            Naziv = "TEST FIRMA D.O.O.",
            Pib = "123456789",
            Adresa = "Knez Mihailova 1",
            PttIMesto = "11000 Beograd"
        };

        var obracun = new PdvObracunResult
        {
            OdDatuma = new DateTime(2026, 8, 1),
            DoDatuma = new DateTime(2026, 8, 31),

            KirUkupnoSaPdv = 120000m,
            KirOsnovica20 = 100000m,
            KirPdv20 = 20000m,
            KirOsnovica10 = 0m,
            KirPdv10 = 0m,
            KirOslobodjen = 0m,

            KprUkupnoSaPdv = 60000m,
            KprOsnovica20 = 50000m,
            KprPdv20 = 10000m,
            KprOsnovica10 = 0m,
            KprPdv10 = 0m,
            KprOslobodjen = 0m
        };

        // Act
        string xml = PpPdvXmlGenerator.GenerisiPpPdvXml(obracun, firma, zahtevZaPovracaj: false);

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("PoreskaPrijavaPPPDV", xml);
        Assert.Contains("123456789", xml);
        Assert.Contains("100000.00", xml); // Polje001 (Osnovica 20%)
        Assert.Contains("20000.00", xml);  // Polje101 (PDV 20%)
        Assert.Contains("50000.00", xml);  // Polje009 (KPR Osnovica 20%)
        Assert.Contains("10000.00", xml);  // Polje109 (KPR PDV 20% i Polje111 obaveza)

        var xdoc = XDocument.Parse(xml);
        Assert.NotNull(xdoc.Root);
        Assert.Equal("PoreskaPrijavaPPPDV", xdoc.Root.Name.LocalName);
    }
}

using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using AccountingData.Models;

namespace AccountingData.Services;

public static class PpPdvXmlGenerator
{
    private static readonly XNamespace PpPdvNs = "http://prijave.purs.gov.rs/namespaces/pppdv";

    /// <summary>
    /// Generiše zvanični XML fajl Obrasca PP-PDV za ePorezi portal Poreske uprave RS.
    /// </summary>
    public static string GenerisiPpPdvXml(PdvObracunResult obracun, Firma firma, bool zahtevZaPovracaj = false)
    {
        var culture = CultureInfo.InvariantCulture;

        decimal obavezaZaUplatu = obracun.PdvRazlika > 0 ? obracun.PdvRazlika : 0m;
        decimal iznosZaPovracaj = obracun.PdvRazlika < 0 ? Math.Abs(obracun.PdvRazlika) : 0m;

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(PpPdvNs + "PoreskaPrijavaPPPDV",
                // 1. Podaci o prijavi i obvezniku
                new XElement(PpPdvNs + "PodaciOPrijavi",
                    new XElement(PpPdvNs + "PoreskiIdentifikacioniBroj", firma.Pib ?? string.Empty),
                    new XElement(PpPdvNs + "NazivObveznika", firma.Naziv),
                    new XElement(PpPdvNs + "SedisteObveznika", $"{firma.Adresa ?? ""}, {firma.PttIMesto ?? ""}".Trim(',', ' ')),
                    new XElement(PpPdvNs + "PoreskiPeriodOd", obracun.OdDatuma.ToString("yyyy-MM-dd")),
                    new XElement(PpPdvNs + "PoreskiPeriodDo", obracun.DoDatuma.ToString("yyyy-MM-dd")),
                    new XElement(PpPdvNs + "TipPrijave", "1"), // 1 = Redovna prijava
                    new XElement(PpPdvNs + "IzmenaPrijave", "0")
                ),

                // 2. Promet dobara i usluga i obračunati PDV (Izlazni / KIR)
                new XElement(PpPdvNs + "ObracunatiPdv",
                    new XElement(PpPdvNs + "Polje001", obracun.KirOsnovica20.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje101", obracun.KirPdv20.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje002", obracun.KirOsnovica10.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje102", obracun.KirPdv10.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje003", obracun.KirOslobodjen.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje004", "0.00"),
                    new XElement(PpPdvNs + "Polje008", (obracun.KirOsnovica20 + obracun.KirOsnovica10 + obracun.KirOslobodjen).ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje108", obracun.KirUkupanPdv.ToString("F2", culture))
                ),

                // 3. Prethodni porez (Ulazni / KPR)
                new XElement(PpPdvNs + "PrethodniPdv",
                    new XElement(PpPdvNs + "Polje009", obracun.KprOsnovica20.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje109", obracun.KprPdv20.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje010", obracun.KprOsnovica10.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje110", obracun.KprPdv10.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje008Prethodni", (obracun.KprOsnovica20 + obracun.KprOsnovica10 + obracun.KprOslobodjen).ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje108Prethodni", obracun.KprUkupanPdv.ToString("F2", culture))
                ),

                // 4. Konačni obračun (Obaveza / Povraćaj / Kredit)
                new XElement(PpPdvNs + "KonacniObracun",
                    new XElement(PpPdvNs + "Polje111", obavezaZaUplatu.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje112", iznosZaPovracaj.ToString("F2", culture)),
                    new XElement(PpPdvNs + "Polje113", (iznosZaPovracaj > 0 && zahtevZaPovracaj) ? "1" : "0")
                )
            )
        );

        using var ms = new MemoryStream();
        using var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true
        });
        doc.Save(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}

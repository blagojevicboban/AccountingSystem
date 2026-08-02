using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeData.Services;

public static class SefUblGenerator
{
    private static readonly XNamespace InvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    /// <summary>
    /// Generiše zvanični UBL 2.1 XML dokument za srpski SEF po specifikaciji e-Faktura.
    /// </summary>
    public static string GenerisiUblXml(RacunOtpremnica racun, Firma firma, Partner partner)
    {
        var culture = CultureInfo.InvariantCulture;

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(InvoiceNs + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
                new XElement(Cbc + "CustomizationID", "urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.rs:srbdt:2021"),
                new XElement(Cbc + "ProfileID", "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"),
                new XElement(Cbc + "ID", $"F-{racun.BrojRacuna:D5}"),
                new XElement(Cbc + "IssueDate", racun.DatumRacuna.ToString("yyyy-MM-dd")),
                new XElement(Cbc + "DueDate", (racun.RokPlacanja ?? racun.DatumRacuna.AddDays(racun.RokPlacanjaDana)).ToString("yyyy-MM-dd")),
                new XElement(Cbc + "InvoiceTypeCode", racun.TipDokumenta == TipRacunOtpremnice.Predracun ? "386" : "380"),
                new XElement(Cbc + "DocumentCurrencyCode", "RSD"),

                // Supplier (Izdavalac)
                BuildSupplierParty(firma),

                // Customer (Primalac)
                BuildCustomerParty(partner),

                // Payment Means (Način plaćanja i tekući račun)
                new XElement(Cac + "PaymentMeans",
                    new XElement(Cbc + "PaymentMeansCode", "30"),
                    new XElement(Cac + "PayeeFinancialAccount",
                        new XElement(Cbc + "ID", firma.ZiroRacun ?? string.Empty)
                    )
                ),

                // Tax Subtotals & Total
                BuildTaxTotal(racun, culture),

                // Legal Monetary Totals
                BuildMonetaryTotal(racun, culture),

                // Invoice Lines
                BuildInvoiceLines(racun, culture)
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

    private static XElement BuildSupplierParty(Firma firma)
    {
        return new XElement(Cac + "AccountingSupplierParty",
            new XElement(Cac + "Party",
                new XElement(Cbc + "EndpointID", new XAttribute("schemeID", "9948"), firma.Pib ?? string.Empty),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", firma.Naziv)
                ),
                new XElement(Cac + "PostalAddress",
                    new XElement(Cbc + "StreetName", firma.Adresa ?? string.Empty),
                    new XElement(Cbc + "CityName", firma.PttIMesto ?? "Beograd"),
                    new XElement(Cac + "Country",
                        new XElement(Cbc + "IdentificationCode", "RS")
                    )
                ),
                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cbc + "CompanyID", $"RS{firma.Pib}"),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT")
                    )
                ),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", firma.Naziv),
                    new XElement(Cbc + "CompanyID", firma.MaticniBroj ?? string.Empty)
                )
            )
        );
    }

    private static XElement BuildCustomerParty(Partner partner)
    {
        return new XElement(Cac + "AccountingCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cbc + "EndpointID", new XAttribute("schemeID", "9948"), partner.Pib ?? string.Empty),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", partner.Naziv)
                ),
                new XElement(Cac + "PostalAddress",
                    new XElement(Cbc + "StreetName", partner.Adresa ?? string.Empty),
                    new XElement(Cbc + "CityName", partner.PttIMesto ?? "Beograd"),
                    new XElement(Cac + "Country",
                        new XElement(Cbc + "IdentificationCode", "RS")
                    )
                ),
                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cbc + "CompanyID", $"RS{partner.Pib}"),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "VAT")
                    )
                ),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", partner.Naziv),
                    new XElement(Cbc + "CompanyID", partner.MaticniBroj ?? string.Empty)
                )
            )
        );
    }

    private static XElement BuildTaxTotal(RacunOtpremnica racun, CultureInfo culture)
    {
        // Grupisanje po PDV stopama (npr. 20% i 10%)
        var grupisano = racun.Stavke
            .GroupBy(s => s.StopaPdv)
            .Select(g => new
            {
                Stopa = g.Key,
                Osnovica = g.Sum(x => x.Osnovica),
                PdvIznos = g.Sum(x => x.IznosPdv)
            })
            .ToList();

        var taxTotal = new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount", new XAttribute("currencyID", "RSD"), racun.UkupnoPdv.ToString("F2", culture))
        );

        foreach (var item in grupisano)
        {
            string categoryId = item.Stopa > 0 ? "S" : "Z";
            taxTotal.Add(
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount", new XAttribute("currencyID", "RSD"), item.Osnovica.ToString("F2", culture)),
                    new XElement(Cbc + "TaxAmount", new XAttribute("currencyID", "RSD"), item.PdvIznos.ToString("F2", culture)),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID", categoryId),
                        new XElement(Cbc + "Percent", item.Stopa.ToString("F2", culture)),
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "ID", "VAT")
                        )
                    )
                )
            );
        }

        return taxTotal;
    }

    private static XElement BuildMonetaryTotal(RacunOtpremnica racun, CultureInfo culture)
    {
        return new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount", new XAttribute("currencyID", "RSD"), racun.UkupnoOsnovica.ToString("F2", culture)),
            new XElement(Cbc + "TaxExclusiveAmount", new XAttribute("currencyID", "RSD"), racun.UkupnoOsnovica.ToString("F2", culture)),
            new XElement(Cbc + "TaxInclusiveAmount", new XAttribute("currencyID", "RSD"), racun.UkupnoZaUplatu.ToString("F2", culture)),
            new XElement(Cbc + "PayableAmount", new XAttribute("currencyID", "RSD"), racun.UkupnoZaUplatu.ToString("F2", culture))
        );
    }

    private static IEnumerable<XElement> BuildInvoiceLines(RacunOtpremnica racun, CultureInfo culture)
    {
        var lines = new List<XElement>();
        int rbr = 1;

        foreach (var s in racun.Stavke)
        {
            string categoryId = s.StopaPdv > 0 ? "S" : "Z";

            lines.Add(
                new XElement(Cac + "InvoiceLine",
                    new XElement(Cbc + "ID", rbr++.ToString()),
                    new XElement(Cbc + "InvoicedQuantity", new XAttribute("unitCode", "PCE"), s.Kolicina.ToString("F3", culture)),
                    new XElement(Cbc + "LineExtensionAmount", new XAttribute("currencyID", "RSD"), s.Osnovica.ToString("F2", culture)),
                    new XElement(Cac + "Item",
                        new XElement(Cbc + "Name", string.IsNullOrWhiteSpace(s.NazivArtikla) ? $"Artikal {s.SifraArtikla}" : s.NazivArtikla),
                        new XElement(Cac + "ClassifiedTaxCategory",
                            new XElement(Cbc + "ID", categoryId),
                            new XElement(Cbc + "Percent", s.StopaPdv.ToString("F2", culture)),
                            new XElement(Cac + "TaxScheme",
                                new XElement(Cbc + "ID", "VAT")
                            )
                        )
                    ),
                    new XElement(Cac + "Price",
                        new XElement(Cbc + "PriceAmount", new XAttribute("currencyID", "RSD"), s.ProdajnaCena.ToString("F2", culture))
                    )
                )
            );
        }

        return lines;
    }
}

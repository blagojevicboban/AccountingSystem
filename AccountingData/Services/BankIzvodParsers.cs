using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AccountingData.Models;

namespace AccountingData.Services;

public interface IBankIzvodParser
{
    BankIzvod Parse(string content);
}

public static class BankIzvodFormatDetector
{
    public static BankIzvodFormat DetectFormat(string filePath, string content)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".txt" || ext == ".sta" || ext == ".940")
        {
            if (content.Contains(":20:") || content.Contains(":25:") || content.Contains(":61:"))
                return BankIzvodFormat.Mt940Txt;
        }

        if (ext == ".xml" || content.TrimStart().StartsWith("<"))
        {
            if (content.Contains("Document") && content.Contains("camt.053"))
                return BankIzvodFormat.Camt053Xml;
            if (content.Contains("<Izvod") || content.Contains("<StavkaIzvoda") || content.Contains("<Halcom"))
                return BankIzvodFormat.HalcomXml;
            if (content.Contains("<Asseco") || content.Contains("<OfficeBanking") || content.Contains("<IzvodBanka"))
                return BankIzvodFormat.AssecoXml;
            
            // Fallback xml inspection
            if (content.Contains("camt.053")) return BankIzvodFormat.Camt053Xml;
            if (content.Contains("Stavka")) return BankIzvodFormat.HalcomXml;
            return BankIzvodFormat.HalcomXml;
        }

        return BankIzvodFormat.Nepoznato;
    }
}

public static class BankIzvodParserFactory
{
    public static IBankIzvodParser GetParser(BankIzvodFormat format)
    {
        return format switch
        {
            BankIzvodFormat.HalcomXml => new HalcomXmlParser(),
            BankIzvodFormat.AssecoXml => new AssecoXmlParser(),
            BankIzvodFormat.Camt053Xml => new Camt053XmlParser(),
            BankIzvodFormat.Mt940Txt => new Mt940TxtParser(),
            _ => new HalcomXmlParser()
        };
    }
}

/// <summary>
/// Parser za Halcom E-Bank XML format (standard za Intesa, Unicredit, OTP, NLB Komercijalnu itd. u RS)
/// </summary>
public class HalcomXmlParser : IBankIzvodParser
{
    public BankIzvod Parse(string content)
    {
        var result = new BankIzvod { Format = BankIzvodFormat.HalcomXml };
        var doc = XDocument.Parse(content);
        var root = doc.Root;
        if (root == null) return result;

        var header = root.Element("Zaglavlje") ?? root.Element("Header") ?? root;
        result.BrojIzvoda = GetVal(header, "BrojIzvoda", "Broj", "StatementNumber") ?? "1";
        result.BrojRacuna = GetVal(header, "BrojRacuna", "Racun", "Account") ?? "";
        
        string datumStr = GetVal(header, "DatumIzvoda", "Datum", "Date") ?? "";
        if (DateTime.TryParse(datumStr, out var d)) result.DatumIzvoda = d;

        result.PocetnoStanje = ParseDecimal(GetVal(header, "PocetnoStanje", "PrethodnoStanje", "OpeningBalance"));
        result.KrajnjeStanje = ParseDecimal(GetVal(header, "KrajnjeStanje", "NovoStanje", "ClosingBalance"));

        var stavkeNodes = root.Descendants().Where(e => e.Name.LocalName.Equals("StavkaIzvoda", StringComparison.OrdinalIgnoreCase) ||
                                                       e.Name.LocalName.Equals("Stavka", StringComparison.OrdinalIgnoreCase) ||
                                                       e.Name.LocalName.Equals("Promet", StringComparison.OrdinalIgnoreCase));

        int redniBroj = 1;
        foreach (var node in stavkeNodes)
        {
            var item = new BankIzvodStavka
            {
                BrojStavke = redniBroj++,
                SvrhaDoznake = GetVal(node, "SvrhaDoznake", "Svrha", "Opis", "Purpose") ?? "",
                NazivPartnera = GetVal(node, "NazivPartnera", "Naziv", "Nalogodavac", "Primalac", "Name") ?? "",
                RacunPartnera = GetVal(node, "RacunPartnera", "Racun", "Account") ?? "",
                PibPartnera = GetVal(node, "PibPartnera", "PIB", "Pib", "VatId") ?? "",
                PozivNaBroj = GetVal(node, "PozivNaBroj", "PozivNaBrojOdobrenja", "PozivNaBrojZaduzenja", "Reference") ?? ""
            };

            string dateStr = GetVal(node, "DatumValute", "Datum", "Date") ?? "";
            if (DateTime.TryParse(dateStr, out var valutaDate)) item.DatumValute = valutaDate;
            else item.DatumValute = result.DatumIzvoda;

            decimal iznos = ParseDecimal(GetVal(node, "Iznos", "Amount"));
            string smer = GetVal(node, "Smer", "Tip", "DugujePotrazuje", "Indicator") ?? "";

            if (smer.Equals("D", StringComparison.OrdinalIgnoreCase) ||
                smer.Contains("Zaduzenje", StringComparison.OrdinalIgnoreCase) ||
                smer.Contains("Isplata", StringComparison.OrdinalIgnoreCase))
            {
                item.Tip = BankIzvodStavkaTip.Isplata;
            }
            else
            {
                item.Tip = BankIzvodStavkaTip.Uplata;
            }

            item.Iznos = Math.Abs(iznos);
            result.Stavke.Add(item);
        }

        result.UkupnoUplata = result.Stavke.Where(s => s.Tip == BankIzvodStavkaTip.Uplata).Sum(s => s.Iznos);
        result.UkupnoIsplata = result.Stavke.Where(s => s.Tip == BankIzvodStavkaTip.Isplata).Sum(s => s.Iznos);

        return result;
    }

    private static string? GetVal(XElement parent, params string[] names)
    {
        foreach (var name in names)
        {
            var elem = parent.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (elem != null && !string.IsNullOrWhiteSpace(elem.Value))
                return elem.Value.Trim();
            
            var attr = parent.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (attr != null && !string.IsNullOrWhiteSpace(attr.Value))
                return attr.Value.Trim();
        }
        return null;
    }

    private static decimal ParseDecimal(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0m;
        val = val.Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}

/// <summary>
/// Parser za Asseco / Office Banking XML format
/// </summary>
public class AssecoXmlParser : IBankIzvodParser
{
    private readonly HalcomXmlParser _fallback = new();
    public BankIzvod Parse(string content)
    {
        var izvod = _fallback.Parse(content);
        izvod.Format = BankIzvodFormat.AssecoXml;
        return izvod;
    }
}

/// <summary>
/// Parser za ISO 20022 CAMT.053 XML format
/// </summary>
public class Camt053XmlParser : IBankIzvodParser
{
    public BankIzvod Parse(string content)
    {
        var result = new BankIzvod { Format = BankIzvodFormat.Camt053Xml };
        var doc = XDocument.Parse(content);
        
        var stmt = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Stmt");
        if (stmt == null) return result;

        result.BrojIzvoda = stmt.Element(stmt.Name.Namespace + "Id")?.Value ?? "1";
        
        var acct = stmt.Element(stmt.Name.Namespace + "Acct");
        if (acct != null)
        {
            var idElem = acct.Descendants().FirstOrDefault(e => e.Name.LocalName == "Id");
            result.BrojRacuna = idElem?.Value ?? "";
        }

        var balNodes = stmt.Elements().Where(e => e.Name.LocalName == "Bal");
        foreach (var bal in balNodes)
        {
            var tp = bal.Descendants().FirstOrDefault(e => e.Name.LocalName == "Cd")?.Value;
            var amt = bal.Descendants().FirstOrDefault(e => e.Name.LocalName == "Amt")?.Value;
            decimal dAmt = ParseDecimal(amt);
            if (tp == "OPBD" || tp == "PRBD") result.PocetnoStanje = dAmt;
            if (tp == "CLBD") result.KrajnjeStanje = dAmt;
        }

        int redni = 1;
        var ntryNodes = stmt.Elements().Where(e => e.Name.LocalName == "Ntry");
        foreach (var ntry in ntryNodes)
        {
            var amtVal = ntry.Element(ntry.Name.Namespace + "Amt")?.Value;
            var cdtDbtInd = ntry.Element(ntry.Name.Namespace + "CdtDbtInd")?.Value;
            
            var dtElem = ntry.Descendants().FirstOrDefault(e => e.Name.LocalName == "Dt");
            DateTime dtVal = DateTime.Today;
            if (dtElem != null && DateTime.TryParse(dtElem.Value, out var parsedDt))
            {
                dtVal = parsedDt;
            }

            var rtrInf = ntry.Descendants().FirstOrDefault(e => e.Name.LocalName == "TxDetails" || e.Name.LocalName == "NtryDtls");
            string ustrd = ntry.Descendants().FirstOrDefault(e => e.Name.LocalName == "Ustrd")?.Value ?? "";
            string dbtrName = ntry.Descendants().FirstOrDefault(e => e.Name.LocalName == "Dbtr")?.Element(ntry.Name.Namespace + "Nm")?.Value ?? "";
            string cdtrName = ntry.Descendants().FirstOrDefault(e => e.Name.LocalName == "Cdtr")?.Element(ntry.Name.Namespace + "Nm")?.Value ?? "";
            string refNum = ntry.Descendants().FirstOrDefault(e => e.Name.LocalName == "Ref")?.Value ?? "";

            var item = new BankIzvodStavka
            {
                BrojStavke = redni++,
                DatumValute = dtVal,
                Iznos = ParseDecimal(amtVal),
                SvrhaDoznake = ustrd,
                PozivNaBroj = refNum,
                Tip = (cdtDbtInd == "CRDT" || cdtDbtInd == "DBIT") ? (cdtDbtInd == "DBIT" ? BankIzvodStavkaTip.Isplata : BankIzvodStavkaTip.Uplata) : BankIzvodStavkaTip.Uplata,
                NazivPartnera = cdtDbtInd == "DBIT" ? cdtrName : dbtrName
            };

            result.Stavke.Add(item);
        }

        result.UkupnoUplata = result.Stavke.Where(s => s.Tip == BankIzvodStavkaTip.Uplata).Sum(s => s.Iznos);
        result.UkupnoIsplata = result.Stavke.Where(s => s.Tip == BankIzvodStavkaTip.Isplata).Sum(s => s.Iznos);

        return result;
    }

    private static decimal ParseDecimal(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0m;
        val = val.Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}

/// <summary>
/// Parser za SWIFT MT940 tekstualni format bankarskog izvoda
/// </summary>
public class Mt940TxtParser : IBankIzvodParser
{
    public BankIzvod Parse(string content)
    {
        var result = new BankIzvod { Format = BankIzvodFormat.Mt940Txt };
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        int redni = 1;
        BankIzvodStavka? currentStavka = null;

        foreach (var line in lines)
        {
            string tLine = line.Trim();
            if (tLine.StartsWith(":20:"))
            {
                result.BrojIzvoda = tLine.Substring(4).Trim();
            }
            else if (tLine.StartsWith(":25:"))
            {
                result.BrojRacuna = tLine.Substring(4).Trim();
            }
            else if (tLine.StartsWith(":60F:") || tLine.StartsWith(":60M:"))
            {
                // Opening Balance e.g. :60F:C260801RSD150000,00
                string body = tLine.Substring(5);
                if (body.Length > 10)
                {
                    string amtStr = body.Substring(10).Replace(",", ".");
                    result.PocetnoStanje = decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
                }
            }
            else if (tLine.StartsWith(":62F:") || tLine.StartsWith(":62M:"))
            {
                // Closing Balance
                string body = tLine.Substring(5);
                if (body.Length > 10)
                {
                    string amtStr = body.Substring(10).Replace(",", ".");
                    result.KrajnjeStanje = decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
                }
            }
            else if (tLine.StartsWith(":61:"))
            {
                // Statement Line e.g. :61:2608010801CD12500,00NTRF...
                if (currentStavka != null)
                {
                    result.Stavke.Add(currentStavka);
                }

                currentStavka = new BankIzvodStavka { BrojStavke = redni++ };
                string body = tLine.Substring(4);

                // Parse Date (YYMMDD)
                if (body.Length >= 6 && DateTime.TryParseExact(body.Substring(0, 6), "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    currentStavka.DatumValute = dt;
                }

                // C / D / RC / RD
                bool isDebit = body.Contains("D") && !body.Contains("CD");
                currentStavka.Tip = isDebit ? BankIzvodStavkaTip.Isplata : BankIzvodStavkaTip.Uplata;

                // Extract Amount
                var match = Regex.Match(body, @"[CD]([0-9]+[,\.][0-9]{2})");
                if (match.Success)
                {
                    string amtStr = match.Groups[1].Value.Replace(",", ".");
                    currentStavka.Iznos = decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
                }
            }
            else if (tLine.StartsWith(":86:") && currentStavka != null)
            {
                // Information to Account Owner
                string info = tLine.Substring(4).Trim();
                currentStavka.SvrhaDoznake = info;

                // Extract potential PIB
                var pibMatch = Regex.Match(info, @"\b(1[0-9]{8}|2[0-9]{8}|[0-9]{9})\b");
                if (pibMatch.Success)
                {
                    currentStavka.PibPartnera = pibMatch.Value;
                }
            }
            else if (currentStavka != null && tLine.StartsWith("?"))
            {
                currentStavka.SvrhaDoznake += " " + tLine.TrimStart('?');
            }
        }

        if (currentStavka != null)
        {
            result.Stavke.Add(currentStavka);
        }

        result.UkupnoUplata = result.Stavke.Where(s => s.Tip == BankIzvodStavkaTip.Uplata).Sum(s => s.Iznos);
        result.UkupnoIsplata = result.Stavke.Where(s => s.Tip == BankIzvodStavkaTip.Isplata).Sum(s => s.Iznos);

        return result;
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingData.Models;

namespace AccountingData.Services;

public class DmsOcrInvoiceParser
{
    public Task<OcrRacunResult> ProcessDocumentAsync(string filePath)
    {
        return Task.Run(() =>
        {
            var result = new OcrRacunResult();
            if (!File.Exists(filePath))
            {
                result.StatusPoruka = "Fajl ne postoji.";
                return result;
            }

            string rawText = ExtractTextFromFile(filePath);
            result.RawText = rawText;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                result.StatusPoruka = "Dokument je prazan ili tekst nije prepoznat.";
                return result;
            }

            ParseInvoiceFields(rawText, result);
            return result;
        });
    }

    private static string ExtractTextFromFile(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".txt" || ext == ".xml" || ext == ".csv")
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        // Za PDF ili slikovne fajlove, čitamo tekstualne sadržaje/metadate
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            string raw = Encoding.UTF8.GetString(bytes);

            // Ako PDF sadrži čist tekstualni tok
            var sb = new StringBuilder();
            var matches = Regex.Matches(raw, @"\(([^()]+)\)\s*Tj");
            foreach (Match m in matches)
            {
                if (m.Groups.Count > 1) sb.AppendLine(m.Groups[1].Value);
            }

            if (sb.Length > 20) return sb.ToString();

            // Alternativna ekstrakcija alfanumeričkih linija
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Where(l => l.Contains("PIB") || l.Contains("Faktura") || l.Contains("Racun") || l.Contains("PDV") || l.Contains("Ukupno"))
                           .Take(50);
            
            string combined = string.Join("\n", lines);
            return string.IsNullOrWhiteSpace(combined) ? Path.GetFileNameWithoutExtension(filePath) : combined;
        }
        catch
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }

    public static void ParseInvoiceFields(string text, OcrRacunResult result)
    {
        // 1. Ekstrakcija PIB-a dobavljača (9 cifara koji počinje sa 1 ili 2)
        var pibMatch = Regex.Match(text, @"\bPIB:?\s*([12]\d{8})\b", RegexOptions.IgnoreCase);
        if (!pibMatch.Success)
        {
            pibMatch = Regex.Match(text, @"\b([12]\d{8})\b");
        }

        if (pibMatch.Success)
        {
            result.PibDobavljaca = pibMatch.Groups[1].Value;
        }

        // 2. Ekstrakcija broja računa
        var brMatch = Regex.Match(text, @"(?:Faktura|Račun|Racun|Br\.?|Broj|PFR\s*broj):?\s*([A-Za-z0-9\-\/]{3,20})", RegexOptions.IgnoreCase);
        if (brMatch.Success)
        {
            result.BrojRacuna = brMatch.Groups[1].Value;
        }
        else
        {
            var genericBr = Regex.Match(text, @"\b(202\d[-/]\d{1,6})\b");
            if (genericBr.Success) result.BrojRacuna = genericBr.Value;
        }

        // 3. Ekstrakcija datuma (dd.MM.yyyy)
        var dateMatches = Regex.Matches(text, @"\b(\d{1,2}[\.\/]\d{1,2}[\.\/]\d{4})\b");
        if (dateMatches.Count > 0)
        {
            if (DateTime.TryParse(dateMatches[0].Value.Replace("/", "."), out var d1))
                result.DatumRacuna = d1;
            
            if (dateMatches.Count > 1 && DateTime.TryParse(dateMatches[1].Value.Replace("/", "."), out var d2))
                result.ValutaDospela = d2;
            else if (result.DatumRacuna.HasValue)
                result.ValutaDospela = result.DatumRacuna.Value.AddDays(15);
        }

        // 4. Ekstrakcija iznosa (Ukupno / Bruto / Za uplatu)
        var ukupanMatch = Regex.Match(text, @"(?:Za\s*uplatu|Ukupno|UKUPNO|BRUTO|Svega):?\s*([\d\.\,]+)", RegexOptions.IgnoreCase);
        if (ukupanMatch.Success)
        {
            result.UkupanIznosBruto = ParseDecimal(ukupanMatch.Groups[1].Value);
        }

        // 5. Ekstrakcija PDV iznosa
        var pdvMatch = Regex.Match(text, @"(?:PDV|Iznos\s*PDV-a|Porez):?\s*([\d\.\,]+)", RegexOptions.IgnoreCase);
        if (pdvMatch.Success)
        {
            result.PdvIznos = ParseDecimal(pdvMatch.Groups[1].Value);
        }

        // 6. Proračun ili ekstrakcija Osnovice (Neto)
        var osnovicaMatch = Regex.Match(text, @"(?:Osnovica|NETO|Neto):?\s*([\d\.\,]+)", RegexOptions.IgnoreCase);
        if (osnovicaMatch.Success)
        {
            result.OsnovicaNeto = ParseDecimal(osnovicaMatch.Groups[1].Value);
        }

        // Usklađivanje relacije: Osnovica + PDV == Ukupno Bruto
        if (result.UkupanIznosBruto > 0 && result.PdvIznos > 0 && result.OsnovicaNeto <= 0)
        {
            result.OsnovicaNeto = result.UkupanIznosBruto - result.PdvIznos;
        }
        else if (result.UkupanIznosBruto > 0 && result.OsnovicaNeto > 0 && result.PdvIznos <= 0)
        {
            result.PdvIznos = result.UkupanIznosBruto - result.OsnovicaNeto;
        }
        else if (result.OsnovicaNeto > 0 && result.PdvIznos > 0 && result.UkupanIznosBruto <= 0)
        {
            result.UkupanIznosBruto = result.OsnovicaNeto + result.PdvIznos;
        }

        // Procena pouzdanosti (Confidence)
        if (!string.IsNullOrEmpty(result.PibDobavljaca) && result.UkupanIznosBruto > 0 && !string.IsNullOrEmpty(result.BrojRacuna))
        {
            result.Confidence = OcrMatchConfidence.Exact;
            result.StatusPoruka = "🟢 Uspešno izvučeni PIB, broj računa i iznosi!";
        }
        else if (!string.IsNullOrEmpty(result.PibDobavljaca) || result.UkupanIznosBruto > 0)
        {
            result.Confidence = OcrMatchConfidence.High;
            result.StatusPoruka = "🟡 Delimično izvučeni podaci sa računa.";
        }
        else
        {
            result.Confidence = OcrMatchConfidence.Low;
            result.StatusPoruka = "🔴 Nisu pronađeni prepoznatljivi iznosi ili PIB.";
        }
    }

    private static decimal ParseDecimal(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0m;
        val = val.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}

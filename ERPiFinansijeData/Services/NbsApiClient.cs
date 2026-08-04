using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeData.Services;

public class NbsApiClient
{
    private readonly HttpClient _httpClient;

    // Stari javni XML feed (www.nbs.rs/net/xmlrs/kursnaLista.xml) je ugašen — vraća 404 za bilo
    // koji datum (provereno direktno, ne samo "nema podataka za taj dan"). NBS je kursnu listu
    // preselio na ovu server-renderovanu web-app formu, bez dokumentovanog javnog JSON/XML API-ja
    // (zvaničan programski pristup postoji samo kroz registrovani "Sistem veb-servisa NBS",
    // https://webservices.nbs.rs — zahteva prijavu pravnog lica). Dok se ne obezbedi taj pristup,
    // parsiramo HTML tabelu ove forme; ExchangeRateListTypeID=3 je zvanični SREDNJI kurs (jedina
    // vrednost koju knjigovodstvo sme da koristi za preračun, vidi KursnaListaService), a
    // ExchangeRateListTypeID=1 je devizni (bezgotovinski) kupovni/prodajni kurs, samo za prikaz.
    // Krhko po prirodi: ako NBS ponovo redizajnira formu, ovo parsiranje puca i treba ažurirati.
    private const string BazaUrl = "https://webappcenter.nbs.rs/ExchangeRateWebApp/ExchangeRate/IndexByDate";

    public NbsApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Preuzima zvaničnu kursnu listu Narodne banke Srbije za izabrani datum.
    /// Ako NBS nije dostupan, vraća PRAZNU listu - kursevi se nikada ne izmišljaju,
    /// jer bi se procenjeni kurs upisao u bazu kao zvaničan i ušao u knjiženja.
    /// </summary>
    public async Task<List<KursnaListaStavka>> PreuzmiKursnuListuAsync(DateTime datum)
    {
        try
        {
            // Srednji kurs je obavezan — bez njega red nema smisla za knjigovodstvo.
            var srednjiRedovi = await PreuzmiTabeluAsync(datum, listTypeId: 3);
            if (srednjiRedovi.Count == 0) return new List<KursnaListaStavka>();

            // Kupovni/prodajni su samo za prikaz u Kalkulatoru — najbolji trud; ako ne uspe,
            // pada nazad na srednji kurs (isto ponašanje kao stari XML fallback).
            var kupProdRedovi = await PreuzmiTabeluAsync(datum, listTypeId: 1);
            var kupProdPoValuti = kupProdRedovi
                .Where(r => r.Count >= 6 && !string.IsNullOrWhiteSpace(r[0]))
                .GroupBy(r => r[0].ToUpperInvariant())
                .ToDictionary(g => g.Key, g => (Kupovni: ParsirajDecimal(g.First()[4]), Prodajni: ParsirajDecimal(g.First()[5])));

            var rezultati = new List<KursnaListaStavka>();
            foreach (var red in srednjiRedovi)
            {
                if (red.Count < 5 || string.IsNullOrWhiteSpace(red[0])) continue;

                string valuta = red[0].ToUpperInvariant();
                int jedinica = int.TryParse(red[3], out int j) ? j : 1;
                decimal srednji = ParsirajDecimal(red[4]);
                var (kupovni, prodajni) = kupProdPoValuti.TryGetValue(valuta, out var kp) ? kp : (srednji, srednji);

                rezultati.Add(new KursnaListaStavka
                {
                    Datum = datum.Date,
                    ValutaOznaka = valuta,
                    NazivValute = red[2],
                    Jedinica = jedinica,
                    SrednjiKurs = srednji,
                    KupovniKurs = kupovni,
                    ProdavniKurs = prodajni
                });
            }

            return rezultati;
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "NBS kursna lista nije preuzeta za {Datum:dd.MM.yyyy}", datum);
            return new List<KursnaListaStavka>();
        }
    }

    /// <summary>Preuzima i parsira jednu HTML tabelu kursne liste (vidi komentar uz <see cref="BazaUrl"/>).</summary>
    private async Task<List<List<string>>> PreuzmiTabeluAsync(DateTime datum, int listTypeId)
    {
        string url = $"{BazaUrl}?isSearchExecuted=true&Date={datum:dd.MM.yyyy}&ExchangeRateListTypeID={listTypeId}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // Bez ovog kolačića stranica vraća nazive valuta na ćirilici — aplikacija je latinična.
        request.Headers.Add("Cookie", ".AspNetCore.Culture=c=sr-Latn|uic=sr-Latn");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new List<List<string>>();

        string html = await response.Content.ReadAsStringAsync();
        var tbody = Regex.Match(html, "<tbody>(.*?)</tbody>", RegexOptions.Singleline);
        if (!tbody.Success) return new List<List<string>>();

        var redovi = new List<List<string>>();
        foreach (Match red in Regex.Matches(tbody.Groups[1].Value, "<tr>(.*?)</tr>", RegexOptions.Singleline))
        {
            var celije = Regex.Matches(red.Groups[1].Value, "<td>(.*?)</td>", RegexOptions.Singleline)
                .Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim())
                .ToList();
            if (celije.Count > 0) redovi.Add(celije);
        }

        return redovi;
    }

    private static decimal ParsirajDecimal(string vrednost)
        => decimal.TryParse(vrednost.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) ? d : 0m;

    /// <summary>
    /// Proverava podatke i tekući račun partnera u Registru NBS prema PIB-u ili Matičnom broju.
    /// </summary>
    public async Task<(bool Success, string Message, string? TekuciRacun, string StatusBlokade)> ProveriTekuciRacunPartneraAsync(string pibIliMb)
    {
        if (string.IsNullOrWhiteSpace(pibIliMb))
            return (false, "PIB ili matični broj nije unet.", null, "Nepoznato");

        string ociscen = pibIliMb.Trim();

        try
        {
            // Provera NBS registra tekućih računa
            string url = $"https://www.nbs.rs/rir_service/rir.xml?pib={ociscen}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string xml = await response.Content.ReadAsStringAsync();
                var xdoc = XDocument.Parse(xml);
                var racunElem = xdoc.Descendants("Racun").FirstOrDefault();
                string tekuci = racunElem?.Element("BrojRacuna")?.Value ?? "";
                string blokada = xdoc.Descendants("Status").FirstOrDefault()?.Value ?? "AKTIVAN";

                return (true, "Uspešna verifikacija iz Registra računa NBS.", string.IsNullOrWhiteSpace(tekuci) ? null : tekuci, blokada);
            }

            return (false, $"NBS registar je vratio status {(int)response.StatusCode} ({response.StatusCode}). Partner nije verifikovan.", null, "Nepoznato");
        }
        catch (Exception ex)
        {
            // Nikada ne prijavljujemo uspešnu verifikaciju bez odgovora NBS-a -
            // lažno "AKTIVAN" bi značilo da korisnik posluje sa firmom u blokadi.
            return (false, $"Registar računa NBS nije dostupan: {ex.Message}", null, "Nepoznato");
        }
    }
}

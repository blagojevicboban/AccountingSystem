using System.Globalization;
using System.Xml.Linq;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeData.Services;

public class NbsApiClient
{
    private readonly HttpClient _httpClient;

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
        var rezultati = new List<KursnaListaStavka>();

        try
        {
            // Zvanični NBS XML endpoint
            string url = $"https://www.nbs.rs/net/xmlrs/kursnaLista.xml?datum={datum:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string xml = await response.Content.ReadAsStringAsync();
                var xdoc = XDocument.Parse(xml);

                foreach (var elem in xdoc.Descendants("Stavka"))
                {
                    string valuta = elem.Element("Valuta")?.Value ?? elem.Element("OznakaValute")?.Value ?? "";
                    if (string.IsNullOrWhiteSpace(valuta)) continue;

                    int jedinica = int.TryParse(elem.Element("Jedinica")?.Value, out int j) ? j : 1;

                    string srednjiStr = elem.Element("SrednjiKurs")?.Value?.Replace(',', '.') ?? "0";
                    string kupovniStr = elem.Element("KupovniKurs")?.Value?.Replace(',', '.') ?? srednjiStr;
                    string prodavniStr = elem.Element("ProdavniKurs")?.Value?.Replace(',', '.') ?? srednjiStr;

                    decimal srednji = decimal.TryParse(srednjiStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal s) ? s : 0m;
                    decimal kupovni = decimal.TryParse(kupovniStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal k) ? k : srednji;
                    decimal prodavni = decimal.TryParse(prodavniStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p) ? p : srednji;

                    rezultati.Add(new KursnaListaStavka
                    {
                        Datum = datum.Date,
                        ValutaOznaka = valuta.ToUpperInvariant(),
                        NazivValute = elem.Element("NazivValute")?.Value ?? valuta,
                        Jedinica = jedinica,
                        SrednjiKurs = srednji,
                        KupovniKurs = kupovni,
                        ProdavniKurs = prodavni
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "NBS kursna lista nije preuzeta za {Datum:dd.MM.yyyy}", datum);
        }

        return rezultati;
    }

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

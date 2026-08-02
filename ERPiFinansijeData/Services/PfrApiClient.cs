using System.Net.Http.Json;
using System.Text.Json;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeData.Services;

public class PfrApiClient
{
    private readonly HttpClient _httpClient;

    public PfrApiClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Proverava dostupnost LPFR/VPFR servisa.
    /// </summary>
    public async Task<(bool Success, string Message)> TestirajPfrKonekcijuAsync(PfrPostavke postavke)
    {
        if (string.IsNullOrWhiteSpace(postavke.PfrUrl))
            return (false, "PFR URL nije definisan.");

        try
        {
            string url = postavke.PfrUrl.TrimEnd('/') + "/api/v1/status";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(postavke.PacKod))
            {
                request.Headers.TryAddWithoutValidation("PAC", postavke.PacKod);
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return (true, "PFR servis je aktivan i dostupan (STATUS 200 OK).");
            }

            return (false, $"PFR vraća status: {response.StatusCode} ({(int)response.StatusCode})");
        }
        catch (Exception ex)
        {
            if (postavke.SimulatorMod)
                return (true, $"PFR nije dostupan ({ex.Message}), ali je SIMULATOR MOD uključen.\n\nUPOZORENJE: računi se neće stvarno fiskalizovati.");

            return (false, $"PFR servis nije dostupan na {postavke.PfrUrl}.\n\nDetalji: {ex.Message}");
        }
    }

    /// <summary>
    /// Šalje zahtev za fiskalizaciju računa PFR servisu.
    /// </summary>
    public async Task<(bool Success, bool Simulacija, string Message, PfrOdgovor? Odgovor)> FiskalizujRacunAsync(PfrZahtev zahtev, PfrPostavke postavke)
    {
        string greska;

        try
        {
            string url = postavke.PfrUrl.TrimEnd('/') + "/api/v1/invoices";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(zahtev)
            };

            if (!string.IsNullOrWhiteSpace(postavke.PacKod))
            {
                request.Headers.TryAddWithoutValidation("PAC", postavke.PacKod);
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var pfrRes = await response.Content.ReadFromJsonAsync<PfrOdgovor>();

                if (pfrRes == null || string.IsNullOrWhiteSpace(pfrRes.InvoiceNumber))
                    return (false, false, "PFR je vratio prazan ili neispravan odgovor - račun NIJE fiskalizovan.", null);

                return (true, false, "Fiskalni račun je uspešno izdat i verifikovan u PFR-u.", pfrRes);
            }

            string telo = await response.Content.ReadAsStringAsync();
            greska = $"PFR je odbio zahtev - status {(int)response.StatusCode} ({response.StatusCode}). {telo}".Trim();
        }
        catch (Exception ex)
        {
            greska = $"Komunikacija sa PFR servisom na {postavke.PfrUrl} nije uspela: {ex.Message}";
        }

        // Bez izričito uključenog simulator moda, neuspeh je GREŠKA - račun se ne sme
        // prikazati kao fiskalizovan jer bi to bila neistinita poreska evidencija.
        if (!postavke.SimulatorMod)
            return (false, false, $"Račun NIJE fiskalizovan.\n\n{greska}", null);

        return (true, true, $"SIMULIRAN račun (PFR nije dostupan).\n\nOVAJ RAČUN NIJE FISKALIZOVAN i nema pravnu vrednost.\n\n{greska}", GenerisiSimuliraniOdgovor(zahtev));
    }

    /// <summary>
    /// Generiše simulirani odgovor za testiranje i obuku. Broj računa je izričito
    /// označen prefiksom SIMULACIJA, a verifikacioni URL se NE generiše jer takav
    /// račun ne postoji u PURS sistemu.
    /// </summary>
    private static PfrOdgovor GenerisiSimuliraniOdgovor(PfrZahtev zahtev)
    {
        string randCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        string brojRacuna = $"SIMULACIJA-{randCode}";
        decimal ukupno = zahtev.Items.Sum(i => i.TotalAmount);

        return new PfrOdgovor
        {
            InvoiceNumber = brojRacuna,
            InvoiceCounter = $"{DateTime.Now.Month}/{DateTime.Now.Millisecond}S",
            SdcDateTime = DateTime.Now,
            TotalAmount = ukupno,
            VerificationUrl = string.Empty,
            Journal = "========================================\n" +
                      "   *** SIMULACIJA - NIJE FISKALNI ***   \n" +
                      "========================================\n" +
                      $"Broj: {brojRacuna}\n" +
                      $"Vreme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n" +
                      $"Ukupno: {ukupno:N2} RSD\n" +
                      $"Kasir: {zahtev.Cashier}\n" +
                      "========================================\n" +
                      "Ovaj dokument NIJE fiskalni račun i nije\n" +
                      "evidentiran u sistemu Poreske uprave.\n"
        };
    }
}

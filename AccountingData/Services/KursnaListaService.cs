using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class KursnaListaService
{
    private readonly AccountingDbContext _db;
    private readonly NbsApiClient _nbsClient;

    public KursnaListaService(AccountingDbContext db, NbsApiClient? nbsClient = null)
    {
        _db = db;
        _nbsClient = nbsClient ?? new NbsApiClient();
    }

    /// <summary>
    /// Vraća kursnu listu za zadati datum. Ako nije u bazi, preuzima je sa NBS i sačuvava.
    /// </summary>
    public async Task<List<KursnaListaStavka>> GetKursnaListaZaDatumAsync(DateTime datum)
    {
        var targetDate = datum.Date;
        var postojeci = await _db.KursneListeStavke
            .Where(k => k.Datum == targetDate)
            .OrderBy(k => k.ValutaOznaka)
            .ToListAsync();

        if (postojeci.Count > 0)
            return postojeci;

        // Preuzimanje sa NBS-a
        var noviKursevi = await _nbsClient.PreuzmiKursnuListuAsync(targetDate);
        if (noviKursevi.Count > 0)
        {
            _db.KursneListeStavke.AddRange(noviKursevi);
            await _db.SaveChangesAsync();
        }

        return noviKursevi;
    }

    /// <summary>
    /// Forsira ponovno preuzimanje i osvežavanje kursne liste sa NBS API-ja za zadati datum.
    /// </summary>
    public async Task<List<KursnaListaStavka>> OsveziSaNbsAsync(DateTime datum)
    {
        var targetDate = datum.Date;

        // Prvo preuzimanje, pa tek onda brisanje - da nedostupan NBS ne obriše
        // već sačuvanu kursnu listu i ostavi knjiženja bez kursa.
        var novi = await _nbsClient.PreuzmiKursnuListuAsync(targetDate);
        if (novi.Count == 0)
            return new List<KursnaListaStavka>();

        var stari = await _db.KursneListeStavke.Where(k => k.Datum == targetDate).ToListAsync();
        if (stari.Count > 0)
            _db.KursneListeStavke.RemoveRange(stari);

        _db.KursneListeStavke.AddRange(novi);
        await _db.SaveChangesAsync();

        return novi;
    }

    /// <summary>
    /// Preračunava devizni iznos u dinare (RSD) po zvaničnom srednjem kursu NBS na zadati datum.
    /// </summary>
    public async Task<decimal> PretvoriDevizeURsdAsync(decimal iznos, string valutaOznaka, DateTime datum)
    {
        if (string.IsNullOrWhiteSpace(valutaOznaka) || valutaOznaka.Equals("RSD", StringComparison.OrdinalIgnoreCase))
            return iznos;

        var kursevi = await GetKursnaListaZaDatumAsync(datum);
        var stavka = kursevi.FirstOrDefault(k => k.ValutaOznaka.Equals(valutaOznaka, StringComparison.OrdinalIgnoreCase));

        // Bez kursa se iznos NE sme vratiti nepromenjen - to bi značilo da se npr.
        // 100 EUR proknjiži kao 100 RSD.
        if (stavka == null || stavka.SrednjiKurs <= 0)
            throw new InvalidOperationException(
                $"Ne postoji kurs za valutu {valutaOznaka} na dan {datum:dd.MM.yyyy}. " +
                "Preuzmite kursnu listu sa NBS-a ili je unesite ručno pre knjiženja.");

        return Math.Round((iznos * stavka.SrednjiKurs) / stavka.Jedinica, 2);
    }
}

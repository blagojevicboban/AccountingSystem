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
        var stari = await _db.KursneListeStavke.Where(k => k.Datum == targetDate).ToListAsync();
        if (stari.Count > 0)
        {
            _db.KursneListeStavke.RemoveRange(stari);
            await _db.SaveChangesAsync();
        }

        var novi = await _nbsClient.PreuzmiKursnuListuAsync(targetDate);
        if (novi.Count > 0)
        {
            _db.KursneListeStavke.AddRange(novi);
            await _db.SaveChangesAsync();
        }

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

        if (stavka == null || stavka.SrednjiKurs <= 0)
            return iznos;

        return Math.Round((iznos * stavka.SrednjiKurs) / stavka.Jedinica, 2);
    }
}

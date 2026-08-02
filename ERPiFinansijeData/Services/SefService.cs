using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class SefService
{
    private readonly AccountingDbContext _db;

    public SefService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Generiše UBL 2.1 XML i šalje e-Fakturu na SEF. Ažurira status u bazi.
    /// </summary>
    public async Task<(bool Success, string Message)> PosaljiNaSefAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice
            .Include(r => r.Stavke)
            .Include(r => r.Partner)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);

        if (racun == null)
            return (false, "Faktura/Račun nije pronađen u bazi.");

        var firma = await _db.Firme.FirstOrDefaultAsync();
        if (firma == null)
            return (false, "Podaci o vašoj firmi nisu pronađeni u bazi.");

        if (string.IsNullOrWhiteSpace(firma.SefApiKey))
            return (false, "SEF API Ključ nije podešen! Idite u Podešavanja -> SEF e-Fakture i unesite vaš API ključ.");

        if (racun.Partner == null)
            return (false, "Kupac/Partner na fakturi nije izabran.");

        // 1. Generisanje UBL XML
        string ublXml;
        try
        {
            ublXml = SefUblGenerator.GenerisiUblXml(racun, firma, racun.Partner);
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri generisanju UBL 2.1 XML-a: {ex.Message}");
        }

        // 2. Slanje preko SEF API-ja
        var client = new SefApiClient(firma.SefApiKey, firma.SefEnvironment);
        var res = await client.PosaljiFakturuUblAsync(ublXml);

        if (res.Success)
        {
            racun.SefId = res.Data > 0 ? res.Data : (long)racun.RacunOtpremnicaId;
            racun.SefStatus = SefStatusFakture.Poslata;
            racun.SefDatumSlanja = DateTime.Now;
            racun.SefPoruka = res.Message;

            await _db.SaveChangesAsync();
            return (true, $"Faktura #{racun.BrojRacuna} je uspešno poslata na SEF! (SEF ID: {racun.SefId})");
        }
        else
        {
            racun.SefStatus = SefStatusFakture.Greska;
            racun.SefPoruka = res.Message;
            await _db.SaveChangesAsync();

            return (false, $"Neuspešno slanje na SEF: {res.Message}");
        }
    }

    /// <summary>
    /// Osvežava SEF status poslate fakture iz SEF API-ja.
    /// </summary>
    public async Task<(bool Success, string Message, SefStatusFakture Status)> OsveziStatusNaSefuAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice.FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);
        if (racun == null)
            return (false, "Faktura nije pronađena.", SefStatusFakture.NijePoslata);

        if (!racun.SefId.HasValue || racun.SefId.Value == 0)
            return (false, "Faktura još uvek nema dodeljen SEF ID (nije poslata na SEF).", racun.SefStatus);

        var firma = await _db.Firme.FirstOrDefaultAsync();
        if (firma == null || string.IsNullOrWhiteSpace(firma.SefApiKey))
            return (false, "API Ključ nije podešen.", racun.SefStatus);

        var client = new SefApiClient(firma.SefApiKey, firma.SefEnvironment);
        var res = await client.ProveriStatusFaktureAsync(racun.SefId.Value);

        if (res.Success && res.Data != null)
        {
            string statusStr = (res.Data.Status ?? "").Trim().ToLowerInvariant();
            SefStatusFakture novStatus = racun.SefStatus;

            if (statusStr.Contains("approved") || statusStr.Contains("prihvacen") || statusStr.Contains("odobren"))
                novStatus = SefStatusFakture.Odobrena;
            else if (statusStr.Contains("rejected") || statusStr.Contains("odbijen"))
                novStatus = SefStatusFakture.Odbijena;
            else if (statusStr.Contains("cancelled") || statusStr.Contains("otkazan"))
                novStatus = SefStatusFakture.Otkazana;
            else if (statusStr.Contains("sent") || statusStr.Contains("poslat"))
                novStatus = SefStatusFakture.Poslata;

            racun.SefStatus = novStatus;
            racun.SefPoruka = $"Status sa SEF-a: {res.Data.Status} (Ažurirano: {DateTime.Now:HH:mm:ss})";
            await _db.SaveChangesAsync();

            return (true, racun.SefPoruka, novStatus);
        }
        else
        {
            return (false, res.Message ?? "Neuspešna provera statusa.", racun.SefStatus);
        }
    }

    /// <summary>
    /// Generiše i čuva UBL 2.1 XML fajl na zadatu putanju na disku.
    /// </summary>
    public async Task<(bool Success, string Message)> SacuvajUblXmlFajlAsync(int racunOtpremnicaId, string putanjaFajla)
    {
        var racun = await _db.RacuniOtpremnice
            .Include(r => r.Stavke)
            .Include(r => r.Partner)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);

        if (racun == null) return (false, "Faktura nije pronađena.");
        var firma = await _db.Firme.FirstOrDefaultAsync();
        if (firma == null) return (false, "Podaci o firmi nisu pronađeni.");

        var partner = racun.Partner ?? new Partner { Naziv = "Kupac", Pib = "123456789" };

        try
        {
            string xml = SefUblGenerator.GenerisiUblXml(racun, firma, partner);
            await File.WriteAllTextAsync(putanjaFajla, xml);
            return (true, $"UBL 2.1 XML fajl je sačuvan na putanji:\n{putanjaFajla}");
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri cuvanju XML fajla: {ex.Message}");
        }
    }

    /// <summary>
    /// Preuzima spisak ulaznih e-faktura od dobavljača.
    /// </summary>
    public async Task<SefApiResponse<List<SefUlaznaFakturaSummary>>> PreuzmiUlazneFaktureAsync(DateTime odDatuma)
    {
        var firma = await _db.Firme.FirstOrDefaultAsync();
        if (firma == null || string.IsNullOrWhiteSpace(firma.SefApiKey))
        {
            return new SefApiResponse<List<SefUlaznaFakturaSummary>>
            {
                Success = false,
                Message = "API Ključ za SEF nije podešen u postavkama firme."
            };
        }

        var client = new SefApiClient(firma.SefApiKey, firma.SefEnvironment);
        return await client.PreuzmiUlazneFaktureAsync(odDatuma);
    }
}

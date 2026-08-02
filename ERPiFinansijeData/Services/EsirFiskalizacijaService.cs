using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class EsirFiskalizacijaService
{
    private readonly AccountingDbContext _db;
    private readonly PfrApiClient _pfrClient;

    public EsirFiskalizacijaService(AccountingDbContext db, PfrApiClient? pfrClient = null)
    {
        _db = db;
        _pfrClient = pfrClient ?? new PfrApiClient();
    }

    /// <summary>
    /// Fiskalizuje selektovani račun/fakturu preko PFR servisa.
    /// </summary>
    public async Task<(bool Success, bool Simulacija, string Message, FiskalniRacunLog? Log)> FiskalizujRacunAsync(int racunId, string nacinPlacanja = "Cash")
    {
        var racun = await _db.RacuniOtpremnice
            .Include(r => r.Stavke)
                .ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunId);

        if (racun == null)
            return (false, false, "Račun nije pronađen u bazi.", null);

        if (racun.FiskalniStatus == FiskalniStatus.Fiskalizovan)
            return (false, false, $"Račun #{racun.BrojRacuna} je već fiskalizovan (Broj: {racun.FiskalniBroj}).", null);

        var firma = await _db.Firme.FirstOrDefaultAsync() ?? new Firma();

        var pfrPostavke = new PfrPostavke
        {
            PfrUrl = string.IsNullOrWhiteSpace(firma.PfrUrl) ? "http://localhost:8443" : firma.PfrUrl,
            PacKod = string.IsNullOrWhiteSpace(firma.PfrPacKod) ? "123456" : firma.PfrPacKod,
            Kasir = string.IsNullOrWhiteSpace(firma.PfrKasirName) ? "Glavni Kasir" : firma.PfrKasirName,
            SimulatorMod = firma.PfrSimulatorMod
        };

        // Priprema PFR zahteva
        var zahtev = new PfrZahtev
        {
            InvoiceType = "Normal",
            TransactionType = "Sale",
            Cashier = pfrPostavke.Kasir,
            Payment = new List<PfrZahtevPlacanje>
            {
                new PfrZahtevPlacanje
                {
                    Amount = racun.UkupnoZaUplatu,
                    PaymentType = nacinPlacanja
                }
            }
        };

        foreach (var s in racun.Stavke)
        {
            string labela = (s.StopaPdv == 10m) ? "E" : (s.StopaPdv == 0m ? "А" : "Đ");

            zahtev.Items.Add(new PfrZahtevStavka
            {
                Name = s.Artikal?.Naziv ?? s.NazivArtikla ?? "Artikal",
                Quantity = s.Kolicina,
                UnitPrice = s.ProdajnaCena,
                TotalAmount = s.Ukupno,
                Labels = new List<string> { labela }
            });
        }

        var (success, simulacija, message, odgovor) = await _pfrClient.FiskalizujRacunAsync(zahtev, pfrPostavke);

        if (success && odgovor != null)
        {
            var log = new FiskalniRacunLog
            {
                RacunOtpremnicaId = racun.RacunOtpremnicaId,
                InvoiceNumber = odgovor.InvoiceNumber,
                InvoiceCounter = odgovor.InvoiceCounter,
                SdcDateTime = odgovor.SdcDateTime,
                InvoiceType = "Normal",
                TransactionType = "Sale",
                TotalAmount = odgovor.TotalAmount,
                PaymentType = nacinPlacanja,
                QrCodeData = odgovor.VerificationUrl,
                VerificationUrl = odgovor.VerificationUrl,
                Kasir = pfrPostavke.Kasir,
                RawJsonResponse = odgovor.Journal
            };

            _db.FiskalniRacuniLog.Add(log);

            racun.FiskalniBroj = odgovor.InvoiceNumber;
            racun.FiskalniQrKod = odgovor.VerificationUrl;
            racun.FiskalniDatum = odgovor.SdcDateTime;
            // Simulirani račun se NIKADA ne označava kao fiskalizovan - to bi bila neistinita evidencija.
            racun.FiskalniStatus = simulacija ? FiskalniStatus.Simulacija : FiskalniStatus.Fiskalizovan;

            await _db.SaveChangesAsync();

            return (true, simulacija, message, log);
        }

        racun.FiskalniStatus = FiskalniStatus.Greska;
        await _db.SaveChangesAsync();

        return (false, false, message, null);
    }
}

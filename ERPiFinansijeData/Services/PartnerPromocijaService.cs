using Microsoft.EntityFrameworkCore;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeData.Services;

/// <summary>
/// Pretvara "sintetičkog" partnera (PartnerId=0, izveden direktno iz kontnog plana — vidi
/// OtvoreneStavkeService.GetPartneriAsync) u pravi red u tabeli Partneri, i tom prilikom
/// povezuje (backfill) sve dosadašnje stavke naloga tog konta koje još nemaju PartnerId na
/// novosazdanog partnera. Bez ovog drugog koraka bi zatvaranje otvorenih stavki i istorija
/// zatvaranja i dalje bili "nije podržano" za celu uvezenu istoriju — te funkcije filtriraju
/// po StavkaNaloga.PartnerId, ne po broju konta.
/// Menja se samo kolona PartnerId (veza/identitet), nikad iznosi ili datumi već proknjiženih
/// stavki — postojeće knjiženje ostaje netaknuto.
/// </summary>
public class PartnerPromocijaService
{
    private readonly AccountingDbContext _db;

    public PartnerPromocijaService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Snima podatke partnera. Ako <paramref name="brojKontaZaPromociju"/> nije null (partner je
    /// bio sintetički, PartnerId=0), kreira pravi red u Partneri (ili ga, ako već postoji red sa
    /// istom šifrom, samo ažurira — idempotentno, sprečava duplikate) i povezuje sve stavke naloga
    /// tog konta bez PartnerId-ja na njega. Ako je partner već pravi (PartnerId>0), samo ažurira
    /// postojeći red.
    /// </summary>
    public async Task<Partner> SacuvajPartneraAsync(int partnerId, string? brojKontaZaPromociju, Partner podaci)
    {
        Partner partner;

        if (partnerId > 0)
        {
            partner = await _db.Partneri.FirstOrDefaultAsync(p => p.PartnerId == partnerId)
                ?? throw new InvalidOperationException($"Partner sa ID {partnerId} ne postoji.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(brojKontaZaPromociju))
                throw new InvalidOperationException("Nedostaje konto za promociju sintetičkog partnera.");

            // Idempotentno: ako je partner već ranije promovisan (npr. dvostruki klik na Sačuvaj),
            // ne dupliramo red nego nastavljamo da ažuriramo isti.
            partner = await _db.Partneri.FirstOrDefaultAsync(p => p.SifraPartnera == brojKontaZaPromociju)
                ?? new Partner { SifraPartnera = brojKontaZaPromociju };

            if (partner.PartnerId == 0)
            {
                _db.Partneri.Add(partner);
            }
        }

        partner.Naziv = podaci.Naziv;
        partner.Adresa = podaci.Adresa;
        partner.PttIMesto = podaci.PttIMesto;
        partner.Pib = podaci.Pib;
        partner.MaticniBroj = podaci.MaticniBroj;
        partner.Telefon = podaci.Telefon;
        partner.ZiroRacun = podaci.ZiroRacun;
        partner.KontoPartnera = podaci.KontoPartnera;

        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(brojKontaZaPromociju))
        {
            await _db.StavkeNaloga
                .Where(s => s.PartnerId == null && s.BrojKonta == brojKontaZaPromociju)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PartnerId, partner.PartnerId));
        }

        return partner;
    }
}

using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class PdvService
{
    private readonly AccountingDbContext _db;

    public PdvService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Vraća stavke Knjige izdatih računa (KIR) iz proknjiženih računa-otpremnica za zadati period.
    /// </summary>
    public async Task<List<PdvZapis>> GetKirZapisiAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var query = _db.RacuniOtpremnice
            .Include(r => r.Partner)
            .Include(r => r.Stavke)
            .Where(r => r.IsKnjizen);

        if (odDatuma.HasValue) query = query.Where(r => r.DatumRacuna >= odDatuma.Value);
        if (doDatuma.HasValue) query = query.Where(r => r.DatumRacuna <= doDatuma.Value);

        var racuni = await query.OrderBy(r => r.DatumRacuna).ThenBy(r => r.BrojRacuna).ToListAsync();
        var rezultat = new List<PdvZapis>();
        int rbr = 1;

        foreach (var r in racuni)
        {
            decimal osn20 = 0m, pdv20 = 0m;
            decimal osn10 = 0m, pdv10 = 0m;
            decimal oslobodjen = 0m;

            foreach (var st in r.Stavke)
            {
                decimal pdvStopa = st.StopaPdv;
                if (pdvStopa >= 18m) // Opšta stopa 20% / 18%
                {
                    osn20 += st.Osnovica;
                    pdv20 += st.IznosPdv;
                }
                else if (pdvStopa > 0m) // Posebna stopa 10% / 8%
                {
                    osn10 += st.Osnovica;
                    pdv10 += st.IznosPdv;
                }
                else // Oslobođeno 0%
                {
                    oslobodjen += st.Osnovica;
                }
            }

            rezultat.Add(new PdvZapis
            {
                PdvZapisId = r.RacunOtpremnicaId,
                TipKnjige = TipPdvKnjige.KIR_IzdatRacun,
                RedniBroj = rbr++,
                DatumRacuna = r.DatumRacuna,
                DatumKnjizenja = r.DatumRacuna,
                BrojDokumenta = r.BrojRacuna.ToString(),
                PartnerNaziv = r.Partner?.Naziv ?? "Kupac na malo",
                PartnerPib = r.Partner?.Pib ?? "",
                UkupnaNaknadaSaPdv = r.UkupnoZaUplatu,
                Osnovica20 = osn20,
                Pdv20 = pdv20,
                Osnovica10 = osn10,
                Pdv10 = pdv10,
                OslobodjenPromet = oslobodjen,
                IzvornoDokumentId = r.RacunOtpremnicaId
            });
        }

        return rezultat;
    }

    /// <summary>
    /// Vraća stavke Knjige primljenih računa (KPR) iz proknjiženih kalkulacija i ulaza za zadati period.
    /// </summary>
    public async Task<List<PdvZapis>> GetKprZapisiAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var queryKalk = _db.Kalkulacije
            .Where(k => k.IsKnjizen);

        if (odDatuma.HasValue) queryKalk = queryKalk.Where(k => k.Datum >= odDatuma.Value);
        if (doDatuma.HasValue) queryKalk = queryKalk.Where(k => k.Datum <= doDatuma.Value);

        var kalkulacije = await queryKalk.OrderBy(k => k.Datum).ToListAsync();
        var rezultat = new List<PdvZapis>();
        int rbr = 1;

        foreach (var k in kalkulacije)
        {
            decimal osn20 = 0m, pdv20 = 0m;
            decimal osn10 = 0m, pdv10 = 0m;
            decimal oslobodjen = 0m;

            if (k.PoreskaStopaProcenat >= 18m)
            {
                osn20 = k.SvegaNabavno + k.Razlika;
                pdv20 = k.Porez;
            }
            else if (k.PoreskaStopaProcenat > 0m)
            {
                osn10 = k.SvegaNabavno + k.Razlika;
                pdv10 = k.Porez;
            }
            else
            {
                oslobodjen = k.SvegaNabavno;
            }

            rezultat.Add(new PdvZapis
            {
                PdvZapisId = k.KalkulacijaId,
                TipKnjige = TipPdvKnjige.KPR_PrimljenRacun,
                RedniBroj = rbr++,
                DatumRacuna = k.DatumRacuna ?? k.Datum,
                DatumKnjizenja = k.Datum,
                BrojDokumenta = k.BrojRacuna ?? k.BrojKalkulacije.ToString(),
                PartnerNaziv = k.SifraDobavljaca ?? "Dobavljač",
                PartnerPib = "",
                UkupnaNaknadaSaPdv = k.ProdajnaVrednost,
                Osnovica20 = osn20,
                Pdv20 = pdv20,
                Osnovica10 = osn10,
                Pdv10 = pdv10,
                OslobodjenPromet = oslobodjen,
                IzvornoDokumentId = k.KalkulacijaId
            });
        }

        return rezultat;
    }

    /// <summary>
    /// Računa zbirne podatke PDV obaveze (POPDV rekapitulaciju) za period.
    /// </summary>
    public async Task<PdvObracunResult> GetPdvObracunAsync(DateTime? odDatuma = null, DateTime? doDatuma = null)
    {
        var kir = await GetKirZapisiAsync(odDatuma, doDatuma);
        var kpr = await GetKprZapisiAsync(odDatuma, doDatuma);

        return new PdvObracunResult
        {
            OdDatuma = odDatuma ?? DateTime.Today.AddDays(-30),
            DoDatuma = doDatuma ?? DateTime.Today,
            
            KirUkupnoSaPdv = kir.Sum(x => x.UkupnaNaknadaSaPdv),
            KirOsnovica20 = kir.Sum(x => x.Osnovica20),
            KirPdv20 = kir.Sum(x => x.Pdv20),
            KirOsnovica10 = kir.Sum(x => x.Osnovica10),
            KirPdv10 = kir.Sum(x => x.Pdv10),
            KirOslobodjen = kir.Sum(x => x.OslobodjenPromet),

            KprUkupnoSaPdv = kpr.Sum(x => x.UkupnaNaknadaSaPdv),
            KprOsnovica20 = kpr.Sum(x => x.Osnovica20),
            KprPdv20 = kpr.Sum(x => x.Pdv20),
            KprOsnovica10 = kpr.Sum(x => x.Osnovica10),
            KprPdv10 = kpr.Sum(x => x.Pdv10),
            KprOslobodjen = kpr.Sum(x => x.OslobodjenPromet)
        };
    }
}

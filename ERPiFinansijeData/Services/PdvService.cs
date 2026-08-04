using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class PdvService
{
    private readonly AccountingDbContext _db;

    public PdvService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Vraća stavke Knjige izdatih računa (KIR) iz proknjiženih računa-otpremnica, kao i iz ručno
    /// unetih GK naloga koji nisu prošli kroz Trgovinu (prepoznaju se po liniji na kontu 4700 —
    /// izlazni PDV — sa popunjenom Osnovicom/Stopom PDV), za zadati period.
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

        // Ručno proknjižene fakture (Nalozi/StavkeNaloga) sa linijom na kontu 4700 koja nosi
        // Osnovicu/StopuPdv — isključujemo naloge koje je Trgovina već sama kreirala (NalogId
        // povezan sa RacunOtpremnica) da se ista faktura ne bi brojala dvaput.
        var vecObuhvaceniNalogIdKir = await _db.RacuniOtpremnice
            .Where(r => r.NalogId != null)
            .Select(r => r.NalogId!.Value)
            .ToListAsync();

        var rucneKirStavkeQuery = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Where(s => s.BrojKonta.StartsWith("4700") && s.Nalog != null && s.Nalog.IsKnjizen
                && s.Osnovica != null && s.StopaPdv != null
                && !vecObuhvaceniNalogIdKir.Contains(s.NalogId));

        if (odDatuma.HasValue) rucneKirStavkeQuery = rucneKirStavkeQuery.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);
        if (doDatuma.HasValue) rucneKirStavkeQuery = rucneKirStavkeQuery.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var rucneKirStavke = await rucneKirStavkeQuery.OrderBy(s => s.Nalog!.DatumNaloga).ToListAsync();

        foreach (var s in rucneKirStavke)
        {
            rezultat.Add(NapraviRucniPdvZapis(s, TipPdvKnjige.KIR_IzdatRacun, rbr++, s.Potrazuje));
        }

        return PreurediPoDatumu(rezultat);
    }

    /// <summary>Konačno sortiranje mešanog izvora (dokumenti + ručni unosi) po datumu, sa ponovnim brojanjem R.br.</summary>
    private static List<PdvZapis> PreurediPoDatumu(List<PdvZapis> zapisi)
    {
        var sortirano = zapisi.OrderBy(z => z.DatumRacuna).ThenBy(z => z.BrojDokumenta).ToList();
        for (int i = 0; i < sortirano.Count; i++) sortirano[i].RedniBroj = i + 1;
        return sortirano;
    }

    private static PdvZapis NapraviRucniPdvZapis(StavkaNaloga s, TipPdvKnjige tip, int rbr, decimal pdvIznos)
    {
        decimal osnovica = s.Osnovica ?? 0m;
        decimal stopa = s.StopaPdv ?? 0m;

        decimal osn20 = 0m, pdv20 = 0m, osn10 = 0m, pdv10 = 0m, oslobodjen = 0m;
        if (stopa >= 18m) { osn20 = osnovica; pdv20 = pdvIznos; }
        else if (stopa > 0m) { osn10 = osnovica; pdv10 = pdvIznos; }
        else { oslobodjen = osnovica; }

        return new PdvZapis
        {
            PdvZapisId = s.StavkaNalogaId,
            TipKnjige = tip,
            RedniBroj = rbr,
            DatumRacuna = s.DatumDokumenta ?? s.Nalog!.DatumNaloga,
            DatumKnjizenja = s.Nalog!.DatumNaloga,
            BrojDokumenta = s.BrojDokumenta ?? s.Nalog.BrojNaloga.ToString(),
            PartnerNaziv = s.Partner?.Naziv ?? "Ručni unos",
            PartnerPib = s.Partner?.Pib ?? "",
            UkupnaNaknadaSaPdv = osnovica + pdvIznos,
            Osnovica20 = osn20,
            Pdv20 = pdv20,
            Osnovica10 = osn10,
            Pdv10 = pdv10,
            OslobodjenPromet = oslobodjen,
            IzvornoDokumentId = s.NalogId
        };
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

        // Ručno proknjižene ulazne fakture sa linijom na kontu 2700 (ulazni PDV, Osnovica/StopaPdv
        // popunjeni), isključujući naloge koje je Kalkulacija sama kreirala (NalogId povezan).
        var vecObuhvaceniNalogIdKpr = await _db.Kalkulacije
            .Where(k => k.NalogId != null)
            .Select(k => k.NalogId!.Value)
            .ToListAsync();

        var rucneKprStavkeQuery = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Where(s => s.BrojKonta.StartsWith("2700") && s.Nalog != null && s.Nalog.IsKnjizen
                && s.Osnovica != null && s.StopaPdv != null
                && !vecObuhvaceniNalogIdKpr.Contains(s.NalogId));

        if (odDatuma.HasValue) rucneKprStavkeQuery = rucneKprStavkeQuery.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);
        if (doDatuma.HasValue) rucneKprStavkeQuery = rucneKprStavkeQuery.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var rucneKprStavke = await rucneKprStavkeQuery.OrderBy(s => s.Nalog!.DatumNaloga).ToListAsync();

        foreach (var s in rucneKprStavke)
        {
            rezultat.Add(NapraviRucniPdvZapis(s, TipPdvKnjige.KPR_PrimljenRacun, rbr++, s.Duguje));
        }

        return PreurediPoDatumu(rezultat);
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

    /// <summary>
    /// Generiše zvanični XML fajl Obrasca PP-PDV za ePorezi portal za dati period.
    /// </summary>
    public async Task<(bool Success, string Message, string XmlContent)> GenerisiPpPdvXmlAsync(DateTime? odDatuma, DateTime? doDatuma, bool zahtevZaPovracaj = false)
    {
        var firma = await _db.Firme.FirstOrDefaultAsync();
        if (firma == null)
            return (false, "Podaci o vašoj firmi nisu pronađeni u bazi.", "");

        var obracun = await GetPdvObracunAsync(odDatuma, doDatuma);
        try
        {
            string xml = PpPdvXmlGenerator.GenerisiPpPdvXml(obracun, firma, zahtevZaPovracaj);
            return (true, "Uspešno generisana PP-PDV prijava za ePorezi portal.", xml);
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri kreiranju PP-PDV XML-a: {ex.Message}", "");
        }
    }
}

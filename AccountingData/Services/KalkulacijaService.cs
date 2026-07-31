using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class KalkulacijaService
{
    private readonly AccountingDbContext _db;

    public KalkulacijaService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Čista formula obračuna veleprodajne kalkulacije — analogno legacy
    /// kalkknjizenje proceduri iz MAT2.PRG: nabavna vrednost + zavisni troškovi
    /// (transport, uskladištenje, utovar/istovar, osiguranje, ostalo) = svega
    /// nabavno; na to se dodaje trgovačka razlika (marža %) i porez (PDV %),
    /// dajući prodajnu vrednost. Bez zavisnosti od baze — testabilno u izolaciji.
    /// </summary>
    public static void Izracunaj(Kalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;
        k.SvegaNabavno = k.NabavnaVrednost + k.SvegaTroskovi;
        k.Razlika = Math.Round(k.SvegaNabavno * k.MarzaProcenat / 100m, 2);
        k.Porez = Math.Round((k.SvegaNabavno + k.Razlika) * k.PoreskaStopaProcenat / 100m, 2);
        k.ProdajnaVrednost = k.SvegaNabavno + k.Razlika + k.Porez;
    }

    /// <summary>
    /// Isto kao <see cref="Izracunaj"/>, ali za kalkulaciju sa stavkama (artikal/količina/nabavna
    /// cena po stavci) — analogno legacy veleprodajnoj kalkulaciji iz MAT6.PRG:867-892
    /// (<c>vizmenakalkulacija</c>). Zavisni troškovi (SvegaTroskovi) se raspoređuju srazmerno po
    /// učešću svake stavke u ukupnoj nabavnoj vrednosti (Iznos), sa ostatkom zaokruživanja
    /// dodatim na poslednju stavku (MAT6.PRG:888) da zbir Troskovi po stavkama tačno == SvegaTroskovi.
    /// Marža % i PDV % su, za razliku od legacy (koji dozvoljava override po liniji preko šifarnika
    /// tarifa po artiklu — kojeg ovaj sistem još nema), jedinstveni za ceo dokument (isti kao header).
    /// Header agregati (NabavnaVrednost/SvegaNabavno/Razlika/Porez/ProdajnaVrednost) se postavljaju
    /// kao zbir vrednosti po stavkama, ne header formulom (koja bi kod stavki bila redundantna).
    /// </summary>
    public static void IzracunajSaStavkama(Kalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;

        foreach (var s in k.Stavke)
        {
            s.Iznos = s.Kolicina * s.NabavnaCena;
        }
        decimal svegaIznos = k.Stavke.Sum(s => s.Iznos);

        decimal raspodeljenoTroskova = 0;
        for (int i = 0; i < k.Stavke.Count; i++)
        {
            var s = k.Stavke[i];
            bool poslednja = i == k.Stavke.Count - 1;

            s.Troskovi = poslednja
                ? k.SvegaTroskovi - raspodeljenoTroskova
                : (svegaIznos != 0 ? Math.Round(k.SvegaTroskovi * s.Iznos / svegaIznos, 2) : 0m);
            if (!poslednja) raspodeljenoTroskova += s.Troskovi;

            s.NabavnaVrednost = s.Iznos + s.Troskovi;
            s.RazlikaIznos = Math.Round(s.NabavnaVrednost * k.MarzaProcenat / 100m, 2);
            decimal prodajnaBezPoreza = s.NabavnaVrednost + s.RazlikaIznos;
            s.PorezIznos = Math.Round(prodajnaBezPoreza * k.PoreskaStopaProcenat / 100m, 2);
            s.ProdajnaVrednost = prodajnaBezPoreza + s.PorezIznos;
            s.ProdajnaCena = s.Kolicina != 0 ? s.ProdajnaVrednost / s.Kolicina : 0m;
        }

        k.NabavnaVrednost = svegaIznos;
        k.SvegaNabavno = k.Stavke.Sum(s => s.NabavnaVrednost);
        k.Razlika = k.Stavke.Sum(s => s.RazlikaIznos);
        k.Porez = k.Stavke.Sum(s => s.PorezIznos);
        k.ProdajnaVrednost = k.Stavke.Sum(s => s.ProdajnaVrednost);
    }

    public async Task<List<Kalkulacija>> GetKalkulacijeAsync(string? search = null)
    {
        var query = _db.Kalkulacije.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKalkulacije.ToString().Contains(search));
        }
        return await query.OrderByDescending(k => k.Datum).ToListAsync();
    }

    public async Task<Kalkulacija> SaveKalkulacijuAsync(Kalkulacija kalkulacija)
    {
        if (kalkulacija.Stavke.Count > 0)
        {
            IzracunajSaStavkama(kalkulacija);
        }
        else
        {
            Izracunaj(kalkulacija);
        }

        if (kalkulacija.KalkulacijaId == 0)
        {
            _db.Kalkulacije.Add(kalkulacija);
        }
        else
        {
            var existing = await _db.Kalkulacije
                .Include(k => k.Stavke)
                .FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacija.KalkulacijaId);

            if (existing == null)
            {
                throw new InvalidOperationException("Kalkulacija nije pronađena.");
            }
            if (existing.IsKnjizen)
            {
                throw new InvalidOperationException("Proknjižena kalkulacija se ne može menjati.");
            }

            existing.BrojKalkulacije = kalkulacija.BrojKalkulacije;
            existing.Datum = kalkulacija.Datum;
            existing.SifraDobavljaca = kalkulacija.SifraDobavljaca;
            existing.BrojRacuna = kalkulacija.BrojRacuna;
            existing.DatumRacuna = kalkulacija.DatumRacuna;
            existing.BrojOtpremnice = kalkulacija.BrojOtpremnice;
            existing.DatumOtpremnice = kalkulacija.DatumOtpremnice;
            existing.SifraMagacina = kalkulacija.SifraMagacina;
            existing.NabavnaVrednost = kalkulacija.NabavnaVrednost;
            existing.TransportniTroskovi = kalkulacija.TransportniTroskovi;
            existing.TroskoviUskladistenja = kalkulacija.TroskoviUskladistenja;
            existing.UtovarIstovar = kalkulacija.UtovarIstovar;
            existing.TransportnoOsiguranje = kalkulacija.TransportnoOsiguranje;
            existing.OstaliTroskovi = kalkulacija.OstaliTroskovi;
            existing.SvegaTroskovi = kalkulacija.SvegaTroskovi;
            existing.SvegaNabavno = kalkulacija.SvegaNabavno;
            existing.Razlika = kalkulacija.Razlika;
            existing.MarzaProcenat = kalkulacija.MarzaProcenat;
            existing.Porez = kalkulacija.Porez;
            existing.PoreskaStopaProcenat = kalkulacija.PoreskaStopaProcenat;
            existing.ProdajnaVrednost = kalkulacija.ProdajnaVrednost;

            _db.KalkulacijaStavke.RemoveRange(existing.Stavke);
            existing.Stavke = kalkulacija.Stavke;

            kalkulacija = existing;
        }

        await _db.SaveChangesAsync();
        return kalkulacija;
    }

    /// <summary>
    /// Knjiži kalkulaciju. Ako ima stavki, za svaku dodaje red u robnu (materijalnu) karticu
    /// preko <see cref="MaterijalnaKarticaService"/> — po PRODAJNOJ ceni po jedinici mere
    /// (ne nabavnoj), tačno kao legacy <c>dodaj_mat_kar(..., kal_nal->prod_po_jm, ...)</c>
    /// (MAT1.PRG:1016-1018). Kalkulacije bez stavki (stariji, header-only unos) i dalje samo
    /// menjaju IsKnjizen, bez dodira karticu.
    /// </summary>
    public async Task KnjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);
        if (kalkulacija == null)
        {
            throw new InvalidOperationException("Kalkulacija nije pronađena.");
        }
        if (kalkulacija.IsKnjizen)
        {
            throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} je već proknjižena.");
        }

        if (kalkulacija.Stavke.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(kalkulacija.SifraMagacina))
            {
                throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} ima stavke — izaberite magacin pre knjiženja.");
            }

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in kalkulacija.Stavke)
            {
                await kartice.DodajUlazRedAsync(kalkulacija.SifraMagacina, s.SifraArtikla, kalkulacija.Datum,
                    $"Kalkulacija {kalkulacija.BrojKalkulacije}", s.Kolicina, s.ProdajnaCena);
            }
        }

        kalkulacija.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }
}

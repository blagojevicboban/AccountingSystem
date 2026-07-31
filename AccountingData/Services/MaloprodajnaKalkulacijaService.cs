using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class MaloprodajnaKalkulacijaService
{
    private readonly AccountingDbContext _db;

    public MaloprodajnaKalkulacijaService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Obračun maloprodajne (MP) kalkulacije bez stavki (header-only, za stare/legacy-uvezene
    /// zapise bez artikala) — agregat legacy stavke-po-stavke procedure <c>izmenakalkulacija()</c>
    /// iz MAT3.PRG:943-997 kao da je ceo dokument jedna stavka: nabavna vrednost + zavisni
    /// troškovi = svega nabavno (MAT3.PRG:968); + marža% = razlika (MAT3.PRG:969); + PDV% na
    /// (svega nabavno + razlika) = porez (MAT3.PRG:972); zbir = prodajna vrednost (MAT3.PRG:976,
    /// bez posebne takse koja ovde nije modelovana). RabatIznos se računa nezavisno od nabavne
    /// vrednosti (MAT3.PRG:500: rabat_iz = svega_iznos * rabat/100) i NE oduzima se od prodajne
    /// vrednosti — u legacy sistemu je to čisto informativni podatak (rabat dobavljača).
    /// </summary>
    public static void Izracunaj(MaloprodajnaKalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;
        k.SvegaNabavno = k.NabavnaVrednost + k.SvegaTroskovi;
        k.Razlika = Math.Round(k.SvegaNabavno * k.MarzaProcenat / 100m, 2);
        decimal prodajnaBezPoreza = k.SvegaNabavno + k.Razlika;
        k.Porez = Math.Round(prodajnaBezPoreza * k.PoreskaStopaProcenat / 100m, 2);
        k.ProdajnaVrednost = prodajnaBezPoreza + k.Porez;
        k.RabatIznos = Math.Round(k.NabavnaVrednost * k.RabatPri / 100m, 2);
    }

    /// <summary>
    /// Isto kao <see cref="Izracunaj"/>, ali za kalkulaciju sa stavkama (artikal/količina/nabavna
    /// cena po stavci) — analogno legacy maloprodajnoj kalkulaciji iz MAT3.PRG:943-985
    /// (<c>izmenakalkulacija</c>). Zavisni troškovi (SvegaTroskovi) se raspoređuju srazmerno po
    /// učešću svake stavke u ukupnoj nabavnoj vrednosti (Iznos), sa ostatkom zaokruživanja
    /// dodatim na poslednju stavku (MAT3.PRG:965/991) da zbir Troskovi po stavkama tačno ==
    /// SvegaTroskovi. Marža % i PDV % su, isto kao i kod veleprodajne kalkulacije
    /// (<see cref="KalkulacijaService.IzracunajSaStavkama"/>), jedinstveni za ceo dokument (isti
    /// kao header) — legacy dozvoljava override po liniji preko šifarnika tarifa po artiklu, koji
    /// ovaj sistem još nema. Header agregati se postavljaju kao zbir vrednosti po stavkama.
    /// </summary>
    public static void IzracunajSaStavkama(MaloprodajnaKalkulacija k)
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
        k.RabatIznos = Math.Round(k.NabavnaVrednost * k.RabatPri / 100m, 2);
    }

    public async Task<List<MaloprodajnaKalkulacija>> GetKalkulacijeAsync(string? search = null)
    {
        var query = _db.MaloprodajneKalkulacije.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKalkulacije.ToString().Contains(search));
        }
        return await query.OrderByDescending(k => k.Datum).ToListAsync();
    }

    public async Task<MaloprodajnaKalkulacija> SaveKalkulacijuAsync(MaloprodajnaKalkulacija kalkulacija)
    {
        if (kalkulacija.Stavke.Count > 0)
        {
            IzracunajSaStavkama(kalkulacija);
        }
        else
        {
            Izracunaj(kalkulacija);
        }

        if (kalkulacija.MaloprodajnaKalkulacijaId == 0)
        {
            _db.MaloprodajneKalkulacije.Add(kalkulacija);
        }
        else
        {
            var existing = await _db.MaloprodajneKalkulacije
                .Include(k => k.Stavke)
                .FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalkulacija.MaloprodajnaKalkulacijaId);

            if (existing == null)
            {
                throw new InvalidOperationException("Kalkulacija nije pronađena.");
            }
            if (existing.IsKnjizen)
            {
                throw new InvalidOperationException("Proknjižena kalkulacija se ne može menjati.");
            }

            existing.SifraProdavnice = kalkulacija.SifraProdavnice;
            existing.BrojKalkulacije = kalkulacija.BrojKalkulacije;
            existing.Datum = kalkulacija.Datum;
            existing.SifraMagacinaPrima = kalkulacija.SifraMagacinaPrima;
            existing.SifraMagacinaDaje = kalkulacija.SifraMagacinaDaje;
            existing.SifraDobavljaca = kalkulacija.SifraDobavljaca;
            existing.BrojOtpremnice = kalkulacija.BrojOtpremnice;
            existing.DatumOtpremnice = kalkulacija.DatumOtpremnice;
            existing.BrojRacuna = kalkulacija.BrojRacuna;
            existing.DatumRacuna = kalkulacija.DatumRacuna;
            existing.TransportniTroskovi = kalkulacija.TransportniTroskovi;
            existing.TroskoviUskladistenja = kalkulacija.TroskoviUskladistenja;
            existing.UtovarIstovar = kalkulacija.UtovarIstovar;
            existing.TransportnoOsiguranje = kalkulacija.TransportnoOsiguranje;
            existing.OstaliTroskovi = kalkulacija.OstaliTroskovi;
            existing.SvegaTroskovi = kalkulacija.SvegaTroskovi;
            existing.RabatPri = kalkulacija.RabatPri;
            existing.NabavnaVrednost = kalkulacija.NabavnaVrednost;
            existing.SvegaNabavno = kalkulacija.SvegaNabavno;
            existing.Razlika = kalkulacija.Razlika;
            existing.MarzaProcenat = kalkulacija.MarzaProcenat;
            existing.Porez = kalkulacija.Porez;
            existing.PoreskaStopaProcenat = kalkulacija.PoreskaStopaProcenat;
            existing.ProdajnaVrednost = kalkulacija.ProdajnaVrednost;
            existing.RabatIznos = kalkulacija.RabatIznos;

            _db.MaloprodajnaKalkulacijaStavke.RemoveRange(existing.Stavke);
            existing.Stavke = kalkulacija.Stavke;

            kalkulacija = existing;
        }

        await _db.SaveChangesAsync();
        return kalkulacija;
    }

    /// <summary>
    /// Knjiži MP kalkulaciju. Ako ima stavki, za svaku generiše IZLAZ (razduženje) iz magacina
    /// koji daje robu (SifraMagacinaDaje) — analogno legacy <c>knjiz_malkul()</c> (MAT3.PRG:1044+),
    /// koji za svaku stavku upisuje red u "razduz" (razduženje) iz mag_daje. Za razliku od legacy
    /// (koji upisuje po unetoj nabavnoj ceni stavke), izlaz se knjiži po TRENUTNOJ prosečnoj ceni
    /// magacina (<see cref="MaterijalnaKarticaService.DodajIzlazRedAsync"/>) — isti princip po kom
    /// su već proknjiženi svi ostali izlazi (Zaduženje/Razduženje/Trebovanje) u ovom sistemu, radi
    /// dosledne prosečne cene na kartici. Kalkulacije bez stavki (header-only legacy uvoz) samo
    /// menjaju IsKnjizen, bez dodira karticu.
    /// </summary>
    public async Task KnjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.MaloprodajneKalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalkulacijaId);
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
            if (string.IsNullOrWhiteSpace(kalkulacija.SifraMagacinaDaje))
            {
                throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} ima stavke — izaberite magacin (daje) pre knjiženja.");
            }

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in kalkulacija.Stavke)
            {
                await kartice.DodajIzlazRedAsync(kalkulacija.SifraMagacinaDaje, s.SifraArtikla, kalkulacija.Datum,
                    $"MP kalkulacija {kalkulacija.BrojKalkulacije}", s.Kolicina);
            }
        }

        kalkulacija.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Rasknjiži MP kalkulaciju — uklanja redove materijalne kartice koje je ova kalkulacija
    /// upisala (obrnutim redosledom od knjiženja) i vraća je u status nacrta radi izmene. Baca
    /// grešku ako je za neki artikal u međuvremenu knjiženo nešto kasnije.
    /// </summary>
    public async Task RasknjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.MaloprodajneKalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.MaloprodajnaKalkulacijaId == kalkulacijaId);
        if (kalkulacija == null)
        {
            throw new InvalidOperationException("Kalkulacija nije pronađena.");
        }
        if (!kalkulacija.IsKnjizen)
        {
            throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} nije proknjižena.");
        }

        if (kalkulacija.Stavke.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(kalkulacija.SifraMagacinaDaje))
            {
                throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} nema magacin (daje) — nije moguće rasknjižiti.");
            }

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in kalkulacija.Stavke.AsEnumerable().Reverse())
            {
                await kartice.UkloniPoslednjiRedAsync(kalkulacija.SifraMagacinaDaje, s.SifraArtikla, $"MP kalkulacija {kalkulacija.BrojKalkulacije}");
            }
        }

        kalkulacija.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}

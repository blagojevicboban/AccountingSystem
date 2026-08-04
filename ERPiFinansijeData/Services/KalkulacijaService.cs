using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

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

        await KnjiziUGlavnuKnjiguAsync(kalkulacija);

        kalkulacija.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Nalog za glavnu knjigu, po obrascu zatečenom u knjiženjima ovih firmi
    /// (vidi <see cref="RobnaKonta"/>, opis stavke „KALKUL.VELEPRODAJE"):
    /// <code>
    ///   1320   duguje     svega nabavno + razlika   (prodajna vrednost bez PDV)
    ///   1329   potražuje  razlika u ceni
    ///   43xxx  potražuje  svega nabavno             (obaveza prema dobavljaču)
    /// </code>
    /// Veleprodaja nema ukalkulisani PDV — roba se vodi po prodajnoj vrednosti BEZ poreza,
    /// pa <see cref="Kalkulacija.Porez"/> (koji ulazi u ProdajnaVrednost) ovde namerno ne učestvuje.
    ///
    /// Preskače se bez konta dobavljača — bez protivstavke nalog ne bi bio u ravnoteži, a
    /// kalkulacija sa starijeg uvoza ume da nema popunjenog dobavljača.
    /// </summary>
    private async Task KnjiziUGlavnuKnjiguAsync(Kalkulacija kalkulacija)
    {
        decimal svegaNabavno = kalkulacija.SvegaNabavno;
        decimal razlika = kalkulacija.Razlika;
        decimal prodajnaBezPoreza = svegaNabavno + razlika;

        if (prodajnaBezPoreza == 0) return;
        if (string.IsNullOrWhiteSpace(kalkulacija.SifraDobavljaca)) return;

        string opis = $"Kalkulacija veleprodaje {kalkulacija.BrojKalkulacije}";
        int sledeciBroj = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBroj,
            DatumNaloga = kalkulacija.Datum,
            Opis = opis,
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now,
            VrstaNaloga = "KALKULACIJA"
        };

        int rb = 1;
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = rb++,
            BrojKonta = RobnaKonta.RobaVeleprodaja,
            Opis = opis,
            BrojDokumenta = kalkulacija.BrojRacuna,
            Duguje = prodajnaBezPoreza,
            Potrazuje = 0m
        });

        if (razlika != 0)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = rb++,
                BrojKonta = RobnaKonta.RazlikaUCeniVeleprodaja,
                Opis = opis,
                Duguje = 0m,
                Potrazuje = razlika
            });
        }

        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = rb,
            BrojKonta = kalkulacija.SifraDobavljaca,
            Opis = opis,
            BrojDokumenta = kalkulacija.BrojRacuna,
            Duguje = 0m,
            Potrazuje = svegaNabavno
        });

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        kalkulacija.NalogId = nalog.NalogId;
    }

    /// <summary>
    /// Prebacuje veleprodajnu kalkulaciju u maloprodajnu, sa svim stavkama. Potrebno je zato što
    /// legacy sistem ima jedan te isti fajl (KALKULAC.DBF) i za nabavku u stovarište i za nabavku
    /// pravo u prodavnicu — vrsta se vidi tek po magacinu, pa uvoz sve svrsta u veleprodajne.
    ///
    /// Prenosi se sam dokument; redovi robne kartice se ne diraju jer opisuju isti stvarni događaj
    /// (roba je ušla u isti magacin). Odbija kalkulaciju koja je proknjižena kroz glavnu knjigu —
    /// takvu treba prvo rasknjižiti, da nalog na veleprodajnim kontima ne bi ostao iza nje.
    /// </summary>
    public async Task<MaloprodajnaKalkulacija> PrebaciUMaloprodajuAsync(int kalkulacijaId)
    {
        var vp = await _db.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);
        if (vp == null)
        {
            throw new InvalidOperationException("Kalkulacija nije pronađena.");
        }
        if (vp.NalogId != null)
        {
            throw new InvalidOperationException(
                $"Kalkulacija {vp.BrojKalkulacije} je proknjižena u glavnu knjigu — rasknjižite je pre prebacivanja u maloprodaju.");
        }

        var mp = new MaloprodajnaKalkulacija
        {
            SifraProdavnice = 0,
            BrojKalkulacije = vp.BrojKalkulacije,
            Datum = vp.Datum,
            // KALKULAC.DBF ima samo MAG_PRIMA — magacin koji prima robu je prodavnica.
            SifraMagacinaPrima = vp.SifraMagacina ?? await MagacinIzRobneKarticeAsync(vp.BrojKalkulacije),
            SifraMagacinaDaje = null,
            SifraDobavljaca = vp.SifraDobavljaca,
            BrojOtpremnice = vp.BrojOtpremnice,
            DatumOtpremnice = vp.DatumOtpremnice,
            BrojRacuna = vp.BrojRacuna,
            DatumRacuna = vp.DatumRacuna,
            TransportniTroskovi = vp.TransportniTroskovi,
            TroskoviUskladistenja = vp.TroskoviUskladistenja,
            UtovarIstovar = vp.UtovarIstovar,
            TransportnoOsiguranje = vp.TransportnoOsiguranje,
            OstaliTroskovi = vp.OstaliTroskovi,
            SvegaTroskovi = vp.SvegaTroskovi,
            NabavnaVrednost = vp.NabavnaVrednost,
            SvegaNabavno = vp.SvegaNabavno,
            Razlika = vp.Razlika,
            MarzaProcenat = vp.MarzaProcenat,
            Porez = vp.Porez,
            PoreskaStopaProcenat = vp.PoreskaStopaProcenat,
            ProdajnaVrednost = vp.ProdajnaVrednost,
            IsKnjizen = vp.IsKnjizen,
            Stavke = vp.Stavke.OrderBy(s => s.RedniBroj).Select(s => new MaloprodajnaKalkulacijaStavka
            {
                RedniBroj = s.RedniBroj,
                SifraArtikla = s.SifraArtikla,
                Kolicina = s.Kolicina,
                NabavnaCena = s.NabavnaCena,
                Iznos = s.Iznos,
                Troskovi = s.Troskovi,
                NabavnaVrednost = s.NabavnaVrednost,
                RazlikaProcenat = s.RazlikaProcenat,
                RazlikaIznos = s.RazlikaIznos,
                ProdajnaVrednostBezPoreza = s.ProdajnaVrednostBezPoreza,
                PorezProcenat = s.PorezProcenat,
                PorezIznos = s.PorezIznos,
                PosebanPorezProcenat = s.PosebanPorezProcenat,
                PosebanPorezIznos = s.PosebanPorezIznos,
                PrenetiPorez = s.PrenetiPorez,
                PrenetiPosebanPorez = s.PrenetiPosebanPorez,
                PorezZaUplatu = s.PorezZaUplatu,
                ProdajnaVrednost = s.ProdajnaVrednost,
                ProdajnaCena = s.ProdajnaCena,
                IsKnjizen = s.IsKnjizen
            }).ToList()
        };

        _db.MaloprodajneKalkulacije.Add(mp);
        _db.KalkulacijaStavke.RemoveRange(vp.Stavke);
        _db.Kalkulacije.Remove(vp);
        await _db.SaveChangesAsync();

        return mp;
    }

    /// <summary>
    /// Magacin u koji je kalkulacija stvarno knjižena, očitan iz robne kartice. Treba za
    /// kalkulacije uvezene starijom verzijom, kojima je zaglavlje ostalo bez magacina
    /// (MAG_PRIMA tada nije mapiran) iako redovi kartice nose pravi magacin.
    /// Opis reda je pisan u dva oblika kroz verzije — „Kalkulacija 7" i „Kalkulacija7".
    /// </summary>
    private async Task<string?> MagacinIzRobneKarticeAsync(int brojKalkulacije)
    {
        string saRazmakom = $"Kalkulacija {brojKalkulacije}";
        string bezRazmaka = $"Kalkulacija{brojKalkulacije}";

        return await _db.MaterijalneKartice
            .Where(m => m.OpisPromene == saRazmakom || m.OpisPromene == bezRazmaka)
            .Select(m => m.SifraMagacina)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Prebacuje u maloprodaju sve veleprodajne kalkulacije. Koristi se kad firma ceo robni
    /// promet vodi kroz prodavnicu, pa je podela na veleprodajne/maloprodajne nastala samo zato
    /// što legacy sistem obe vrste drži u istom fajlu (KALKULAC.DBF).
    /// Vraća broj prebačenih i spisak onih koje su preskočene, sa razlogom.
    /// </summary>
    public async Task<(int Prebaceno, List<string> Preskoceno)> PrebaciSveUMaloprodajuAsync()
        => await PrebaciAsync(await _db.Kalkulacije.Select(k => k.KalkulacijaId).ToListAsync());

    /// <summary>
    /// Prebacuje u maloprodaju samo one veleprodajne kalkulacije čiji je magacin maloprodajni —
    /// za firme koje zaista vode i jedno i drugo, pa se sme razdvojiti po magacinu.
    /// </summary>
    public async Task<(int Prebaceno, List<string> Preskoceno)> PrebaciMaloprodajneAsync()
    {
        var maloprodajniMagacini = await _db.Magacini
            .Where(m => m.VrstaMagacina == "Maloprodaja")
            .Select(m => m.SifraMagacina)
            .ToListAsync();

        var kandidati = await _db.Kalkulacije
            .Where(k => k.SifraMagacina != null && maloprodajniMagacini.Contains(k.SifraMagacina))
            .Select(k => k.KalkulacijaId)
            .ToListAsync();

        return await PrebaciAsync(kandidati);
    }

    private async Task<(int Prebaceno, List<string> Preskoceno)> PrebaciAsync(List<int> kandidati)
    {
        int prebaceno = 0;
        var preskoceno = new List<string>();
        foreach (var id in kandidati)
        {
            try
            {
                await PrebaciUMaloprodajuAsync(id);
                prebaceno++;
            }
            catch (InvalidOperationException ex)
            {
                preskoceno.Add(ex.Message);
            }
        }

        return (prebaceno, preskoceno);
    }

    /// <summary>Uklanja nalog kojim je kalkulacija bila proknjižena, ako i dalje postoji.</summary>
    private async Task UkloniNalogAsync(int? nalogId)
    {
        if (nalogId == null) return;

        var nalog = await _db.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NalogId == nalogId);
        if (nalog == null) return;

        _db.StavkeNaloga.RemoveRange(nalog.Stavke);
        _db.Nalozi.Remove(nalog);
    }

    /// <summary>
    /// Rasknjiži kalkulaciju — uklanja redove materijalne kartice koje je ova kalkulacija
    /// upisala (obrnutim redosledom od knjiženja) i vraća je u status nacrta radi izmene.
    /// Baca grešku ako je za neki artikal u međuvremenu knjiženo nešto kasnije.
    /// </summary>
    public async Task RasknjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.Kalkulacije.Include(k => k.Stavke).FirstOrDefaultAsync(k => k.KalkulacijaId == kalkulacijaId);
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
            if (string.IsNullOrWhiteSpace(kalkulacija.SifraMagacina))
            {
                throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} nema magacin — nije moguće rasknjižiti.");
            }

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in kalkulacija.Stavke.AsEnumerable().Reverse())
            {
                await kartice.UkloniPoslednjiRedAsync(kalkulacija.SifraMagacina, s.SifraArtikla, $"Kalkulacija {kalkulacija.BrojKalkulacije}");
            }
        }

        await UkloniNalogAsync(kalkulacija.NalogId);
        kalkulacija.NalogId = null;

        kalkulacija.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}

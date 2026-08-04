using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class RacunOtpremnicaService
{
    private readonly AccountingDbContext _db;

    public RacunOtpremnicaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<RacunOtpremnica>> GetRacuneAsync(int? magacinId = null)
    {
        var query = _db.RacuniOtpremnice
            .Include(r => r.Partner)
            .Include(r => r.Magacin)
            .Include(r => r.Stavke)
                .ThenInclude(s => s.Artikal)
            .AsQueryable();

        if (magacinId.HasValue && magacinId.Value > 0)
        {
            query = query.Where(r => r.MagacinId == magacinId.Value);
        }

        return await query.OrderByDescending(r => r.DatumRacuna).ThenByDescending(r => r.RacunOtpremnicaId).ToListAsync();
    }

    public async Task<RacunOtpremnica?> GetRacunByIdAsync(int id)
    {
        return await _db.RacuniOtpremnice
            .Include(r => r.Partner)
            .Include(r => r.Magacin)
            .Include(r => r.Stavke)
                .ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == id);
    }

    public async Task SaveRacunAsync(RacunOtpremnica racun)
    {
        // Preračunaj zbirove
        decimal osn = 0m, rab = 0m, pdv = 0m, tot = 0m;
        int rb = 1;
        foreach (var s in racun.Stavke)
        {
            s.RedniBroj = rb++;
            decimal brutovrednost = s.Kolicina * s.ProdajnaCena;
            decimal iznosRabata = brutovrednost * (s.RabatProcenat / 100m);
            s.Osnovica = brutovrednost - iznosRabata;
            s.IznosPdv = s.Osnovica * (s.StopaPdv / 100m);
            s.Ukupno = s.Osnovica + s.IznosPdv;

            osn += s.Osnovica;
            rab += iznosRabata;
            pdv += s.IznosPdv;
            tot += s.Ukupno;
        }

        racun.UkupnoOsnovica = osn;
        racun.UkupnoRabat = rab;
        racun.UkupnoPdv = pdv;
        racun.UkupnoZaUplatu = tot;

        if (racun.RacunOtpremnicaId == 0)
        {
            _db.RacuniOtpremnice.Add(racun);
        }
        else
        {
            var existing = await _db.RacuniOtpremnice
                .Include(r => r.Stavke)
                .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racun.RacunOtpremnicaId);

            if (existing != null)
            {
                if (existing.IsKnjizen) throw new InvalidOperationException("Proknjiženi račun-otpremnica se ne može menjati.");

                existing.TipDokumenta = racun.TipDokumenta;
                existing.RokVazenjaPredracuna = racun.RokVazenjaPredracuna;
                existing.BrojRacuna = racun.BrojRacuna;
                existing.DatumRacuna = racun.DatumRacuna;
                existing.RokPlacanja = racun.RokPlacanja;
                existing.PartnerId = racun.PartnerId;
                existing.MagacinId = racun.MagacinId;
                existing.Napomena = racun.Napomena;
                existing.UkupnoOsnovica = racun.UkupnoOsnovica;
                existing.UkupnoRabat = racun.UkupnoRabat;
                existing.UkupnoPdv = racun.UkupnoPdv;
                existing.UkupnoZaUplatu = racun.UkupnoZaUplatu;

                _db.RacunOtpremnicaStavke.RemoveRange(existing.Stavke);
                existing.Stavke = racun.Stavke;
            }
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>Pretvara predračun u račun-otpremnicu: menja tip dokumenta i postavlja tekući datum kao datum računa, zadržavajući iste stavke.</summary>
    public async Task PretvoriUFakturuAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice.FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);
        if (racun == null) throw new InvalidOperationException("Predračun nije pronađen.");
        if (racun.TipDokumenta != TipRacunOtpremnice.Predracun) throw new InvalidOperationException("Dokument nije predračun.");

        racun.TipDokumenta = TipRacunOtpremnice.Racun;
        racun.DatumRacuna = DateTime.Now;
        racun.RokVazenjaPredracuna = null;
        await _db.SaveChangesAsync();
    }

    public async Task KnjiziRacunAsync(int racunOtpremnicaId)
    {
        var racun = await GetRacunByIdAsync(racunOtpremnicaId);
        if (racun == null) throw new InvalidOperationException("Račun nije pronađen.");
        if (racun.TipDokumenta == TipRacunOtpremnice.Predracun) throw new InvalidOperationException("Predračun se ne može knjižiti — prvo ga pretvorite u račun.");
        if (racun.IsKnjizen) throw new InvalidOperationException("Račun je već proknjižen.");

        // Razduženje robne kartice — po prosečnoj (nabavnoj) ceni, ne po prodajnoj sa
        // fakture, jer se zaliha vrednuje po ponderisanoj nabavnoj ceni (isto načelo kao
        // Trebovanje/Primopredaja preko MaterijalnaKarticaService). Magacin je obavezan
        // čim račun ima stavke — bez njega ne znamo koju karticu razdužiti.
        decimal nabavnaVrednostProdate = 0m;
        if (racun.Stavke.Count > 0)
        {
            if (racun.Magacin == null)
            {
                throw new InvalidOperationException($"Račun {racun.BrojRacuna} nema izabran magacin — izaberite magacin pre knjiženja.");
            }

            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in racun.Stavke)
            {
                string sifraArtikla = s.Artikal?.SifraArtikla ?? s.SifraArtikla;
                nabavnaVrednostProdate += await kartice.DodajIzlazRedAsync(
                    racun.Magacin.SifraMagacina, sifraArtikla, racun.DatumRacuna,
                    $"Račun {racun.BrojRacuna}", s.Kolicina);
            }
        }

        // Automatsko kreiranje naloga knjiženja u Glavnoj knjizi
        int sledeciBrojNaloga = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;
        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            DatumNaloga = racun.DatumRacuna,
            VrstaNaloga = "Prodaja",
            Opis = $"Račun-otpremnica br. {racun.BrojRacuna}",
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now
        };

        // 1. Duguje Kupac — analitika izabrana na dokumentu (konto iz kontnog plana, grupa 204/120).
        // Sintetika 2040 ostaje samo za račune unete pre nego što se kupac birao iz kontnog plana.
        string kontoKupca = string.IsNullOrWhiteSpace(racun.KontoKupca) ? "2040" : racun.KontoKupca.Trim();

        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 1,
            BrojKonta = kontoKupca,
            BrojDokumenta = racun.BrojRacuna.ToString(),
            Opis = $"Faktura br. {racun.BrojRacuna}",
            Duguje = racun.UkupnoZaUplatu,
            Potrazuje = 0m,
            PartnerId = racun.PartnerId
        });

        // 2. Potražuje Prihod od prodaje robe (Konto 6120)
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 2,
            BrojKonta = "6120",
            BrojDokumenta = racun.BrojRacuna.ToString(),
            Opis = $"Prihod po fakturi {racun.BrojRacuna}",
            Duguje = 0m,
            Potrazuje = racun.UkupnoOsnovica,
            PartnerId = racun.PartnerId
        });

        // 3. Potražuje Obračunati PDV (Konto 4700)
        if (racun.UkupnoPdv > 0)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 3,
                BrojKonta = "4700",
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Obračunati PDV po fakturi {racun.BrojRacuna}",
                Duguje = 0m,
                Potrazuje = racun.UkupnoPdv,
                PartnerId = racun.PartnerId
            });
        }

        // 4/5. Razduženje robe: nabavna vrednost prodate robe (5010) duguje / roba na
        // zalihama (1320 ili 1340, prema vrsti magacina) potražuje — istovremeno sa
        // prihodom, tako da nalog ostane u ravnoteži i van dvostepenog obrasca po redu.
        if (nabavnaVrednostProdate != 0)
        {
            string kontoRobe = RobnaKonta.RobaZaVrstuMagacina(racun.Magacin!.VrstaMagacina);

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 4,
                BrojKonta = "5010",
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Nabavna vrednost prodate robe po fakturi {racun.BrojRacuna}",
                Duguje = nabavnaVrednostProdate,
                Potrazuje = 0m
            });

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = 5,
                BrojKonta = kontoRobe,
                BrojDokumenta = racun.BrojRacuna.ToString(),
                Opis = $"Razduženje robe po fakturi {racun.BrojRacuna}",
                Duguje = 0m,
                Potrazuje = nabavnaVrednostProdate
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        racun.IsKnjizen = true;
        racun.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Rasknjiži račun-otpremnicu — uklanja redove materijalne kartice koje je ovaj
    /// račun upisao pri razduženju (obrnutim redosledom od knjiženja) i briše nalog
    /// knjiženja koji je automatski kreiran u Glavnoj knjizi, pa vraća račun u status
    /// nacrta radi izmene. Baca grešku ako je za neki artikal u međuvremenu knjiženo
    /// nešto kasnije (isti obrazac kao Kalkulacija/Trebovanje/Primopredaja).
    /// </summary>
    public async Task RasknjiziRacunAsync(int racunOtpremnicaId)
    {
        var racun = await _db.RacuniOtpremnice
            .Include(r => r.Magacin)
            .Include(r => r.Stavke).ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(r => r.RacunOtpremnicaId == racunOtpremnicaId);
        if (racun == null) throw new InvalidOperationException("Račun nije pronađen.");
        if (!racun.IsKnjizen) throw new InvalidOperationException("Račun nije proknjižen.");

        if (racun.Magacin != null && racun.Stavke.Count > 0)
        {
            var kartice = new MaterijalnaKarticaService(_db);
            foreach (var s in racun.Stavke.AsEnumerable().Reverse())
            {
                string sifraArtikla = s.Artikal?.SifraArtikla ?? s.SifraArtikla;
                await kartice.UkloniPoslednjiRedAsync(racun.Magacin.SifraMagacina, sifraArtikla, $"Račun {racun.BrojRacuna}");
            }
        }

        if (racun.NalogId.HasValue)
        {
            var nalog = await _db.Nalozi.FirstOrDefaultAsync(n => n.NalogId == racun.NalogId.Value);
            if (nalog != null)
            {
                _db.Nalozi.Remove(nalog);
            }
        }

        racun.IsKnjizen = false;
        racun.NalogId = null;
        await _db.SaveChangesAsync();
    }
}

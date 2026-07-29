using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

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

    public async Task KnjiziRacunAsync(int racunOtpremnicaId)
    {
        var racun = await GetRacunByIdAsync(racunOtpremnicaId);
        if (racun == null) throw new InvalidOperationException("Račun nije pronađen.");
        if (racun.IsKnjizen) throw new InvalidOperationException("Račun je već proknjižen.");

        // Automatsko kreiranje naloga knjiženja u Glavnoj knjizi
        int sledeciBrojNaloga = await _db.Nalozi.Select(n => n.BrojNaloga).DefaultIfEmpty(0).MaxAsync() + 1;
        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            DatumNaloga = racun.DatumRacuna,
            VrstaNaloga = "Prodaja",
            Opis = $"Račun-otpremnica br. {racun.BrojRacuna}",
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now
        };

        // 1. Duguje Kupac (Konto 2040)
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 1,
            BrojKonta = "2040",
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

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        racun.IsKnjizen = true;
        racun.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();
    }
}

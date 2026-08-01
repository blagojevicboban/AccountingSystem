using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class KompenzacijaService
{
    private readonly AccountingDbContext _db;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;

    public KompenzacijaService(AccountingDbContext db)
    {
        _db = db;
        _zatvaranjeService = new ZatvaranjeStavkiService(_db);
    }

    /// <summary>
    /// Pametni mehanizam skenira partnere koji istovremeno imaju otvoren dug kao kupci i kao dobavljači
    /// </summary>
    public async Task<List<ObostranoDugovanjeCandidate>> GetObostranaDugovanjaAsync()
    {
        var partneri = await _db.Partneri.ToListAsync();
        var rezultat = new List<ObostranoDugovanjeCandidate>();

        foreach (var p in partneri)
        {
            var otvoreneStavke = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(p.PartnerId, DateTime.Today, samoOtvorene: true);

            decimal potrazivanjeKupac = otvoreneStavke
                .Where(s => s.BrojKonta.StartsWith("2040") || s.BrojKonta.StartsWith("204"))
                .Sum(s => s.Preostalo);

            decimal obavezaDobavljac = otvoreneStavke
                .Where(s => s.BrojKonta.StartsWith("4350") || s.BrojKonta.StartsWith("435"))
                .Sum(s => s.Preostalo);

            if (potrazivanjeKupac > 0 && obavezaDobavljac > 0)
            {
                rezultat.Add(new ObostranoDugovanjeCandidate
                {
                    PartnerId = p.PartnerId,
                    NazivPartnera = p.Naziv,
                    Pib = p.Pib ?? "",
                    PotrazivanjeKupac = potrazivanjeKupac,
                    ObavezaDobavljac = obavezaDobavljac
                });
            }
        }

        return rezultat.OrderByDescending(r => r.MaksimalnaKompenzacija).ToList();
    }

    public async Task<List<Kompenzacija>> GetKompenzacijeAsync()
    {
        return await _db.Kompenzacije
            .Include(k => k.Stavke)
            .OrderByDescending(k => k.Datum)
            .ThenByDescending(k => k.KompenzacijaId)
            .ToListAsync();
    }

    public async Task<Kompenzacija?> GetKompenzacijaByIdAsync(int id)
    {
        return await _db.Kompenzacije
            .Include(k => k.Stavke)
            .FirstOrDefaultAsync(k => k.KompenzacijaId == id);
    }

    public async Task<Kompenzacija> SacuvajKompenzacijuAsync(Kompenzacija kompenzacija)
    {
        kompenzacija.UkupanIznosKompenzacije = kompenzacija.Stavke.Sum(s => s.IznosZaKompenzaciju);

        if (kompenzacija.KompenzacijaId == 0)
        {
            if (string.IsNullOrWhiteSpace(kompenzacija.BrojDokumenta))
            {
                int sledeciBroj = await _db.Kompenzacije.CountAsync() + 1;
                kompenzacija.BrojDokumenta = $"KOM-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.Kompenzacije.Add(kompenzacija);
        }
        else
        {
            var postojeceStavke = _db.KompenzacijeStavke.Where(s => s.KompenzacijaId == kompenzacija.KompenzacijaId);
            _db.KompenzacijeStavke.RemoveRange(postojeceStavke);

            _db.Kompenzacije.Update(kompenzacija);
        }

        await _db.SaveChangesAsync();
        return kompenzacija;
    }

    public async Task<bool> ObrisiKompenzacijuAsync(int id)
    {
        var kompenzacija = await _db.Kompenzacije.FindAsync(id);
        if (kompenzacija == null || kompenzacija.IsKnjizeno) return false;

        _db.Kompenzacije.Remove(kompenzacija);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Knjiženje kompenzacije u Glavnoj knjizi i automatsko zatvaranje otvorenih stavki (IOS)
    /// </summary>
    public async Task<(bool Success, string Message, int? NalogId)> KnjiziIZatvoriKompenzacijuAsync(
        int kompenzacijaId, string korisnikId = "admin", string korisnickoIme = "Administrator")
    {
        var kompenzacija = await GetKompenzacijaByIdAsync(kompenzacijaId);
        if (kompenzacija == null) return (false, "Kompenzacija ne postoji.", null);

        if (kompenzacija.IsKnjizeno)
        {
            return (false, "Kompenzacija je već proknjižena.", kompenzacija.NalogId);
        }

        if (kompenzacija.UkupanIznosKompenzacije <= 0)
        {
            return (false, "Iznos kompenzacije mora biti veći od 0.", null);
        }

        int sledeciBrojNaloga = await _db.Nalozi.CountAsync() + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            VrstaNaloga = "KOM",
            DatumNaloga = kompenzacija.Datum,
            Opis = $"Kompenzacija br. {kompenzacija.BrojDokumenta} za partnera {kompenzacija.NazivPartnera}",
            IsKnjizen = true,
            UkupnoDuguje = kompenzacija.UkupanIznosKompenzacije,
            UkupnoPotrazuje = kompenzacija.UkupanIznosKompenzacije
        };

        // Stavka 1: Duguje Konto 4350 (Zatvaranje obaveze prema dobavljaču)
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 1,
            BrojKonta = "4350",
            Opis = $"Kompenzacija obaveze br. {kompenzacija.BrojDokumenta}",
            Duguje = kompenzacija.UkupanIznosKompenzacije,
            Potrazuje = 0m,
            BrojDokumenta = kompenzacija.BrojDokumenta,
            DatumDokumenta = kompenzacija.Datum,
            PartnerId = kompenzacija.PartnerId
        });

        // Stavka 2: Potražuje Konto 2040 (Zatvaranje potraživanja od kupca)
        nalog.Stavke.Add(new StavkaNaloga
        {
            RedniBroj = 2,
            BrojKonta = "2040",
            Opis = $"Kompenzacija potraživanja br. {kompenzacija.BrojDokumenta}",
            Duguje = 0m,
            Potrazuje = kompenzacija.UkupanIznosKompenzacije,
            BrojDokumenta = kompenzacija.BrojDokumenta,
            DatumDokumenta = kompenzacija.Datum,
            PartnerId = kompenzacija.PartnerId
        });

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        // Automatsko zatvaranje otvorenih stavki u IOS-u
        foreach (var st in kompenzacija.Stavke)
        {
            if (st.StavkaNalogaId > 0 && st.IznosZaKompenzaciju > 0)
            {
                var nalogStavkaDug = nalog.Stavke.First(s => s.BrojKonta == (st.Strana == "Duguje" ? "4350" : "2040"));
                await _zatvaranjeService.ZatvoriAsync(
                    stavkaDugujeId: st.Strana == "Duguje" ? st.StavkaNalogaId : nalogStavkaDug.StavkaNalogaId,
                    stavkaPotrazujeId: st.Strana == "Duguje" ? nalogStavkaDug.StavkaNalogaId : st.StavkaNalogaId,
                    iznos: st.IznosZaKompenzaciju,
                    datum: kompenzacija.Datum,
                    vrstaZatvaranja: "Kompenzacija",
                    napomena: $"Automatsko zatvaranje po kompenzaciji br. {kompenzacija.BrojDokumenta}",
                    korisnikId: 1,
                    korisnickoIme: korisnickoIme
                );
            }
        }

        kompenzacija.IsKnjizeno = true;
        kompenzacija.Status = "Proknjiženo";
        kompenzacija.NalogId = nalog.NalogId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno proknjižena kompenzacija br. {kompenzacija.BrojDokumenta} (Nalog KOM br. {sledeciBrojNaloga}) i zatvorene stavke u IOS-u!", nalog.NalogId);
    }
}

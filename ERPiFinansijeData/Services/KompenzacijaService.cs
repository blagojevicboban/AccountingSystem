using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

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
        // Ukupan iznos kompenzacije je NETO iznos koji se zaista prebija (manja od dve strane),
        // ne zbir obe strane — Stavke sadrži i potraživanja (Strana="Duguje") i obaveze
        // (Strana="Potražuje") u istoj listi, pa bi prost zbir udvostručio iznos.
        decimal zbirPotrazivanja = kompenzacija.Stavke.Where(s => s.Strana == "Duguje").Sum(s => s.IznosZaKompenzaciju);
        decimal zbirObaveza = kompenzacija.Stavke.Where(s => s.Strana == "Potražuje").Sum(s => s.IznosZaKompenzaciju);
        kompenzacija.UkupanIznosKompenzacije = Math.Min(zbirPotrazivanja, zbirObaveza);

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

        // Zbir potraživanja (Strana="Duguje", konto kupca) i zbir obaveza (Strana="Potražuje", konto
        // dobavljača) uključenih u kompenzaciju moraju biti jednaki da bi povezani nalog ostao u ravnoteži —
        // ovo važi i za Dvojnu kompenzaciju (1 partner) i za Asignaciju/Cesiju (2-3 partnera), jer se u oba
        // slučaja prebijaju iste dve strane, samo raspoređene na više partnerovih analitičkih kartica.
        decimal zbirPotrazivanja = kompenzacija.Stavke.Where(s => s.Strana == "Duguje").Sum(s => s.IznosZaKompenzaciju);
        decimal zbirObaveza = kompenzacija.Stavke.Where(s => s.Strana == "Potražuje").Sum(s => s.IznosZaKompenzaciju);
        if (Math.Abs(zbirPotrazivanja - zbirObaveza) > 0.01m)
        {
            return (false, $"Zbir potraživanja ({zbirPotrazivanja:N2}) mora biti jednak zbiru obaveza ({zbirObaveza:N2}) uključenih u kompenzaciju.", null);
        }

        int sledeciBrojNaloga = await _db.Nalozi.CountAsync() + 1;

        var nalog = new Nalog
        {
            BrojNaloga = sledeciBrojNaloga,
            VrstaNaloga = "KOM",
            DatumNaloga = kompenzacija.Datum,
            Opis = $"Kompenzacija br. {kompenzacija.BrojDokumenta} ({kompenzacija.Vrsta})",
            IsKnjizen = true
        };

        // Po jedna zatvarajuća linija za svaku (ugovorna strana, Strana) grupu uključenu u kompenzaciju —
        // kod Dvojne kompenzacije to su tačno 2 linije (isti partner na 4350 i 2040) kao ranije; kod
        // Asignacije/Cesije po jedna linija za svakog od 2-3 uključenih partnera, tako da svačija
        // sopstvena analitička kartica (204 ili 435) dobije ispravno zatvaranje. "Ugovorna strana" može
        // biti pravi partner (PartnerId>0, ključ "P{id}") ili sintetički partner — legacy analitički
        // konto bez veze u šifarniku (PartnerId==0), gde ključ mora biti sam broj konta ("K{konto}") jer
        // više različitih legacy konta može imati PartnerId==0 u istoj kompenzaciji (npr. Cesija između
        // dva legacy konta) i ne smeju se stopiti u jednu zajedničku liniju.
        var noveLinije = new Dictionary<(string Kljuc, string Strana), StavkaNaloga>();
        int rbr = 1;

        foreach (var grupa in kompenzacija.Stavke.GroupBy(s => (Kljuc: s.PartnerId > 0 ? $"P{s.PartnerId}" : $"K{s.BrojKonta}", s.Strana)))
        {
            decimal iznos = grupa.Sum(s => s.IznosZaKompenzaciju);
            if (iznos <= 0) continue;

            bool jeSinteticki = grupa.Key.Kljuc.StartsWith("K");
            int? partnerIdZaLiniju = jeSinteticki ? null : grupa.First().PartnerId;
            // Pravi partner: standardno sintetičko konto (2040/4350). Legacy konto: TAČAN broj konta
            // (npr. "204457"), da bi zatvarajuća linija ostala vidljiva na kartici baš tog konta.
            string kontoZaLiniju = jeSinteticki ? grupa.First().BrojKonta : (grupa.Key.Strana == "Duguje" ? "2040" : "4350");

            StavkaNaloga linija = grupa.Key.Strana == "Duguje"
                ? new StavkaNaloga // zatvara potraživanje od kupca (204) -> nova Potražuje linija
                {
                    RedniBroj = rbr++,
                    BrojKonta = kontoZaLiniju,
                    Opis = $"Kompenzacija potraživanja br. {kompenzacija.BrojDokumenta}",
                    Duguje = 0m,
                    Potrazuje = iznos,
                    BrojDokumenta = kompenzacija.BrojDokumenta,
                    DatumDokumenta = kompenzacija.Datum,
                    PartnerId = partnerIdZaLiniju
                }
                : new StavkaNaloga // zatvara obavezu prema dobavljaču (435) -> nova Duguje linija
                {
                    RedniBroj = rbr++,
                    BrojKonta = kontoZaLiniju,
                    Opis = $"Kompenzacija obaveze br. {kompenzacija.BrojDokumenta}",
                    Duguje = iznos,
                    Potrazuje = 0m,
                    BrojDokumenta = kompenzacija.BrojDokumenta,
                    DatumDokumenta = kompenzacija.Datum,
                    PartnerId = partnerIdZaLiniju
                };

            nalog.Stavke.Add(linija);
            noveLinije[grupa.Key] = linija;
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        // Automatsko zatvaranje otvorenih stavki u IOS-u
        foreach (var st in kompenzacija.Stavke)
        {
            string kljuc = st.PartnerId > 0 ? $"P{st.PartnerId}" : $"K{st.BrojKonta}";
            if (st.StavkaNalogaId > 0 && st.IznosZaKompenzaciju > 0 &&
                noveLinije.TryGetValue((kljuc, st.Strana), out var novaLinija))
            {
                await _zatvaranjeService.ZatvoriAsync(
                    stavkaDugujeId: st.Strana == "Duguje" ? st.StavkaNalogaId : novaLinija.StavkaNalogaId,
                    stavkaPotrazujeId: st.Strana == "Duguje" ? novaLinija.StavkaNalogaId : st.StavkaNalogaId,
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

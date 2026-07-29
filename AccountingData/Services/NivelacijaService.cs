using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AccountingData.Models;

namespace AccountingData.Services;

public class NivelacijaService
{
    public static async Task<List<NivelacijaCena>> GetNivelacijeAsync(AccountingDbContext db, string? pretraga = null)
    {
        var query = db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke)
                .ThenInclude(s => s.Artikal)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(pretraga))
        {
            pretraga = pretraga.ToLower();
            query = query.Where(n =>
                n.BrojNivelacije.ToString().Contains(pretraga) ||
                (n.Opis != null && n.Opis.ToLower().Contains(pretraga)) ||
                (n.Magacin != null && n.Magacin.NazivMagacina.ToLower().Contains(pretraga)));
        }

        var list = await query.OrderByDescending(n => n.DatumNivelacije).ThenByDescending(n => n.NivelacijaCenaId).ToListAsync();

        foreach (var niv in list)
        {
            if (niv.Magacin != null)
            {
                niv.SifraMagacina = niv.Magacin.SifraMagacina;
                niv.NazivMagacina = niv.Magacin.NazivMagacina;
            }

            foreach (var st in niv.Stavke)
            {
                if (st.Artikal != null)
                {
                    st.SifraArtikla = st.Artikal.SifraArtikla;
                    st.NazivArtikla = st.Artikal.Naziv;
                    st.JedinicaMere = st.Artikal.JedinicaMere;
                }
            }
        }

        return list;
    }

    public static async Task<NivelacijaCena?> GetNivelacijaByIdAsync(AccountingDbContext db, int id)
    {
        var niv = await db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke)
                .ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(n => n.NivelacijaCenaId == id);

        if (niv != null)
        {
            if (niv.Magacin != null)
            {
                niv.SifraMagacina = niv.Magacin.SifraMagacina;
                niv.NazivMagacina = niv.Magacin.NazivMagacina;
            }

            foreach (var st in niv.Stavke)
            {
                if (st.Artikal != null)
                {
                    st.SifraArtikla = st.Artikal.SifraArtikla;
                    st.NazivArtikla = st.Artikal.Naziv;
                    st.JedinicaMere = st.Artikal.JedinicaMere;
                }
            }
        }

        return niv;
    }

    public static async Task<NivelacijaCena> SaveNivelacijaAsync(AccountingDbContext db, NivelacijaCena niv)
    {
        niv.UkupnoRazlika = niv.Stavke.Sum(s => s.UkupnaRazlika);

        if (niv.NivelacijaCenaId == 0)
        {
            db.NivelacijeCena.Add(niv);
        }
        else
        {
            var existing = await db.NivelacijeCena.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NivelacijaCenaId == niv.NivelacijaCenaId);
            if (existing != null)
            {
                db.Entry(existing).CurrentValues.SetValues(niv);
                db.NivelacijaStavke.RemoveRange(existing.Stavke);
                foreach (var st in niv.Stavke)
                {
                    st.NivelacijaStavkaId = 0;
                    st.NivelacijaCenaId = niv.NivelacijaCenaId;
                    existing.Stavke.Add(st);
                }
            }
        }

        await db.SaveChangesAsync();
        return niv;
    }

    public static async Task<bool> KnjiziNivelacijuAsync(AccountingDbContext db, int id)
    {
        var niv = await db.NivelacijeCena
            .Include(n => n.Magacin)
            .Include(n => n.Stavke)
                .ThenInclude(s => s.Artikal)
            .FirstOrDefaultAsync(n => n.NivelacijaCenaId == id);

        if (niv == null || niv.IsKnjizen) return false;

        // Ažuriranje cena u artiklima
        foreach (var st in niv.Stavke)
        {
            if (st.Artikal != null && st.NovaCena > 0)
            {
                st.Artikal.ProdajnaCena = st.NovaCena;
            }
        }

        // Kreiranje naloga knjiženja za razliku u ceni
        if (niv.UkupnoRazlika != 0)
        {
            string kontoMagacina = niv.Magacin?.VrstaMagacina == "Maloprodaja" ? "1340" : "1320";
            string kontoRazlike = "1329";

            int sledeciBrojNaloga = await db.Nalozi.Select(n => n.BrojNaloga).DefaultIfEmpty(0).MaxAsync() + 1;
            var nalog = new Nalog
            {
                BrojNaloga = sledeciBrojNaloga,
                DatumNaloga = niv.DatumNivelacije,
                Opis = $"Nivelacija cena br. {niv.BrojNivelacije}",
                IsKnjizen = true,
                DatumKnjiženja = DateTime.Now
            };

            if (niv.UkupnoRazlika > 0)
            {
                // Povećanje vrednosti robe i razlike u ceni
                nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 1, BrojKonta = kontoMagacina, Opis = nalog.Opis, Duguje = niv.UkupnoRazlika, Potrazuje = 0 });
                nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 2, BrojKonta = kontoRazlike, Opis = nalog.Opis, Duguje = 0, Potrazuje = niv.UkupnoRazlika });
            }
            else
            {
                // Smanjenje vrednosti robe i razlike u ceni
                decimal absRazlika = Math.Abs(niv.UkupnoRazlika);
                nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 1, BrojKonta = kontoRazlike, Opis = nalog.Opis, Duguje = absRazlika, Potrazuje = 0 });
                nalog.Stavke.Add(new StavkaNaloga { RedniBroj = 2, BrojKonta = kontoMagacina, Opis = nalog.Opis, Duguje = 0, Potrazuje = absRazlika });
            }

            nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
            nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

            db.Nalozi.Add(nalog);
            await db.SaveChangesAsync();
            niv.NalogId = nalog.NalogId;
        }

        niv.IsKnjizen = true;
        await db.SaveChangesAsync();
        return true;
    }

    public static async Task<int> MasovnoKnjizenjeNivelacijaAsync(AccountingDbContext db)
    {
        var neknjizene = await db.NivelacijeCena.Where(n => !n.IsKnjizen).Select(n => n.NivelacijaCenaId).ToListAsync();
        int count = 0;
        foreach (var id in neknjizene)
        {
            if (await KnjiziNivelacijuAsync(db, id)) count++;
        }
        return count;
    }

    /// <summary>
    /// Clipper svodj_pros_p() / generisanje_niv():
    /// Automatski generiše Zapisnik o nivelaciji cena poređenjem zaliha i prosečnih prodajnih cena za izabrani magacin.
    /// </summary>
    public static async Task<NivelacijaCena?> SvodjenjeNaProdajnuVrednostAsync(AccountingDbContext db, int magacinId, DateTime datumNaloga)
    {
        var magacin = await db.Magacini.FirstOrDefaultAsync(m => m.MagacinId == magacinId);
        if (magacin == null) return null;

        var kartice = await db.MaterijalneKartice
            .Where(k => k.SifraMagacina == magacin.SifraMagacina && k.DatumPromene <= datumNaloga)
            .ToListAsync();

        if (kartice.Count == 0) return null;

        var artikliDict = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);

        var artikliStanja = kartice
            .GroupBy(k => k.SifraArtikla, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var last = g.OrderBy(k => k.DatumPromene).ThenBy(k => k.MaterijalnaKarticaId).LastOrDefault();
                decimal zaliha = last?.Stanje ?? 0m;
                artikliDict.TryGetValue(g.Key, out var art);
                decimal staraCena = last?.Cena ?? (art?.ProdajnaCena ?? 0m);

                decimal ukUlazKolicina = g.Sum(k => k.Ulaz);
                decimal ukUlazVrednost = g.Sum(k => k.Ulaz * k.Cena);
                decimal prosecnaCena = ukUlazKolicina > 0 ? ukUlazVrednost / ukUlazKolicina : staraCena;

                return new
                {
                    SifraArtikla = g.Key,
                    Artikal = art,
                    Zaliha = zaliha,
                    StaraCena = staraCena,
                    NovaCena = Math.Round(prosecnaCena, 2)
                };
            })
            .Where(x => x.Zaliha > 0 && Math.Abs(x.NovaCena - x.StaraCena) >= 0.01m)
            .ToList();

        if (artikliStanja.Count == 0) return null;

        int sledeciBroj = await db.NivelacijeCena.Select(n => n.BrojNivelacije).DefaultIfEmpty(0).MaxAsync() + 1;

        var niv = new NivelacijaCena
        {
            BrojNivelacije = sledeciBroj,
            DatumNivelacije = datumNaloga,
            MagacinId = magacinId,
            SifraMagacina = magacin.SifraMagacina,
            NazivMagacina = magacin.NazivMagacina,
            Opis = $"Automatsko svođenje cena na prosečnu prodajnu vrednost za magacin {magacin.NazivMagacina}",
            IsKnjizen = false
        };

        int rbr = 1;
        foreach (var x in artikliStanja)
        {
            decimal razlikaPoJed = x.NovaCena - x.StaraCena;
            decimal ukupnaRazlika = Math.Round(x.Zaliha * razlikaPoJed, 2);

            niv.Stavke.Add(new NivelacijaStavka
            {
                RedniBroj = rbr++,
                ArtikalId = x.Artikal?.ArtikalId,
                SifraArtikla = x.SifraArtikla,
                NazivArtikla = x.Artikal?.Naziv ?? "",
                JedinicaMere = x.Artikal?.JedinicaMere ?? "kom",
                KolicinaZaliha = x.Zaliha,
                StaraCena = x.StaraCena,
                NovaCena = x.NovaCena,
                RazlikaPoJedinici = razlikaPoJed,
                UkupnaRazlika = ukupnaRazlika
            });
        }

        niv.UkupnoRazlika = niv.Stavke.Sum(s => s.UkupnaRazlika);

        db.NivelacijeCena.Add(niv);
        await db.SaveChangesAsync();

        return niv;
    }
}

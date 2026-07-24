using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

/// <summary>
/// Vodi materijalnu karticu po prosečnoj (ponderisanoj) ceni — analogno legacy
/// dodaj_mat_kar / ul_dodaj_m_kar / tr_dodaj_m_kar procedurama iz M2/M3.PRG.
///
/// Formula validirana protiv stvarnog legacy M_KART.DBF snapshota (magacin 001,
/// artikal 22560, 22 stavke): prijem knjiži se po UNETOJ ceni (Saldo se samo
/// akumulira, Cena reda = unet cena), a izdavanje/korekcija po TRENUTNOJ
/// prosečnoj ceni = tekući Saldo / tekuće Stanje — brojevi se poklapaju na
/// dve decimale kroz celu istoriju tog artikla.
/// </summary>
public class MaterijalnaKarticaService
{
    private readonly AccountingDbContext _db;

    public MaterijalnaKarticaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Magacin>> GetMagaciniAsync(string? search = null)
    {
        var query = _db.Magacini.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.SifraMagacina.Contains(search) || m.NazivMagacina.Contains(search));
        }
        return await query.OrderBy(m => m.SifraMagacina).ToListAsync();
    }

    public async Task<List<Artikal>> GetArtikliAsync(string? search = null)
    {
        var query = _db.Artikli.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.SifraArtikla.Contains(search) || a.Naziv.Contains(search));
        }
        return await query.OrderBy(a => a.Naziv).ToListAsync();
    }

    public async Task<List<MaterijalnaKartica>> GetKarticaAsync(string sifraMagacina, string sifraArtikla)
    {
        return await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderBy(k => k.DatumPromene)
            .ThenBy(k => k.RedniBroj)
            .ToListAsync();
    }

    /// <summary>Tekuće stanje (količina) i tekući saldo (vrednost) — prosečna cena = saldo/stanje.</summary>
    public async Task<(decimal stanje, decimal saldo)> GetTrenutnoStanjeAsync(string sifraMagacina, string sifraArtikla)
    {
        var poslednja = await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderByDescending(k => k.RedniBroj)
            .FirstOrDefaultAsync();

        return poslednja == null ? (0m, 0m) : (poslednja.Stanje, poslednja.Saldo);
    }

    private async Task<int> SledeciRedniBrojAsync(string sifraMagacina, string sifraArtikla)
    {
        var poslednji = await _db.MaterijalneKartice
            .Where(k => k.SifraMagacina == sifraMagacina && k.SifraArtikla == sifraArtikla)
            .OrderByDescending(k => k.RedniBroj)
            .Select(k => (int?)k.RedniBroj)
            .FirstOrDefaultAsync();
        return (poslednji ?? 0) + 1;
    }

    /// <summary>Prijem robe — knjiži se po unetoj ceni; Saldo se akumulira (staro_saldo + kolicina*cena).</summary>
    public async Task DodajUlazRedAsync(string sifraMagacina, string sifraArtikla, DateTime datum, string opis, decimal kolicina, decimal cena)
    {
        var (staroStanje, staroSaldo) = await GetTrenutnoStanjeAsync(sifraMagacina, sifraArtikla);
        decimal iznos = kolicina * cena;

        _db.MaterijalneKartice.Add(new MaterijalnaKartica
        {
            SifraMagacina = sifraMagacina,
            SifraArtikla = sifraArtikla,
            RedniBroj = await SledeciRedniBrojAsync(sifraMagacina, sifraArtikla),
            DatumPromene = datum,
            OpisPromene = opis,
            Ulaz = kolicina,
            Izlaz = 0,
            Stanje = staroStanje + kolicina,
            Cena = cena,
            CenaIzlaz = 0,
            Duguje = iznos,
            Potrazuje = 0,
            Saldo = staroSaldo + iznos
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Izdavanje (trebovanje) — po TRENUTNOJ prosečnoj ceni (saldo/stanje), ne po
    /// nekoj unetoj ceni, jer se roba na zalihama vrednuje po ponderisanoj nabavnoj
    /// ceni. Baca grešku ako bi stanje otišlo u minus.
    /// </summary>
    public async Task<decimal> DodajIzlazRedAsync(string sifraMagacina, string sifraArtikla, DateTime datum, string opis, decimal kolicina)
    {
        var (staroStanje, staroSaldo) = await GetTrenutnoStanjeAsync(sifraMagacina, sifraArtikla);
        if (kolicina > staroStanje)
        {
            throw new InvalidOperationException(
                $"Nedovoljno stanje na zalihama za artikal {sifraArtikla} u magacinu {sifraMagacina} " +
                $"(na stanju: {staroStanje:N2}, traženo: {kolicina:N2}).");
        }

        decimal prosecnaCena = staroStanje != 0m ? staroSaldo / staroStanje : 0m;
        decimal iznos = kolicina * prosecnaCena;

        _db.MaterijalneKartice.Add(new MaterijalnaKartica
        {
            SifraMagacina = sifraMagacina,
            SifraArtikla = sifraArtikla,
            RedniBroj = await SledeciRedniBrojAsync(sifraMagacina, sifraArtikla),
            DatumPromene = datum,
            OpisPromene = opis,
            Ulaz = 0,
            Izlaz = kolicina,
            Stanje = staroStanje - kolicina,
            Cena = prosecnaCena,
            CenaIzlaz = prosecnaCena,
            Duguje = 0,
            Potrazuje = iznos,
            Saldo = staroSaldo - iznos
        });
        await _db.SaveChangesAsync();
        return iznos;
    }
}

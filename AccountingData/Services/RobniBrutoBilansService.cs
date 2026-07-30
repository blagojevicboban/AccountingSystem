using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AccountingData.Models;

namespace AccountingData.Services;

public class RobniBrutoBilansRed
{
    public string SifraMagacina { get; set; } = string.Empty;
    public string NazivMagacina { get; set; } = string.Empty;

    public string SifraArtikla { get; set; } = string.Empty;
    public string NazivArtikla { get; set; } = string.Empty;
    public string? Pakovanje { get; set; }
    public string JedinicaMere { get; set; } = "kom";
    public decimal Cena { get; set; }

    public decimal PocetnoStanjeKolicina { get; set; }
    public decimal PocetnoStanjeVrednost { get; set; }

    public decimal UlazKolicina { get; set; }
    public decimal UlazVrednost { get; set; } // Duguje

    public decimal IzlazKolicina { get; set; }
    public decimal IzlazVrednost { get; set; } // Potrazuje

    public decimal SaldoKolicinski { get; set; }
    public decimal SaldoVrednosni { get; set; } // Saldo RSD
}

public class RobniBrutoBilansService
{
    /// <summary>Bruto bilans za Robno knjigovodstvo — samo šifre koje postoje u šifarniku Artikli (Roba).</summary>
    public static Task<List<RobniBrutoBilansRed>> GetRobniBrutoBilansAsync(
        AccountingDbContext db, int? magacinId = null, DateTime? doDatuma = null, string? pretraga = null)
        => IzracunajAsync(db, magacinId, doDatuma, pretraga, samoRoba: true);

    /// <summary>Bruto bilans za Materijalno knjigovodstvo — sve šifre koje NISU potvrđena Roba (Materijal + eventualni redovi bez artikla u šifarniku).</summary>
    public static Task<List<RobniBrutoBilansRed>> GetMaterijalniBrutoBilansAsync(
        AccountingDbContext db, int? magacinId = null, DateTime? doDatuma = null, string? pretraga = null)
        => IzracunajAsync(db, magacinId, doDatuma, pretraga, samoRoba: false);

    /// <summary>
    /// MaterijalneKartice je jedna deljena tabela za promet i Robnog i Materijalnog knjigovodstva i sama
    /// po sebi ne beleži kojoj seriji šifra pripada (Roba i Materijal su odvojene tabele koje mogu deliti
    /// istu šifru sa različitim značenjem). Razdvajanje se radi po tome da li je šifra poznata u Artikli.
    /// </summary>
    private static async Task<List<RobniBrutoBilansRed>> IzracunajAsync(
        AccountingDbContext db, int? magacinId, DateTime? doDatuma, string? pretraga, bool samoRoba)
    {
        string? trazeniMagacinSifra = null;
        if (magacinId.HasValue && magacinId.Value > 0)
        {
            var mag = await db.Magacini.FirstOrDefaultAsync(m => m.MagacinId == magacinId.Value);
            trazeniMagacinSifra = mag?.SifraMagacina;
        }

        var query = db.MaterijalneKartice.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(trazeniMagacinSifra))
        {
            query = query.Where(k => k.SifraMagacina == trazeniMagacinSifra);
        }

        if (doDatuma.HasValue)
        {
            DateTime krajDana = doDatuma.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(k => k.DatumPromene <= krajDana);
        }

        var karticeList = await query.ToListAsync();

        var magaciniMap = await db.Magacini.ToDictionaryAsync(m => m.SifraMagacina, m => m.NazivMagacina, StringComparer.OrdinalIgnoreCase);
        var robaMap = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);
        var materijalMap = samoRoba
            ? new Dictionary<string, Materijal>(StringComparer.OrdinalIgnoreCase)
            : await db.Materijali.ToDictionaryAsync(m => m.SifraArtikla, m => m, StringComparer.OrdinalIgnoreCase);

        var rezultat = karticeList
            .GroupBy(k => new { k.SifraMagacina, k.SifraArtikla })
            .Where(g => robaMap.ContainsKey(g.Key.SifraArtikla) == samoRoba)
            .Select(g =>
            {
                var first = g.First();
                materijalMap.TryGetValue(g.Key.SifraArtikla, out var mat);
                robaMap.TryGetValue(g.Key.SifraArtikla, out var rob);
                magaciniMap.TryGetValue(g.Key.SifraMagacina, out string? nazivMag);

                var last = g.OrderBy(k => k.DatumPromene).ThenBy(k => k.MaterijalnaKarticaId).LastOrDefault();

                decimal ukUlazKol = g.Sum(k => k.Ulaz);
                decimal ukUlazVred = g.Sum(k => k.Duguje);
                decimal ukIzlazKol = g.Sum(k => k.Izlaz);
                decimal ukIzlazVred = g.Sum(k => k.Potrazuje);

                decimal zadnjeStanjeKol = last?.Stanje ?? (ukUlazKol - ukIzlazKol);
                decimal zadnjiSaldoVred = last?.Saldo ?? (ukUlazVred - ukIzlazVred);
                decimal zadnjaCena = last?.Cena ?? (rob?.ProdajnaCena ?? 0m);

                return new RobniBrutoBilansRed
                {
                    SifraMagacina = g.Key.SifraMagacina,
                    NazivMagacina = nazivMag ?? g.Key.SifraMagacina,
                    SifraArtikla = g.Key.SifraArtikla,
                    NazivArtikla = mat?.Naziv ?? rob?.Naziv ?? g.Key.SifraArtikla,
                    Pakovanje = mat?.Pakovanje ?? rob?.Pakovanje,
                    JedinicaMere = mat?.JedinicaMere ?? rob?.JedinicaMere ?? "kom",
                    Cena = zadnjaCena,

                    UlazKolicina = ukUlazKol,
                    UlazVrednost = ukUlazVred,

                    IzlazKolicina = ukIzlazKol,
                    IzlazVrednost = ukIzlazVred,

                    SaldoKolicinski = zadnjeStanjeKol,
                    SaldoVrednosni = zadnjiSaldoVred
                };
            })
            .OrderBy(r => r.SifraMagacina)
            .ThenBy(r => r.SifraArtikla)
            .ToList();

        if (!string.IsNullOrWhiteSpace(pretraga))
        {
            string s = pretraga.ToLower();
            rezultat = rezultat.Where(r =>
                r.SifraArtikla.ToLower().Contains(s) ||
                r.NazivArtikla.ToLower().Contains(s) ||
                r.SifraMagacina.ToLower().Contains(s) ||
                r.NazivMagacina.ToLower().Contains(s)).ToList();
        }

        return rezultat;
    }
}

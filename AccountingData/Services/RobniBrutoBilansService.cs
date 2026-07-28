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
    public static async Task<List<RobniBrutoBilansRed>> GetRobniBrutoBilansAsync(
        AccountingDbContext db,
        int? magacinId = null,
        DateTime? doDatuma = null,
        string? pretraga = null)
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
        var artikliMap = await db.Artikli.ToDictionaryAsync(a => a.SifraArtikla, a => a, StringComparer.OrdinalIgnoreCase);

        var rezultat = karticeList
            .GroupBy(k => new { k.SifraMagacina, k.SifraArtikla })
            .Select(g =>
            {
                var first = g.First();
                artikliMap.TryGetValue(g.Key.SifraArtikla, out var art);
                magaciniMap.TryGetValue(g.Key.SifraMagacina, out string? nazivMag);

                var last = g.OrderBy(k => k.DatumPromene).ThenBy(k => k.MaterijalnaKarticaId).LastOrDefault();

                decimal ukUlazKol = g.Sum(k => k.Ulaz);
                decimal ukUlazVred = g.Sum(k => k.Duguje);
                decimal ukIzlazKol = g.Sum(k => k.Izlaz);
                decimal ukIzlazVred = g.Sum(k => k.Potrazuje);

                decimal zadnjeStanjeKol = last?.Stanje ?? (ukUlazKol - ukIzlazKol);
                decimal zadnjiSaldoVred = last?.Saldo ?? (ukUlazVred - ukIzlazVred);
                decimal zadnjaCena = last?.Cena ?? (art?.ProdajnaCena ?? 0m);

                return new RobniBrutoBilansRed
                {
                    SifraMagacina = g.Key.SifraMagacina,
                    NazivMagacina = nazivMag ?? g.Key.SifraMagacina,
                    SifraArtikla = g.Key.SifraArtikla,
                    NazivArtikla = art?.Naziv ?? g.Key.SifraArtikla,
                    JedinicaMere = art?.JedinicaMere ?? "kom",
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

using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class NovaGodinaService
{
    private readonly AccountingDbContext _db;

    public NovaGodinaService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Zaključni saldo po kontu na kraju zadate godine (kumulativno, iz svih
    /// proknjiženih naloga zaključno sa 31.12. te godine) — analogno legacy
    /// ngod_prenos proceduri iz FIN2.PRG.
    /// </summary>
    public async Task<List<BrutoBilansRed>> GetZakljucniSaldoAsync(int godina)
    {
        var krajGodine = new DateTime(godina, 12, 31, 23, 59, 59);

        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga <= krajGodine)
            .ToListAsync();

        var konta = await _db.Konta.ToDictionaryAsync(k => k.BrojKonta, k => k.NazivKonta);

        return stavke
            .GroupBy(s => s.BrojKonta)
            .Select(g =>
            {
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                return new BrutoBilansRed
                {
                    BrojKonta = g.Key,
                    NazivKonta = konta.TryGetValue(g.Key, out var naziv) ? naziv : g.Key,
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    Saldo = duguje - potrazuje
                };
            })
            .Where(r => r.Saldo != 0m)
            .OrderBy(r => r.BrojKonta)
            .ToList();
    }

    private static string BrojPrenosa(int novaGodina) => $"PS-{novaGodina}";

    public async Task<bool> PostojiPrenosAsync(int novaGodina)
    {
        return await _db.Nalozi.AnyAsync(n => n.BrojNaloga == BrojPrenosa(novaGodina));
    }

    /// <summary>
    /// Kreira i odmah knjiži nalog za prenos početnog stanja u novu godinu — jedna
    /// stavka po kontu sa nenultim zaključnim saldom (Duguje ako je saldo pozitivan,
    /// Potražuje ako je negativan). Pošto je zbir svih salda po definiciji 0
    /// (svaki nalog je proveren u ravnoteži pri knjiženju), rezultujući nalog je
    /// automatski u ravnoteži.
    /// </summary>
    public async Task<Nalog> PrenesiUNovuGoduAsync(int izvornaGodina)
    {
        int novaGodina = izvornaGodina + 1;
        string brojPrenosa = BrojPrenosa(novaGodina);

        if (await PostojiPrenosAsync(novaGodina))
        {
            throw new InvalidOperationException($"Prenos početnog stanja za {novaGodina}. godinu je već izvršen (nalog {brojPrenosa}).");
        }

        var saldoPoKontu = await GetZakljucniSaldoAsync(izvornaGodina);
        if (saldoPoKontu.Count == 0)
        {
            throw new InvalidOperationException($"Nema proknjiženih naloga sa nenultim saldom zaključno sa {izvornaGodina}. godinom — nema šta da se prenese.");
        }

        // Zbir salda svih konta mora biti 0 (svaki proknjižen nalog je pojedinačno
        // u ravnoteži, pa zbir svih naloga mora biti takođe). Ako nije, u knjigama
        // postoji neispravan (neuravnotežen) proknjižen nalog — bolje odbiti prenos
        // nego tiho preneti neuravnoteženo početno stanje u novu godinu.
        decimal ukupanSaldo = saldoPoKontu.Sum(r => r.Saldo);
        if (Math.Abs(ukupanSaldo) >= 0.01m)
        {
            throw new InvalidOperationException(
                $"Knjige nisu u ravnoteži zaključno sa {izvornaGodina}. godinom (razlika {ukupanSaldo:N2}) — " +
                "verovatno postoji neispravan proknjižen nalog. Ispravite ga (npr. preko Rasknjiži) pre prenosa u novu godinu.");
        }

        var nalog = new Nalog
        {
            BrojNaloga = brojPrenosa,
            DatumNaloga = new DateTime(novaGodina, 1, 1),
            VrstaNaloga = "Finansijski",
            Opis = $"Prenos početnog stanja iz {izvornaGodina}. godine",
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now
        };

        int red = 1;
        foreach (var r in saldoPoKontu)
        {
            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = red++,
                BrojKonta = r.BrojKonta,
                Opis = "Preneseno početno stanje",
                Duguje = r.Saldo > 0 ? r.Saldo : 0m,
                Potrazuje = r.Saldo < 0 ? -r.Saldo : 0m
            });
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();
        return nalog;
    }
}

using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class OtvoreneStavkeService
{
    private readonly AccountingDbContext _db;

    public OtvoreneStavkeService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Partner>> GetPartneriAsync(string? search = null)
    {
        var query = _db.Partneri.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.SifraPartnera.Contains(search) || p.Naziv.Contains(search));
        }
        return await query.OrderBy(p => p.Naziv).ToListAsync();
    }

    /// <summary>
    /// Otvorene stavke (izvod) za partnera — hronološki, sa kumulativnim saldom,
    /// analogno legacy gk91/otv_st_zag proceduri iz FIN2.PRG, ali vezano preko
    /// StavkaNaloga.PartnerId (ne preko konta, jer legacy ANAL modul za ovu firmu
    /// nije korišćen pa nema podataka za uparivanje po kontu partnera).
    /// </summary>
    public async Task<List<KarticaRed>> GetOtvoreneStavkeAsync(int partnerId)
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.PartnerId == partnerId && s.Nalog != null && s.Nalog.IsKnjizen)
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        var rezultat = new List<KarticaRed>();
        decimal saldo = 0m;

        foreach (var s in stavke)
        {
            saldo += s.Duguje - s.Potrazuje;
            rezultat.Add(new KarticaRed
            {
                Datum = s.Nalog!.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis) : s.Opis,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = saldo
            });
        }

        return rezultat;
    }

    /// <summary>
    /// Bruto bilans analitike — promet i saldo po partneru (umesto po kontu), iz
    /// proknjiženih naloga sa dodeljenim partnerom. U legacy DOS sistemu ovo bi bio
    /// poseban ANAL modul izveštaj (A_brut_bil iz ANAL2.PRG) nad zasebnim ANNAL.DBF
    /// fajlom; ovde je isti podatak (StavkaNaloga.PartnerId) samo grupisan drugačije
    /// od finansijskog bruto bilansa (BrutoBilansService, grupisanog po kontu).
    /// </summary>
    public async Task<List<BrutoBilansAnalitikeRed>> GetBrutoBilansAnalitikeAsync()
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Where(s => s.PartnerId != null && s.Nalog != null && s.Nalog.IsKnjizen)
            .ToListAsync();

        return stavke
            .GroupBy(s => s.PartnerId!.Value)
            .Select(g =>
            {
                var partner = g.First().Partner;
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                return new BrutoBilansAnalitikeRed
                {
                    SifraPartnera = partner?.SifraPartnera ?? "?",
                    NazivPartnera = partner?.Naziv ?? "?",
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    Saldo = duguje - potrazuje
                };
            })
            .OrderBy(r => r.NazivPartnera)
            .ToList();
    }
}

public class BrutoBilansAnalitikeRed
{
    public string SifraPartnera { get; set; } = string.Empty;
    public string NazivPartnera { get; set; } = string.Empty;
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal Saldo { get; set; }
}

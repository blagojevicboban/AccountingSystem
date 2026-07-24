using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class KalkulacijaService
{
    private readonly AccountingDbContext _db;

    public KalkulacijaService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Čista formula obračuna veleprodajne kalkulacije — analogno legacy
    /// kalkknjizenje proceduri iz MAT2.PRG: nabavna vrednost + zavisni troškovi
    /// (transport, uskladištenje, utovar/istovar, osiguranje, ostalo) = svega
    /// nabavno; na to se dodaje trgovačka razlika (marža %) i porez (PDV %),
    /// dajući prodajnu vrednost. Bez zavisnosti od baze — testabilno u izolaciji.
    /// </summary>
    public static void Izracunaj(Kalkulacija k)
    {
        k.SvegaTroskovi = k.TransportniTroskovi + k.TroskoviUskladistenja + k.UtovarIstovar + k.TransportnoOsiguranje + k.OstaliTroskovi;
        k.SvegaNabavno = k.NabavnaVrednost + k.SvegaTroskovi;
        k.Razlika = Math.Round(k.SvegaNabavno * k.MarzaProcenat / 100m, 2);
        k.Porez = Math.Round((k.SvegaNabavno + k.Razlika) * k.PoreskaStopaProcenat / 100m, 2);
        k.ProdajnaVrednost = k.SvegaNabavno + k.Razlika + k.Porez;
    }

    public async Task<List<Kalkulacija>> GetKalkulacijeAsync(string? search = null)
    {
        var query = _db.Kalkulacije.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(k => k.BrojKalkulacije.Contains(search));
        }
        return await query.OrderByDescending(k => k.Datum).ToListAsync();
    }

    public async Task<Kalkulacija> SaveKalkulacijuAsync(Kalkulacija kalkulacija)
    {
        Izracunaj(kalkulacija);

        if (kalkulacija.KalkulacijaId == 0)
        {
            _db.Kalkulacije.Add(kalkulacija);
        }
        else
        {
            _db.Kalkulacije.Update(kalkulacija);
        }

        await _db.SaveChangesAsync();
        return kalkulacija;
    }

    public async Task KnjiziKalkulacijuAsync(int kalkulacijaId)
    {
        var kalkulacija = await _db.Kalkulacije.FindAsync(kalkulacijaId);
        if (kalkulacija == null)
        {
            throw new InvalidOperationException("Kalkulacija nije pronađena.");
        }
        if (kalkulacija.IsKnjizen)
        {
            throw new InvalidOperationException($"Kalkulacija {kalkulacija.BrojKalkulacije} je već proknjižena.");
        }

        kalkulacija.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }
}

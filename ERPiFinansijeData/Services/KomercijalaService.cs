using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class KomercijalaService
{
    private readonly AccountingDbContext _db;

    public KomercijalaService(AccountingDbContext db)
    {
        _db = db;
    }

    #region Ponude i Predračuni

    public async Task<List<PonudaPredracun>> GetPonudeAsync()
    {
        return await _db.PonudePredracuni
            .Include(p => p.Stavke)
            .OrderByDescending(p => p.Datum)
            .ThenByDescending(p => p.PonudaPredracunId)
            .ToListAsync();
    }

    public async Task<PonudaPredracun?> GetPonudaByIdAsync(int id)
    {
        return await _db.PonudePredracuni
            .Include(p => p.Stavke)
            .FirstOrDefaultAsync(p => p.PonudaPredracunId == id);
    }

    public async Task<PonudaPredracun> SacuvajPonuduAsync(PonudaPredracun ponuda)
    {
        // Obračun zbirova
        ponuda.UkupnoNeto = ponuda.Stavke.Sum(s => s.IznosNeto);
        ponuda.UkupnoPdv = ponuda.Stavke.Sum(s => s.IznosPdv);
        ponuda.UkupnoBruto = ponuda.Stavke.Sum(s => s.IznosBruto);

        if (ponuda.PonudaPredracunId == 0)
        {
            if (string.IsNullOrWhiteSpace(ponuda.BrojDokumenta))
            {
                int sledeciBroj = await _db.PonudePredracuni.CountAsync() + 1;
                string prefiks = ponuda.VrstaDokumenta == "Predračun" ? "PRD" : "PON";
                ponuda.BrojDokumenta = $"{prefiks}-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.PonudePredracuni.Add(ponuda);
        }
        else
        {
            var postojeceStavke = _db.PonudeStavke.Where(s => s.PonudaPredracunId == ponuda.PonudaPredracunId);
            _db.PonudeStavke.RemoveRange(postojeceStavke);

            _db.PonudePredracuni.Update(ponuda);
        }

        await _db.SaveChangesAsync();
        return ponuda;
    }

    public async Task<bool> ObrisiPonuduAsync(int id)
    {
        var ponuda = await _db.PonudePredracuni.FindAsync(id);
        if (ponuda == null) return false;

        _db.PonudePredracuni.Remove(ponuda);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 1-Klik konverzija Ponude / Predračuna u Izlazni Račun (RacunOtpremnica) spreman za e-Fakture (SEF)
    /// </summary>
    public async Task<(bool Success, string Message, int? RacunId)> PretvoriPonuduURacunAsync(int ponudaId)
    {
        var ponuda = await GetPonudaByIdAsync(ponudaId);
        if (ponuda == null) return (false, "Ponuda ili predračun ne postoji.", null);

        if (ponuda.RacunOtpremnicaId.HasValue && ponuda.RacunOtpremnicaId.Value > 0)
        {
            return (false, $"Ponuda je već prevođena u račun br. {ponuda.RacunOtpremnicaId}.", ponuda.RacunOtpremnicaId);
        }

        int sledeciBroj = await _db.RacuniOtpremnice.CountAsync() + 1;

        var novRacun = new RacunOtpremnica
        {
            BrojRacuna = sledeciBroj,
            TipDokumenta = TipRacunOtpremnice.Racun,
            DatumRacuna = DateTime.Today,
            RokPlacanja = DateTime.Today.AddDays(15),
            PartnerId = ponuda.PartnerId,
            UkupnoOsnovica = ponuda.UkupnoNeto,
            UkupnoPdv = ponuda.UkupnoPdv,
            UkupnoZaUplatu = ponuda.UkupnoBruto,
            Napomena = $"Automatski kreirano iz {ponuda.VrstaDokumenta} br. {ponuda.BrojDokumenta}. {ponuda.Napomena}",
            SefStatus = SefStatusFakture.NijePoslata,
            IsKnjizen = false
        };

        int rbr = 1;
        foreach (var st in ponuda.Stavke)
        {
            var art = await _db.Artikli.FirstOrDefaultAsync(a => a.SifraArtikla == st.SifraArtikla);
            novRacun.Stavke.Add(new RacunOtpremnicaStavka
            {
                RedniBroj = rbr++,
                ArtikalId = art?.ArtikalId,
                Kolicina = st.Kolicina,
                ProdajnaCena = st.Cena,
                RabatProcenat = st.RabatProcenat,
                StopaPdv = st.PdvStopa,
                Osnovica = st.IznosNeto,
                IznosPdv = st.IznosPdv,
                Ukupno = st.IznosBruto
            });
        }

        _db.RacuniOtpremnice.Add(novRacun);
        await _db.SaveChangesAsync();

        ponuda.Status = "Fakturisano";
        ponuda.RacunOtpremnicaId = novRacun.RacunOtpremnicaId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno kreiran izlazni račun br. {sledeciBroj} iz {ponuda.VrstaDokumenta}!", novRacun.RacunOtpremnicaId);
    }

    #endregion

    #region Narudžbenice Dobavljačima

    public async Task<List<NarudzbenicaDobavljacu>> GetNarudzbeniceAsync()
    {
        return await _db.NarudzbeniceDobavljacima
            .Include(n => n.Stavke)
            .OrderByDescending(n => n.Datum)
            .ThenByDescending(n => n.NarudzbenicaId)
            .ToListAsync();
    }

    public async Task<NarudzbenicaDobavljacu?> GetNarudzbenicaByIdAsync(int id)
    {
        return await _db.NarudzbeniceDobavljacima
            .Include(n => n.Stavke)
            .FirstOrDefaultAsync(n => n.NarudzbenicaId == id);
    }

    public async Task<NarudzbenicaDobavljacu> SacuvajNarudzbenicuAsync(NarudzbenicaDobavljacu narudzbenica)
    {
        narudzbenica.UkupnoNeto = narudzbenica.Stavke.Sum(s => s.IznosNeto);
        narudzbenica.UkupnoPdv = narudzbenica.Stavke.Sum(s => s.IznosPdv);
        narudzbenica.UkupnoBruto = narudzbenica.Stavke.Sum(s => s.IznosBruto);

        if (narudzbenica.NarudzbenicaId == 0)
        {
            if (string.IsNullOrWhiteSpace(narudzbenica.BrojNarudzbenice))
            {
                int sledeciBroj = await _db.NarudzbeniceDobavljacima.CountAsync() + 1;
                narudzbenica.BrojNarudzbenice = $"NAR-{DateTime.Today.Year}/{sledeciBroj:D3}";
            }

            _db.NarudzbeniceDobavljacima.Add(narudzbenica);
        }
        else
        {
            var postojeceStavke = _db.NarudzbeniceStavke.Where(s => s.NarudzbenicaId == narudzbenica.NarudzbenicaId);
            _db.NarudzbeniceStavke.RemoveRange(postojeceStavke);

            _db.NarudzbeniceDobavljacima.Update(narudzbenica);
        }

        await _db.SaveChangesAsync();
        return narudzbenica;
    }

    public async Task<bool> ObrisiNarudzbenicuAsync(int id)
    {
        var narudzbenica = await _db.NarudzbeniceDobavljacima.FindAsync(id);
        if (narudzbenica == null) return false;

        _db.NarudzbeniceDobavljacima.Remove(narudzbenica);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 1-Klik konverzija Narudžbenice Dobavljaču u Ulaznu Kalkulaciju (Kalkulacija)
    /// </summary>
    public async Task<(bool Success, string Message, int? KalkulacijaId)> PretvoriNarudzbenicuUKalkulacijuAsync(int narudzbenicaId)
    {
        var narudzbenica = await GetNarudzbenicaByIdAsync(narudzbenicaId);
        if (narudzbenica == null) return (false, "Narudžbenica ne postoji.", null);

        if (narudzbenica.KalkulacijaId.HasValue && narudzbenica.KalkulacijaId.Value > 0)
        {
            return (false, $"Narudžbenica je već prevođena u kalkulaciju br. {narudzbenica.KalkulacijaId}.", narudzbenica.KalkulacijaId);
        }

        int sledeciBroj = await _db.Kalkulacije.CountAsync() + 1;

        var novKalkulacija = new Kalkulacija
        {
            BrojKalkulacije = sledeciBroj,
            Datum = DateTime.Today,
            BrojRacuna = narudzbenica.BrojNarudzbenice,
            DatumRacuna = narudzbenica.Datum,
            NabavnaVrednost = narudzbenica.UkupnoNeto,
            SvegaNabavno = narudzbenica.UkupnoNeto,
            Porez = narudzbenica.UkupnoPdv,
            IsKnjizen = false
        };

        int rbr = 1;
        foreach (var st in narudzbenica.Stavke)
        {
            novKalkulacija.Stavke.Add(new KalkulacijaStavka
            {
                RedniBroj = rbr++,
                SifraArtikla = st.SifraArtikla,
                Kolicina = st.KolicinaNarucena,
                NabavnaCena = st.Cena,
                Iznos = st.IznosNeto,
                NabavnaVrednost = st.IznosNeto
            });
        }

        _db.Kalkulacije.Add(novKalkulacija);
        await _db.SaveChangesAsync();

        narudzbenica.Status = "Završeno";
        narudzbenica.KalkulacijaId = novKalkulacija.KalkulacijaId;
        await _db.SaveChangesAsync();

        return (true, $"Uspešno kreirana ulazna kalkulacija br. {sledeciBroj} iz Narudžbenice!", novKalkulacija.KalkulacijaId);
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class BankIzvodMatchingEngine
{
    private readonly AccountingDbContext _db;

    public BankIzvodMatchingEngine(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task ProcessMatchingAsync(BankIzvod izvod)
    {
        var partneri = await _db.Partneri.ToListAsync();
        var otvoreneStavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.PartnerId.HasValue)
            .ToListAsync();

        var racuni = await _db.RacuniOtpremnice
            .Where(r => r.IsKnjizen)
            .ToListAsync();

        foreach (var s in izvod.Stavke)
        {
            MatchItem(s, partneri, otvoreneStavke, racuni);
        }
    }

    private static void MatchItem(
        BankIzvodStavka item,
        List<Partner> partneri,
        List<StavkaNaloga> otvoreneStavke,
        List<RacunOtpremnica> racuni)
    {
        // 1. Provera provizije / troška banke
        string svrhaLower = item.SvrhaDoznake.ToLowerInvariant();
        if (svrhaLower.Contains("provizija") || svrhaLower.Contains("naknada za platni") ||
            svrhaLower.Contains("trosak obrade") || svrhaLower.Contains("odrzavanje racuna") ||
            svrhaLower.Contains("kamatna naknada"))
        {
            item.SuggestedKonto = "5530"; // Troškovi platnog prometa
            item.Confidence = MatchConfidence.Exact;
            item.StatusOpis = "🏦 Provizija / trošak banke (Konto 5530)";
            return;
        }

        // Dodeljujemo podrazumevano konto po vrsti transakcije
        item.SuggestedKonto = item.Tip == BankIzvodStavkaTip.Uplata ? "2040" : "4350";

        Partner? matchedPartner = null;

        // 2. Nivo 1: Match po PIB-u
        if (!string.IsNullOrWhiteSpace(item.PibPartnera))
        {
            matchedPartner = partneri.FirstOrDefault(p => !string.IsNullOrEmpty(p.Pib) && p.Pib.Trim() == item.PibPartnera.Trim());
        }

        // Match po Žiro računu
        if (matchedPartner == null && !string.IsNullOrWhiteSpace(item.RacunPartnera))
        {
            string cleanRacun = OdistampajCleanAccount(item.RacunPartnera);
            matchedPartner = partneri.FirstOrDefault(p =>
                !string.IsNullOrEmpty(p.ZiroRacun) && OdistampajCleanAccount(p.ZiroRacun) == cleanRacun);
        }

        // Match po nazivu u svrsi doznake ili NazivPartnera
        if (matchedPartner == null)
        {
            string searchName = !string.IsNullOrWhiteSpace(item.NazivPartnera) ? item.NazivPartnera : item.SvrhaDoznake;
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                matchedPartner = partneri.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.Naziv) &&
                    (searchName.Contains(p.Naziv, StringComparison.OrdinalIgnoreCase) ||
                     p.Naziv.Contains(searchName, StringComparison.OrdinalIgnoreCase)));
            }
        }

        if (matchedPartner != null)
        {
            item.UpareniPartnerId = matchedPartner.PartnerId;
            item.UpareniPartnerNaziv = matchedPartner.Naziv;
            item.Confidence = MatchConfidence.High;
            item.StatusOpis = $"🟢 Uparen partner: {matchedPartner.Naziv} (PIB: {matchedPartner.Pib ?? "—"})";
        }

        // 3. Nivo 2: Match po Pozivu na broj / Broju dokumenta
        string searchRef = item.PozivNaBroj;
        if (string.IsNullOrWhiteSpace(searchRef))
        {
            searchRef = item.SvrhaDoznake;
        }

        if (!string.IsNullOrWhiteSpace(searchRef))
        {
            // Potraži račun-otpremnicu
            var racun = racuni.FirstOrDefault(r =>
                r.BrojRacuna > 0 && searchRef.Contains(r.BrojRacuna.ToString(), StringComparison.OrdinalIgnoreCase));

            if (racun != null)
            {
                item.UpareniDokumentBroj = racun.BrojRacuna.ToString();
                if (item.UpareniPartnerId == null)
                {
                    item.UpareniPartnerId = racun.PartnerId;
                    var p = partneri.FirstOrDefault(x => x.PartnerId == racun.PartnerId);
                    if (p != null) item.UpareniPartnerNaziv = p.Naziv;
                }
                item.Confidence = MatchConfidence.Exact;
                item.StatusOpis = $"🟢 Uparen račun-faktura br. {racun.BrojRacuna}";
                return;
            }

            // Potraži stavku naloga sa brojem dokumenta
            var st = otvoreneStavke.FirstOrDefault(s =>
                !string.IsNullOrEmpty(s.BrojDokumenta) && searchRef.Contains(s.BrojDokumenta, StringComparison.OrdinalIgnoreCase));

            if (st != null)
            {
                item.UparenaStavkaId = st.StavkaNalogaId;
                item.UpareniDokumentBroj = st.BrojDokumenta;
                if (item.UpareniPartnerId == null && st.PartnerId.HasValue)
                {
                    item.UpareniPartnerId = st.PartnerId;
                    var p = partneri.FirstOrDefault(x => x.PartnerId == st.PartnerId);
                    if (p != null) item.UpareniPartnerNaziv = p.Naziv;
                }
                item.Confidence = MatchConfidence.Exact;
                item.StatusOpis = $"🟢 Uparena stavka naloga br. {st.BrojDokumenta}";
                return;
            }
        }

        if (item.Confidence == MatchConfidence.None)
        {
            item.StatusOpis = "🔴 Neupareno — izaberite partnera ili konto ručno";
        }
    }

    private static string OdistampajCleanAccount(string acc)
    {
        return Regex.Replace(acc, @"[^\d]", "");
    }
}

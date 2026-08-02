using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class BankIzvodService
{
    private readonly AccountingDbContext _db;
    private readonly BankIzvodMatchingEngine _matchingEngine;
    private readonly ZatvaranjeStavkiService _zatvaranjeService;

    public BankIzvodService(AccountingDbContext db)
    {
        _db = db;
        _matchingEngine = new BankIzvodMatchingEngine(db);
        _zatvaranjeService = new ZatvaranjeStavkiService(db);
    }

    public async Task<BankIzvod> UcitajIIzanalizirajIzvodAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Fajl bankarskog izvoda nije pronađen.", filePath);

        string content = await File.ReadAllTextAsync(filePath);
        var format = BankIzvodFormatDetector.DetectFormat(filePath, content);
        var parser = BankIzvodParserFactory.GetParser(format);

        var izvod = parser.Parse(content);
        izvod.Format = format;

        await _matchingEngine.ProcessMatchingAsync(izvod);

        return izvod;
    }

    public async Task<Nalog> ProknjiziIzvodIZatvoriStavkeAsync(
        BankIzvod izvod,
        int korisnikId,
        string korisnickoIme)
    {
        if (izvod.Stavke == null || izvod.Stavke.Count == 0)
            throw new InvalidOperationException("Izvod ne sadrži stavke za knjiženje.");

        int sledeciBrojNaloga = 1;
        var maxNalog = await _db.Nalozi.Where(n => n.VrstaNaloga == "IZV").MaxAsync(n => (int?)n.BrojNaloga);
        if (maxNalog.HasValue) sledeciBrojNaloga = maxNalog.Value + 1;

        if (int.TryParse(izvod.BrojIzvoda, out var customBroj) && customBroj > 0)
        {
            sledeciBrojNaloga = customBroj;
        }

        decimal ukupnoDuguje = izvod.Stavke.Sum(s => s.Iznos);
        decimal ukupnoPotrazuje = izvod.Stavke.Sum(s => s.Iznos);

        var nalog = new Nalog
        {
            VrstaNaloga = "IZV",
            BrojNaloga = sledeciBrojNaloga,
            DatumNaloga = izvod.DatumIzvoda,
            Opis = $"Izvod banke br. {izvod.BrojIzvoda} od {izvod.DatumIzvoda:dd.MM.yyyy}",
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now,
            UkupnoDuguje = ukupnoDuguje,
            UkupnoPotrazuje = ukupnoPotrazuje
        };

        var noveStavkeNaloga = new List<StavkaNaloga>();
        int redniBroj = 1;

        foreach (var s in izvod.Stavke)
        {
            string kontoTekuci = "2410"; // Tekući dinarski račun
            string kontoSuprotno = string.IsNullOrWhiteSpace(s.SuggestedKonto) ? (s.Tip == BankIzvodStavkaTip.Uplata ? "2040" : "4350") : s.SuggestedKonto;

            if (s.Tip == BankIzvodStavkaTip.Uplata)
            {
                // Priliv: Duguje Tekući račun (2410), Potražuje Kupac/KontoSuprotno (2040)
                var dugujeStavka = new StavkaNaloga
                {
                    RedniBroj = redniBroj++,
                    BrojKonta = kontoTekuci,
                    Opis = string.IsNullOrWhiteSpace(s.SvrhaDoznake) ? $"Uplata br. {izvod.BrojIzvoda}" : s.SvrhaDoznake,
                    Duguje = s.Iznos,
                    Potrazuje = 0m,
                    BrojDokumenta = string.IsNullOrWhiteSpace(s.PozivNaBroj) ? izvod.BrojIzvoda : s.PozivNaBroj,
                    DatumDokumenta = s.DatumValute,
                    PartnerId = null
                };

                var potrazujeStavka = new StavkaNaloga
                {
                    RedniBroj = redniBroj++,
                    BrojKonta = kontoSuprotno,
                    Opis = string.IsNullOrWhiteSpace(s.SvrhaDoznake) ? $"Uplata br. {izvod.BrojIzvoda}" : s.SvrhaDoznake,
                    Duguje = 0m,
                    Potrazuje = s.Iznos,
                    BrojDokumenta = string.IsNullOrWhiteSpace(s.PozivNaBroj) ? izvod.BrojIzvoda : s.PozivNaBroj,
                    DatumDokumenta = s.DatumValute,
                    PartnerId = s.UpareniPartnerId
                };

                noveStavkeNaloga.Add(dugujeStavka);
                noveStavkeNaloga.Add(potrazujeStavka);
            }
            else
            {
                // Odliv: Duguje Dobavljač/Trošak (4350/5530), Potražuje Tekući račun (2410)
                var dugujeStavka = new StavkaNaloga
                {
                    RedniBroj = redniBroj++,
                    BrojKonta = kontoSuprotno,
                    Opis = string.IsNullOrWhiteSpace(s.SvrhaDoznake) ? $"Isplata br. {izvod.BrojIzvoda}" : s.SvrhaDoznake,
                    Duguje = s.Iznos,
                    Potrazuje = 0m,
                    BrojDokumenta = string.IsNullOrWhiteSpace(s.PozivNaBroj) ? izvod.BrojIzvoda : s.PozivNaBroj,
                    DatumDokumenta = s.DatumValute,
                    PartnerId = s.UpareniPartnerId
                };

                var potrazujeStavka = new StavkaNaloga
                {
                    RedniBroj = redniBroj++,
                    BrojKonta = kontoTekuci,
                    Opis = string.IsNullOrWhiteSpace(s.SvrhaDoznake) ? $"Isplata br. {izvod.BrojIzvoda}" : s.SvrhaDoznake,
                    Duguje = 0m,
                    Potrazuje = s.Iznos,
                    BrojDokumenta = string.IsNullOrWhiteSpace(s.PozivNaBroj) ? izvod.BrojIzvoda : s.PozivNaBroj,
                    DatumDokumenta = s.DatumValute,
                    PartnerId = null
                };

                noveStavkeNaloga.Add(dugujeStavka);
                noveStavkeNaloga.Add(potrazujeStavka);
            }
        }

        nalog.Stavke = noveStavkeNaloga;
        _db.Nalozi.Add(nalog);
        await _db.SaveChangesAsync();

        // Automatsko zatvaranje otvorenih stavki (IOS) za uparene partnere
        foreach (var s in izvod.Stavke)
        {
            if (s.UpareniPartnerId.HasValue && s.UpareniPartnerId > 0)
            {
                try
                {
                    var novastavka = noveStavkeNaloga.FirstOrDefault(x => x.PartnerId == s.UpareniPartnerId && (s.Tip == BankIzvodStavkaTip.Uplata ? x.Potrazuje > 0 : x.Duguje > 0));
                    if (novastavka != null)
                    {
                        var otvoreneZaPartnera = await _zatvaranjeService.GetOtvoreneStavkeZaPartneraAsync(s.UpareniPartnerId.Value, izvod.DatumIzvoda, samoOtvorene: true);
                        var suprotnaStavka = s.UparenaStavkaId.HasValue
                            ? otvoreneZaPartnera.FirstOrDefault(x => x.StavkaNalogaId == s.UparenaStavkaId.Value)
                            : otvoreneZaPartnera.FirstOrDefault(x => s.Tip == BankIzvodStavkaTip.Uplata ? x.Strana == "Duguje" : x.Strana == "Potrazuje");

                        if (suprotnaStavka != null)
                        {
                            decimal iznosZatvaranja = Math.Min(s.Iznos, suprotnaStavka.Preostalo);
                            if (iznosZatvaranja > 0.01m)
                            {
                                if (s.Tip == BankIzvodStavkaTip.Uplata)
                                {
                                    await _zatvaranjeService.ZatvoriAsync(suprotnaStavka.StavkaNalogaId, novastavka.StavkaNalogaId, iznosZatvaranja, izvod.DatumIzvoda, "BankIzvod", $"Zatvaranje po izvodu {izvod.BrojIzvoda}", korisnikId, korisnickoIme);
                                }
                                else
                                {
                                    await _zatvaranjeService.ZatvoriAsync(novastavka.StavkaNalogaId, suprotnaStavka.StavkaNalogaId, iznosZatvaranja, izvod.DatumIzvoda, "BankIzvod", $"Zatvaranje po izvodu {izvod.BrojIzvoda}", korisnikId, korisnickoIme);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignoriši ako je stavka već zatvorena ili iznos prekoračuje
                }
            }
        }

        return nalog;
    }
}

using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingData.Services;

public class DeviznoKnjigovodstvoResult
{
    public string BrojKonta { get; set; } = string.Empty;
    public string Valuta { get; set; } = "EUR";
    public decimal DevizniSaldo { get; set; }
    public decimal KnjigovodstveniSaldoRsd { get; set; }
    public decimal TekuciKurs { get; set; }
    public decimal ValviraniSaldoRsd { get; set; }
    public decimal KursnaRazlikaRsd { get; set; } // >0 Pozitivna (6630), <0 Negativna (5630)
}

public class DeviznoKnjigovodstvoService
{
    private readonly AccountingDbContext _db;

    public DeviznoKnjigovodstvoService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Izračunava kursne razlike za sve devizne konte na zadati datum po važećem NBS kursu.
    /// </summary>
    public async Task<List<DeviznoKnjigovodstvoResult>> ObracunajValviranjeAsync(DateTime naDan, decimal tekuciKursEur = 117.20m, decimal tekuciKursUsd = 108.50m)
    {
        var rezultati = new List<DeviznoKnjigovodstvoResult>();

        // Preuzimamo sve stavke sa konta koja imaju označenu valutu ili su devizna konta (204, 435, 244)
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen && s.Nalog.DatumNaloga <= naDan)
            .Where(s => s.BrojKonta.StartsWith("204") || s.BrojKonta.StartsWith("435") || s.BrojKonta.StartsWith("244") || s.Valuta != "RSD")
            .ToListAsync();

        var grupisano = stavke.GroupBy(s => new { s.BrojKonta, Valuta = string.IsNullOrEmpty(s.Valuta) ? "EUR" : s.Valuta });

        foreach (var g in grupisano)
        {
            decimal devDuguje = g.Sum(s => s.DevizniDuguje);
            decimal devPotrazuje = g.Sum(s => s.DevizniPotrazuje);
            decimal devSaldo = devDuguje - devPotrazuje;

            decimal rsdDuguje = g.Sum(s => s.Duguje);
            decimal rsdPotrazuje = g.Sum(s => s.Potrazuje);
            decimal rsdSaldo = rsdDuguje - rsdPotrazuje;

            if (devSaldo == 0 && rsdSaldo == 0) continue;

            decimal kurs = g.Key.Valuta.ToUpper() == "USD" ? tekuciKursUsd : tekuciKursEur;
            decimal valviraniRsd = Math.Round(devSaldo * kurs, 2);
            decimal razlika = valviraniRsd - rsdSaldo;

            rezultati.Add(new DeviznoKnjigovodstvoResult
            {
                BrojKonta = g.Key.BrojKonta,
                Valuta = g.Key.Valuta,
                DevizniSaldo = devSaldo,
                KnjigovodstveniSaldoRsd = rsdSaldo,
                TekuciKurs = kurs,
                ValviraniSaldoRsd = valviraniRsd,
                KursnaRazlikaRsd = razlika
            });
        }

        return rezultati;
    }

    /// <summary>
    /// Knjiži nalog valviranja deviznih konta na dan bilansa.
    /// </summary>
    public async Task<(bool Success, string Message, Nalog? Nalog)> ProknjiziValviranjeAsync(DateTime naDan, List<DeviznoKnjigovodstvoResult> stavkeValviranja)
    {
        if (stavkeValviranja == null || !stavkeValviranja.Any(s => s.KursnaRazlikaRsd != 0))
            return (false, "Nema kursnih razlika za knjiženje.", null);

        try
        {
            int sledeciBroj = (await _db.Nalozi.MaxAsync(n => (int?)n.BrojNaloga) ?? 0) + 1;

            var nalog = new Nalog
            {
                BrojNaloga = sledeciBroj,
                DatumNaloga = naDan,
                Opis = $"Automatsko valviranje deviznih konta na dan {naDan:dd.MM.yyyy}",
                IsKnjizen = true,
                VrstaNaloga = "VAL"
            };

            _db.Nalozi.Add(nalog);
            await _db.SaveChangesAsync();

            int rbr = 1;
            foreach (var st in stavkeValviranja.Where(s => s.KursnaRazlikaRsd != 0))
            {
                if (st.KursnaRazlikaRsd > 0)
                {
                    // Pozitivna kursna razlika: Konto devizni Duguje, Konto 6630 Potražuje
                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        BrojKonta = st.BrojKonta,
                        Opis = $"Pozitivna kursna razlika ({st.Valuta}) na dan {naDan:dd.MM.yyyy}",
                        Duguje = st.KursnaRazlikaRsd,
                        Potrazuje = 0m,
                        Valuta = st.Valuta,
                        KursValute = st.TekuciKurs
                    });

                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        BrojKonta = "6630", // Prihodi od kursnih razlika
                        Opis = $"Pozitivna kursna razlika ({st.Valuta}) konto {st.BrojKonta}",
                        Duguje = 0m,
                        Potrazuje = st.KursnaRazlikaRsd
                    });
                }
                else
                {
                    // Negativna kursna razlika: Konto 5630 Duguje, Konto devizni Potražuje
                    decimal absRazlika = Math.Abs(st.KursnaRazlikaRsd);

                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        BrojKonta = "5630", // Rashodi od kursnih razlika
                        Opis = $"Negativna kursna razlika ({st.Valuta}) konto {st.BrojKonta}",
                        Duguje = absRazlika,
                        Potrazuje = 0m
                    });

                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = rbr++,
                        BrojKonta = st.BrojKonta,
                        Opis = $"Negativna kursna razlika ({st.Valuta}) na dan {naDan:dd.MM.yyyy}",
                        Duguje = 0m,
                        Potrazuje = absRazlika,
                        Valuta = st.Valuta,
                        KursValute = st.TekuciKurs
                    });
                }
            }

            await _db.SaveChangesAsync();
            return (true, $"Nalog valviranja #{nalog.BrojNaloga} je uspešno sačuvan i proknjižen.", nalog);
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri knjiženju valviranja: {ex.Message}", null);
        }
    }
}

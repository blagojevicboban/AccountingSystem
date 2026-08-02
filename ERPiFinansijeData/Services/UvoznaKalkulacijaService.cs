using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class UvoznaKalkulacijaService
{
    private readonly AccountingDbContext _db;

    public UvoznaKalkulacijaService(AccountingDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Vrš raspodelu zavisnih uvoznih troškova (carina, prevoz, špedicija) na stavke uvozne kalkulacije.
    /// </summary>
    public void ProracunajUvoznuKalkulaciju(UvoznaKalkulacija kalkulacija)
    {
        if (kalkulacija == null || !kalkulacija.Stavke.Any()) return;

        // 1. Prvo izračunamo devizni i RSD iznos po stavci
        foreach (var s in kalkulacija.Stavke)
        {
            s.InoIznosDevize = Math.Round(s.Kolicina * s.InoCenaDevize, 2);
            s.InoIznosRsd = Math.Round(s.InoIznosDevize * kalkulacija.KursValute, 2);
        }

        // 2. Ino faktura u RSD po kursu
        kalkulacija.UkupnoDevize = kalkulacija.Stavke.Sum(s => s.InoIznosDevize);
        kalkulacija.UkupnoFakturaRsd = Math.Round(kalkulacija.UkupnoDevize * kalkulacija.KursValute, 2);

        decimal ukupniZavisniTroskoviRsd = kalkulacija.SpedicijaRsd + kalkulacija.PrevozRsd + kalkulacija.OstaliZavisniTroskoviRsd;
        decimal ukupnoInoRsd = kalkulacija.UkupnoFakturaRsd > 0 ? kalkulacija.UkupnoFakturaRsd : 1m;

        decimal ukupnaCarinaRsd = 0m;

        foreach (var s in kalkulacija.Stavke)
        {
            // Carina po stopi
            s.CarinaIznosRsd = Math.Round(s.InoIznosRsd * (s.CarinaProcenat / 100m), 2);
            ukupnaCarinaRsd += s.CarinaIznosRsd;

            // Proporcionalna raspodela zavisnih troškova na osnovu vrednosti u RSD
            decimal udeo = s.InoIznosRsd / ukupnoInoRsd;
            s.RasporedjeniZavisniTroskoviRsd = Math.Round(ukupniZavisniTroskoviRsd * udeo, 2);

            s.UkupnaNabavnaVrednostRsd = s.InoIznosRsd + s.CarinaIznosRsd + s.RasporedjeniZavisniTroskoviRsd;
            s.NabavnaCenaPoJediniciRsd = s.Kolicina > 0 ? Math.Round(s.UkupnaNabavnaVrednostRsd / s.Kolicina, 4) : 0m;
        }

        kalkulacija.CarinaRsd = ukupnaCarinaRsd;
        kalkulacija.UkupnaNabavnaVrednostRsd = kalkulacija.UkupnoFakturaRsd + kalkulacija.CarinaRsd + ukupniZavisniTroskoviRsd;
    }

    /// <summary>
    /// Snima uvoznu kalkulaciju i knjiži ulaz u magacin i glavnu knjigu.
    /// </summary>
    public async Task<(bool Success, string Message, UvoznaKalkulacija? Kalkulacija)> SacuvajIKnjiziUvozAsync(UvoznaKalkulacija kalkulacija)
    {
        ProracunajUvoznuKalkulaciju(kalkulacija);

        try
        {
            if (kalkulacija.UvoznaKalkulacijaId == 0)
            {
                _db.UvozneKalkulacije.Add(kalkulacija);
            }

            await _db.SaveChangesAsync();

            // Automatsko knjiženje naloga u glavnu knjigu ako već nije proknjižen
            if (!kalkulacija.IsKnjizeno)
            {
                int sledeciBroj = (await _db.Nalozi.MaxAsync(n => (int?)n.BrojNaloga) ?? 0) + 1;

                var nalog = new Nalog
                {
                    BrojNaloga = sledeciBroj,
                    DatumNaloga = kalkulacija.DatumKalkulacije,
                    Opis = $"Knjiženje uvozne kalkulacije #{kalkulacija.BrojKalkulacije} (Ino faktura {kalkulacija.InoBrojFakture})",
                    IsKnjizen = true,
                    VrstaNaloga = "UVOZ"
                };

                _db.Nalozi.Add(nalog);
                await _db.SaveChangesAsync();

                // 1. Konto 1300 / 1010 (Nabavna vrednost robe/materijala iz uvoza) Duguje
                _db.StavkeNaloga.Add(new StavkaNaloga
                {
                    NalogId = nalog.NalogId,
                    RedniBroj = 1,
                    BrojKonta = "1300",
                    Opis = $"Uvoz robe po kalkulaciji #{kalkulacija.BrojKalkulacije}",
                    Duguje = kalkulacija.UkupnaNabavnaVrednostRsd,
                    Potrazuje = 0m
                });

                // 2. Konto 4350 (Ino-dobavljač) Potražuje u devizama i RSD
                _db.StavkeNaloga.Add(new StavkaNaloga
                {
                    NalogId = nalog.NalogId,
                    RedniBroj = 2,
                    BrojKonta = "4350",
                    Opis = $"Ino faktura #{kalkulacija.InoBrojFakture}",
                    Duguje = 0m,
                    Potrazuje = kalkulacija.UkupnoFakturaRsd,
                    Valuta = kalkulacija.Valuta,
                    KursValute = kalkulacija.KursValute,
                    DevizniPotrazuje = kalkulacija.UkupnoDevize,
                    PartnerId = kalkulacija.InoPartnerId
                });

                // 3. Obaveze za carinu i zavisne troškove (Konto 4330 / 4890) Potražuje
                decimal zavisniTroskoviUkupno = kalkulacija.CarinaRsd + kalkulacija.SpedicijaRsd + kalkulacija.PrevozRsd + kalkulacija.OstaliZavisniTroskoviRsd;
                if (zavisniTroskoviUkupno > 0)
                {
                    _db.StavkeNaloga.Add(new StavkaNaloga
                    {
                        NalogId = nalog.NalogId,
                        RedniBroj = 3,
                        BrojKonta = "4330",
                        Opis = $"Zavisni troškovi uvoza (Carina, Prevoz, Špedicija) - Kalkulacija #{kalkulacija.BrojKalkulacije}",
                        Duguje = 0m,
                        Potrazuje = zavisniTroskoviUkupno
                    });
                }

                kalkulacija.IsKnjizeno = true;
                await _db.SaveChangesAsync();
            }

            return (true, $"Uvozna kalkulacija #{kalkulacija.BrojKalkulacije} je uspešno sačuvana i proknjižena.", kalkulacija);
        }
        catch (Exception ex)
        {
            return (false, $"Greška pri knjiženju uvozne kalkulacije: {ex.Message}", null);
        }
    }
}

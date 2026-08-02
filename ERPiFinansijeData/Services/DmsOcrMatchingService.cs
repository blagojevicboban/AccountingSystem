using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class DmsOcrMatchingService
{
    private readonly AccountingDbContext _db;

    public DmsOcrMatchingService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task ProcessOcrMatchingAsync(OcrRacunResult ocr)
    {
        if (!string.IsNullOrWhiteSpace(ocr.PibDobavljaca))
        {
            var pibClean = ocr.PibDobavljaca.Trim();
            var partner = await _db.Partneri.FirstOrDefaultAsync(p => p.Pib != null && p.Pib.Trim() == pibClean);
            if (partner != null)
            {
                ocr.UpareniPartnerId = partner.PartnerId;
                ocr.UpareniPartnerNaziv = partner.Naziv;
                if (string.IsNullOrWhiteSpace(ocr.NazivDobavljaca))
                {
                    ocr.NazivDobavljaca = partner.Naziv;
                }
                ocr.Confidence = OcrMatchConfidence.Exact;
                ocr.StatusPoruka = $"🟢 Uparen partner po PIB-u: {partner.Naziv} (PIB: {partner.Pib})";
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(ocr.NazivDobavljaca))
        {
            var searchName = ocr.NazivDobavljaca.Trim();
            var partner = await _db.Partneri.FirstOrDefaultAsync(p => p.Naziv != null &&
                (p.Naziv.Contains(searchName, StringComparison.OrdinalIgnoreCase) || searchName.Contains(p.Naziv, StringComparison.OrdinalIgnoreCase)));

            if (partner != null)
            {
                ocr.UpareniPartnerId = partner.PartnerId;
                ocr.UpareniPartnerNaziv = partner.Naziv;
                ocr.Confidence = OcrMatchConfidence.High;
                ocr.StatusPoruka = $"🟢 Uparen partner po nazivu: {partner.Naziv}";
            }
        }
    }

    public static List<StavkaNaloga> GenerisiStavkeNalogaZaUlazniRacun(OcrRacunResult ocr)
    {
        var stavke = new List<StavkaNaloga>();
        int redni = 1;

        string brojDok = string.IsNullOrWhiteSpace(ocr.BrojRacuna) ? "UL-RAC" : ocr.BrojRacuna;
        DateTime datumDok = ocr.DatumRacuna ?? DateTime.Today;
        DateTime valutaDok = ocr.ValutaDospela ?? datumDok.AddDays(15);
        string opisDoznake = $"Ulazni račun br. {brojDok} od {datumDok:dd.MM.yyyy}";

        // 1. Duguje Konto 5010 ili 5390 (Osnovica / Neto)
        if (ocr.OsnovicaNeto > 0)
        {
            stavke.Add(new StavkaNaloga
            {
                RedniBroj = redni++,
                BrojKonta = "5010", // Nabavna vrednost robe/materijala ili 5390 usluge
                Opis = opisDoznake,
                Duguje = ocr.OsnovicaNeto,
                Potrazuje = 0m,
                BrojDokumenta = brojDok,
                DatumDokumenta = datumDok,
                ValutaDospela = valutaDok,
                PartnerId = null
            });
        }

        // 2. Duguje Konto 2700 (Ulazni PDV 20%)
        if (ocr.PdvIznos > 0)
        {
            stavke.Add(new StavkaNaloga
            {
                RedniBroj = redni++,
                BrojKonta = "2700", // Prethodni PDV po opštoj stopi
                Opis = $"Prethodni PDV po računu br. {brojDok}",
                Duguje = ocr.PdvIznos,
                Potrazuje = 0m,
                BrojDokumenta = brojDok,
                DatumDokumenta = datumDok,
                ValutaDospela = valutaDok,
                PartnerId = null
            });
        }

        // 3. Potražuje Konto 4350 (Dobavljači u zemlji — Bruto obaveza)
        decimal ukupanBruto = ocr.UkupanIznosBruto > 0 ? ocr.UkupanIznosBruto : (ocr.OsnovicaNeto + ocr.PdvIznos);
        if (ukupanBruto > 0)
        {
            stavke.Add(new StavkaNaloga
            {
                RedniBroj = redni++,
                BrojKonta = "4350", // Dobavljači u zemlji
                Opis = opisDoznake,
                Duguje = 0m,
                Potrazuje = ukupanBruto,
                BrojDokumenta = brojDok,
                DatumDokumenta = datumDok,
                ValutaDospela = valutaDok,
                PartnerId = ocr.UpareniPartnerId
            });
        }

        return stavke;
    }
}

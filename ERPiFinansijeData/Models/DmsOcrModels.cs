using System;

namespace ERPiFinansijeData.Models;

public enum OcrMatchConfidence
{
    Exact,  // 100% — Tačan PIB u bazi i svi iznosi validirani (Osnovica + PDV == Ukupno)
    High,   // 80% — Uparen partner po PIB-u ili nazivu, iznosi pročitani
    Medium, // 50% — Iznosi izvučeni, ali partner nije sigurno prepoznat
    Low,    // 20% — Delimično prepoznati podaci
    None    // 0% — Tekst se ne može strukturno parsirati
}

public class OcrRacunResult
{
    public string PibDobavljaca { get; set; } = string.Empty;
    public string NazivDobavljaca { get; set; } = string.Empty;
    public string BrojRacuna { get; set; } = string.Empty;

    public DateTime? DatumRacuna { get; set; } = DateTime.Today;
    public DateTime? ValutaDospela { get; set; } = DateTime.Today.AddDays(15);

    public decimal OsnovicaNeto { get; set; }
    public decimal PdvIznos { get; set; }
    public decimal PdvStopa { get; set; } = 20.0m;
    public decimal UkupanIznosBruto { get; set; }

    public int? UpareniPartnerId { get; set; }
    public string? UpareniPartnerNaziv { get; set; }

    public OcrMatchConfidence Confidence { get; set; } = OcrMatchConfidence.None;
    public string StatusPoruka { get; set; } = "Neobrađeno";
    public string RawText { get; set; } = string.Empty;
}

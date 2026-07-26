namespace AccountingData.Models;

public enum TipPdvKnjige
{
    KIR_IzdatRacun,
    KPR_PrimljenRacun
}

public class PdvZapis
{
    public int PdvZapisId { get; set; }
    public TipPdvKnjige TipKnjige { get; set; }
    public int RedniBroj { get; set; }
    public DateTime DatumRacuna { get; set; }
    public DateTime DatumKnjizenja { get; set; }
    public string BrojDokumenta { get; set; } = string.Empty;
    public string PartnerNaziv { get; set; } = string.Empty;
    public string PartnerPib { get; set; } = string.Empty;

    public decimal UkupnaNaknadaSaPdv { get; set; }
    public decimal Osnovica20 { get; set; }
    public decimal Pdv20 { get; set; }
    public decimal Osnovica10 { get; set; }
    public decimal Pdv10 { get; set; }
    public decimal OslobodjenPromet { get; set; }

    public int? IzvornoDokumentId { get; set; }
}

public class PdvObracunResult
{
    public DateTime OdDatuma { get; set; }
    public DateTime DoDatuma { get; set; }

    // KIR
    public decimal KirUkupnoSaPdv { get; set; }
    public decimal KirOsnovica20 { get; set; }
    public decimal KirPdv20 { get; set; }
    public decimal KirOsnovica10 { get; set; }
    public decimal KirPdv10 { get; set; }
    public decimal KirOslobodjen { get; set; }
    public decimal KirUkupanPdv => KirPdv20 + KirPdv10;

    // KPR
    public decimal KprUkupnoSaPdv { get; set; }
    public decimal KprOsnovica20 { get; set; }
    public decimal KprPdv20 { get; set; }
    public decimal KprOsnovica10 { get; set; }
    public decimal KprPdv10 { get; set; }
    public decimal KprOslobodjen { get; set; }
    public decimal KprUkupanPdv => KprPdv20 + KprPdv10;

    // Razlika
    public decimal PdvRazlika => KirUkupanPdv - KprUkupanPdv; // > 0 obaveza za uplatu, < 0 povraćaj
}

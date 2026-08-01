using System;
using System.Collections.Generic;

namespace AccountingData.Models;

public enum BankIzvodFormat
{
    HalcomXml,
    AssecoXml,
    Camt053Xml,
    Mt940Txt,
    Nepoznato
}

public enum MatchConfidence
{
    Exact,   // 100% — Upareno po PIB-u / žiro računu i tačnom pozivu na broj / iznosu
    High,    // 80% — Uparen partner po PIB-u/žiro računu, ali nema specifične fakture
    Medium,  // 50% — Delimično uparen partner po nazivu
    None     // 0% — Neupareno
}

public enum BankIzvodStavkaTip
{
    Uplata,  // Priliv na tekući račun (Kreditor = Kupac / Partner)
    Isplata  // Odliv sa tekućeg računa (Debitor = Dobavljač / Trošak)
}

public class BankIzvod
{
    public string BrojIzvoda { get; set; } = string.Empty;
    public DateTime DatumIzvoda { get; set; } = DateTime.Today;
    public string BrojRacuna { get; set; } = string.Empty;
    public decimal PocetnoStanje { get; set; }
    public decimal KrajnjeStanje { get; set; }
    public decimal UkupnoUplata { get; set; }
    public decimal UkupnoIsplata { get; set; }
    public BankIzvodFormat Format { get; set; } = BankIzvodFormat.Nepoznato;

    public List<BankIzvodStavka> Stavke { get; set; } = new();
}

public class BankIzvodStavka
{
    public int BrojStavke { get; set; }
    public DateTime DatumValute { get; set; } = DateTime.Today;
    public string SvrhaDoznake { get; set; } = string.Empty;
    public decimal Iznos { get; set; }
    public BankIzvodStavkaTip Tip { get; set; } = BankIzvodStavkaTip.Uplata;

    public string RacunPartnera { get; set; } = string.Empty;
    public string NazivPartnera { get; set; } = string.Empty;
    public string PibPartnera { get; set; } = string.Empty;
    public string PozivNaBroj { get; set; } = string.Empty;

    // Upareni podaci iz baze
    public int? UpareniPartnerId { get; set; }
    public string? UpareniPartnerNaziv { get; set; }
    public int? UparenaStavkaId { get; set; }
    public string? UpareniDokumentBroj { get; set; }

    // Preporučeno konto za knjiženje (npr. "2040", "4350", "5530")
    public string SuggestedKonto { get; set; } = "2040";

    public MatchConfidence Confidence { get; set; } = MatchConfidence.None;
    public string StatusOpis { get; set; } = "Neupareno";
}

using System;
using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

public enum TipMestaTroska
{
    MestoTroska = 0,   // Npr. Poslovna jedinica Beograd, Uprava, Oddeđenje prodaje
    Projekat = 1,      // Npr. Projekat Izgradnja Objekta A, IT Razvoj ERP
    Objekat = 2,       // Npr. Maloprodajni objekat Niš, Magacin Zemun
    PoslovnaJedinica = 3
}

public class MestoTroska
{
    [Key]
    public int MestoTroskaId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Sifra { get; set; } = string.Empty; // Npr. MT-01, PRJ-2026-A

    [Required]
    [MaxLength(100)]
    public string Naziv { get; set; } = string.Empty; // Npr. Gradilište Novi Sad

    public TipMestaTroska Tip { get; set; } = TipMestaTroska.MestoTroska;

    public bool IsAktivno { get; set; } = true;
    public string Napomena { get; set; } = string.Empty;
}

public class MestoTroskaAnalitikaRed
{
    public string BrojKonta { get; set; } = string.Empty;
    public string NazivKonta { get; set; } = string.Empty;
    public decimal UkupnoDuguje { get; set; }
    public decimal UkupnoPotrazuje { get; set; }
    public decimal Saldo => UkupnoDuguje - UkupnoPotrazuje;
}

public class MestoTroskaProfitabilnostSummary
{
    public decimal UkupnoPrihodi { get; set; }   // Zbir na Kontu 6xx (Potražuje)
    public decimal UkupnoRashodi { get; set; }   // Zbir na Kontu 5xx (Duguje)
    public decimal NetoRezultat => UkupnoPrihodi - UkupnoRashodi; // Profitabilnost projekta / mesta troška
}

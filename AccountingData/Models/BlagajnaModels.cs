using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingData.Models;

public enum VrstaBlagajne
{
    Dinarska = 0, // Konto 2430
    Devizna = 1   // Konto 2440
}

public enum VrstaBlagajnickogNaloga
{
    Uplata = 0, // Nalog za uplatu u blagajnu (Duguje 2430/2440)
    Isplata = 1 // Nalog za isplatu iz blagajne (Potražuje 2430/2440)
}

public class BlagajnickiNalog
{
    [Key]
    public int BlagajnickiNalogId { get; set; }

    public string BrojNaloga { get; set; } = string.Empty; // Npr. BLU-2026/001 ili BLI-2026/001
    public VrstaBlagajne VrstaBlagajne { get; set; } = VrstaBlagajne.Dinarska;
    public VrstaBlagajnickogNaloga VrstaNaloga { get; set; } = VrstaBlagajnickogNaloga.Uplata;

    public DateTime Datum { get; set; } = DateTime.Today;

    public string UplatilacIsplatilac { get; set; } = string.Empty; // Kome ili od koga
    public string Svrha { get; set; } = string.Empty; // Npr. Podizanje gotovine sa tekućeg računa, Isplata akontacije za putni nalog

    public string BrojKontaProtu { get; set; } = "2410"; // Protivkonto (2410 tekući račun, 4350 dobavljači, 5330 putni troškovi, 4650 zaposleni)

    public decimal Iznos { get; set; }

    public string Status { get; set; } = "Nacrt"; // Nacrt, Proknjiženo
    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? NalogId { get; set; } // Povezani nalog u Glavnoj Knjizi
    public bool IsKnjizeno { get; set; }
}

public class BlagajnickiDnevnikRed
{
    public int BlagajnickiNalogId { get; set; }
    public string BrojNaloga { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string Vrsta { get; set; } = "Uplata"; // Uplata / Isplata
    public string UplatilacIsplatilac { get; set; } = string.Empty;
    public string Svrha { get; set; } = string.Empty;
    public string BrojKontaProtu { get; set; } = string.Empty;

    public decimal Uplata { get; set; }
    public decimal Isplata { get; set; }
    public decimal Saldo { get; set; }
}

public class BlagajnickiDnevnikSummary
{
    public decimal PocetnoStanje { get; set; }
    public decimal UkupnoUplata { get; set; }
    public decimal UkupnoIsplata { get; set; }
    public decimal KrajnjeStanje => PocetnoStanje + UkupnoUplata - UkupnoIsplata;
}

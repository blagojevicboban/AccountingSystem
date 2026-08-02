using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

public class PonudaPredracun
{
    [Key]
    public int PonudaPredracunId { get; set; }

    public string BrojDokumenta { get; set; } = string.Empty; // Npr. PON-2026/001 ili PRD-2026/001
    public string VrstaDokumenta { get; set; } = "Ponuda";    // "Ponuda" ili "Predračun"

    public DateTime Datum { get; set; } = DateTime.Today;
    public DateTime RokVazenja { get; set; } = DateTime.Today.AddDays(15);

    public int? PartnerId { get; set; }
    public string NazivPartnera { get; set; } = string.Empty;

    public string Status { get; set; } = "Nacrt"; // Nacrt, Poslato, Prihvaćeno, Fakturisano, Odbijeno

    public decimal UkupnoNeto { get; set; }
    public decimal UkupnoPdv { get; set; }
    public decimal UkupnoBruto { get; set; }

    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? RacunOtpremnicaId { get; set; } // Link ka kreiranom izlaznom računu

    public List<PonudaStavka> Stavke { get; set; } = new();
}

public class PonudaStavka
{
    [Key]
    public int PonudaStavkaId { get; set; }

    public int PonudaPredracunId { get; set; }
    public PonudaPredracun? PonudaPredracun { get; set; }

    public int RedniBroj { get; set; }
    public string SifraArtikla { get; set; } = string.Empty;
    public string NazivArtikla { get; set; } = string.Empty;
    public string JedinicaMere { get; set; } = "kom";

    public decimal Kolicina { get; set; } = 1.0m;
    public decimal Cena { get; set; }
    public decimal RabatProcenat { get; set; }
    public decimal PdvStopa { get; set; } = 20.0m;

    public decimal IznosNeto { get; set; }
    public decimal IznosPdv { get; set; }
    public decimal IznosBruto { get; set; }
}

public class NarudzbenicaDobavljacu
{
    [Key]
    public int NarudzbenicaId { get; set; }

    public string BrojNarudzbenice { get; set; } = string.Empty; // Npr. NAR-2026/001

    public DateTime Datum { get; set; } = DateTime.Today;
    public DateTime? RokIsporuke { get; set; } = DateTime.Today.AddDays(7);

    public int? PartnerId { get; set; }
    public string NazivDobavljaca { get; set; } = string.Empty;

    public string Status { get; set; } = "Naručeno"; // Naručeno, Delimično, Završeno, Otkazano

    public decimal UkupnoNeto { get; set; }
    public decimal UkupnoPdv { get; set; }
    public decimal UkupnoBruto { get; set; }

    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? KalkulacijaId { get; set; } // Link ka kreiranoj ulaznoj kalkulaciji

    public List<NarudzbenicaStavka> Stavke { get; set; } = new();
}

public class NarudzbenicaStavka
{
    [Key]
    public int NarudzbenicaStavkaId { get; set; }

    public int NarudzbenicaId { get; set; }
    public NarudzbenicaDobavljacu? NarudzbenicaDobavljacu { get; set; }

    public int RedniBroj { get; set; }
    public string SifraArtikla { get; set; } = string.Empty;
    public string NazivArtikla { get; set; } = string.Empty;
    public string JedinicaMere { get; set; } = "kom";

    public decimal KolicinaNarucena { get; set; } = 1.0m;
    public decimal KolicinaPristigla { get; set; } = 0.0m;
    public decimal Cena { get; set; }
    public decimal PdvStopa { get; set; } = 20.0m;

    public decimal IznosNeto { get; set; }
    public decimal IznosPdv { get; set; }
    public decimal IznosBruto { get; set; }
}

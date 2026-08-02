using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

public enum VrstaKompenzacije
{
    Dvojna = 0,   // Između 2 partnera (Kupac ⟷ Dobavljač)
    Asignacija = 1,// Ugovor o asignaciji (3 lica: Asignant, Asignat, Asignatar)
    Cesija = 2     // Ugovor o cesiji (3 lica: Cedent, Cesionar, Cesijat)
}

public class Kompenzacija
{
    [Key]
    public int KompenzacijaId { get; set; }

    public string BrojDokumenta { get; set; } = string.Empty; // Npr. KOM-2026/001
    public VrstaKompenzacije Vrsta { get; set; } = VrstaKompenzacije.Dvojna;

    public DateTime Datum { get; set; } = DateTime.Today;

    public int? PartnerId { get; set; }
    public string NazivPartnera { get; set; } = string.Empty;

    // Za Asignaciju / Cesiju (Trojna kompenzacija)
    public int? Partner2Id { get; set; }
    public string? NazivPartnera2 { get; set; }

    public int? Partner3Id { get; set; }
    public string? NazivPartnera3 { get; set; }

    public decimal UkupanIznosKompenzacije { get; set; }

    public string Status { get; set; } = "Nacrt"; // Nacrt, Potvrđeno, Proknjiženo

    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? NalogId { get; set; } // Povezani nalog u Glavnoj Knjizi
    public bool IsKnjizeno { get; set; }

    public List<KompenzacijaStavka> Stavke { get; set; } = new();
}

public class KompenzacijaStavka
{
    [Key]
    public int KompenzacijaStavkaId { get; set; }

    public int KompenzacijaId { get; set; }
    public Kompenzacija? Kompenzacija { get; set; }

    public int RedniBroj { get; set; }
    public int StavkaNalogaId { get; set; } // Povezana otvorena stavka u GK (Konto 2040 ili 4350)

    public string BrojDokumenta { get; set; } = string.Empty;
    public DateTime DatumDokumenta { get; set; } = DateTime.Today;

    public string Strana { get; set; } = "Duguje"; // "Duguje" (Potraživanje od kupca) ili "Potražuje" (Obaveza prema dobavljaču)
    public string BrojKonta { get; set; } = "2040";

    public decimal IznosFakture { get; set; }
    public decimal IznosPreostalo { get; set; }
    public decimal IznosZaKompenzaciju { get; set; }
}

public class ObostranoDugovanjeCandidate
{
    public int PartnerId { get; set; }
    public string NazivPartnera { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;

    public decimal PotrazivanjeKupac { get; set; } // Saldo na Kontu 2040 (Nama duguje kupac)
    public decimal ObavezaDobavljac { get; set; }  // Saldo na Kontu 4350 (Mi dugujemo dobavljaču)

    public decimal MaksimalnaKompenzacija => Math.Min(PotrazivanjeKupac, ObavezaDobavljac);
}

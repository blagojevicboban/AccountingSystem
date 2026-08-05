using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

public enum VrstaSlužbenogPutovanja
{
    Zemlja = 0,       // U zemlji (Konto 5330)
    Inostranstvo = 1  // U inostranstvu (Konto 5340)
}

public class PutniNalog
{
    [Key]
    public int PutniNalogId { get; set; }

    public string BrojNaloga { get; set; } = string.Empty; // Npr. PN-2026/001
    public VrstaSlužbenogPutovanja Vrsta { get; set; } = VrstaSlužbenogPutovanja.Zemlja;

    public string ZaposleniIme { get; set; } = string.Empty;
    public string RadnoMesto { get; set; } = string.Empty;

    /// <summary>
    /// JMBG radnika (Faza 3.2 — prenos oporezivog dela dnevnice u ERPiZarade). Slobodan tekst,
    /// bez stranog ključa: ovaj program nema svoj registar zaposlenih, ERPiZarade ga ima.
    /// Knjigovođa ga prepisuje iz kartona pri unosu naloga; ERPiZarade njime pri uvozu upari
    /// nalog sa tačnim radnikom.
    /// </summary>
    [MaxLength(13)]
    public string Jmbg { get; set; } = string.Empty;

    public string Relacija { get; set; } = string.Empty; // Npr. Beograd — Novi Sad — Beograd
    public string SvrhaPutovanja { get; set; } = string.Empty;
    public string PrevoznoSredstvo { get; set; } = "Službeno vozilo"; // Službeno vozilo, Privatno vozilo, Autobus, Avion

    public DateTime DatumPolaska { get; set; } = DateTime.Now;
    public DateTime DatumPovratka { get; set; } = DateTime.Now.AddDays(1);

    public double TrajanjeSati { get; set; }
    public decimal BrojDnevnica { get; set; }
    public decimal IznosDnevniceRsd { get; set; } = 3000m; // Podrazumevana neoporeziva dnevnica u zemlji

    public decimal UkupnoDnevnice { get; set; }
    public decimal TroskoviGoriva { get; set; }
    public decimal TroskoviSmestaja { get; set; }
    public decimal TroskoviPrevoza { get; set; }
    public decimal OstaliTroskovi { get; set; }

    public decimal Akontacija { get; set; }
    public decimal UkupnoZaIsplatu { get; set; }

    public string Status { get; set; } = "Nacrt"; // Nacrt, Obračunato, Proknjiženo
    public string Napomena { get; set; } = string.Empty;
    public string Korisnik { get; set; } = string.Empty;

    public int? NalogId { get; set; } // Povezani nalog u Glavnoj Knjizi
    public bool IsKnjizeno { get; set; }

    public List<PutniNalogTrosakStavka> StavkeTroskova { get; set; } = new();
}

public class PutniNalogTrosakStavka
{
    [Key]
    public int PutniNalogTrosakStavkaId { get; set; }

    public int PutniNalogId { get; set; }
    public PutniNalog? PutniNalog { get; set; }

    public int RedniBroj { get; set; }
    public string VrstaTroska { get; set; } = "Gorivo"; // Gorivo, Smeštaj, Prevoz, Putarina, Taksiji, Ostalo
    public string BrojRacuna { get; set; } = string.Empty;
    public DateTime DatumRacuna { get; set; } = DateTime.Today;
    public decimal Iznos { get; set; }
    public string Opis { get; set; } = string.Empty;
}

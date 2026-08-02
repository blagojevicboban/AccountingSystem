using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

/// <summary>
/// Šifarnik opisa promena (legacy PROMENE.DBF), po firmi — šifre i njihovo značenje
/// se razlikuju od firme do firme (npr. šifra 23 je "KALKU." u KOR02 a "NIVELACIJE" u
/// KOR03), pa se ne mogu tretirati kao deljeni statički rečnik. Koristi se za dekodiranje
/// <see cref="StavkaNaloga.PromenaKod"/> (legacy relacioni spoj NALOG.PROMENA = PROMENE.SIFRA).
/// </summary>
public class Promena
{
    [Key]
    public int PromenaId { get; set; }

    public int Sifra { get; set; }

    [Required]
    [MaxLength(35)]
    public string Opis { get; set; } = string.Empty;
}

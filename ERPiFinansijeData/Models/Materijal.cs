using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

/// <summary>Šifarnik materijala (M_SIFR.DBF) — nezavisna šifarnička serija od Artikal/ARTIKLI.DBF (Robno).</summary>
public class Materijal
{
    [Key]
    public int MaterijalId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraArtikla { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(20)]
    public string JedinicaMere { get; set; } = "kom";

    [MaxLength(50)]
    public string? Pakovanje { get; set; }
}

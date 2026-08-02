using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

public class Partner
{
    [Key]
    public int PartnerId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraPartnera { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Adresa { get; set; }

    [MaxLength(100)]
    public string? PttIMesto { get; set; }

    [MaxLength(30)]
    public string? Pib { get; set; }

    [MaxLength(30)]
    public string? MaticniBroj { get; set; }

    [MaxLength(50)]
    public string? Telefon { get; set; }

    [MaxLength(50)]
    public string? ZiroRacun { get; set; }

    [MaxLength(20)]
    public string? KontoPartnera { get; set; }
}

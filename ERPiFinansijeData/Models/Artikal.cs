using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

public class Artikal
{
    [Key]
    public int ArtikalId { get; set; }

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

    [MaxLength(20)]
    public string? TarifniBroj { get; set; }

    [MaxLength(50)]
    public string? Barkod { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCena { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    [MaxLength(20)]
    public string? KlasifikacionaSifra { get; set; }

    public bool Selektovan { get; set; }
}

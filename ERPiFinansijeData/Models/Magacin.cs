using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

public class Magacin
{
    [Key]
    public int MagacinId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraMagacina { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string NazivMagacina { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? OdgovornoLice { get; set; }

    [MaxLength(30)]
    public string VrstaMagacina { get; set; } = "Veleprodaja";

    /// <summary>
    /// "šifra - naziv" za padajuće liste. Šifra je prva jer se magacin u dokumentima
    /// (MAG_PRIMA / MAG_DAJE) vodi po šifri, pa je ona ta koja se poredi sa papirom.
    /// </summary>
    [NotMapped]
    public string Prikaz => string.IsNullOrWhiteSpace(SifraMagacina)
        ? NazivMagacina
        : $"{SifraMagacina} - {NazivMagacina}";
}

using System.ComponentModel.DataAnnotations;

namespace AccountingData.Models;

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
}

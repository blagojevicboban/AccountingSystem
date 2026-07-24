using System.ComponentModel.DataAnnotations;

namespace AccountingData.Models;

public class Konto
{
    [Key]
    public int KontoId { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojKonta { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NazivKonta { get; set; } = string.Empty;

    [MaxLength(50)]
    public string VrstaKonta { get; set; } = "Aktivna";

    public bool IsSintetika { get; set; }
    public int Klasa { get; set; }
}

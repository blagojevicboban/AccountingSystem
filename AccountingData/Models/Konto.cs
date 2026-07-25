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

    [MaxLength(20)]
    public string? StariKonto { get; set; }

    [MaxLength(50)]
    public string? Ulica { get; set; }

    [MaxLength(50)]
    public string? Mesto { get; set; }

    [MaxLength(50)]
    public string? ZiroRacun { get; set; }

    [MaxLength(50)]
    public string? Telefon { get; set; }
}

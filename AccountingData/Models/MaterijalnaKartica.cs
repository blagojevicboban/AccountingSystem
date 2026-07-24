using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class MaterijalnaKartica
{
    [Key]
    public int MaterijalnaKarticaId { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraMagacina { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string SifraArtikla { get; set; } = string.Empty;

    public int RedniBroj { get; set; }
    public DateTime DatumPromene { get; set; }

    [MaxLength(50)]
    public string? OpisPromene { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Ulaz { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Izlaz { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Stanje { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Cena { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal CenaIzlaz { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Duguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Potrazuje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Saldo { get; set; }
}

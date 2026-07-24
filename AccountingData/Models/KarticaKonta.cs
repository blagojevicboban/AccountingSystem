using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

/// <summary>
/// Snimljen (legacy) red kartice konta iz KARTICA.DBF — istorijski trag za
/// poređenje sa novim, u aplikaciji računatim karticama (Faza 1).
/// </summary>
public class KarticaKonta
{
    [Key]
    public int KarticaKontaId { get; set; }

    public int RedniBroj { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojKonta { get; set; } = string.Empty;

    public DateTime DatumNaloga { get; set; }

    [MaxLength(20)]
    public string? BrojNaloga { get; set; }

    [MaxLength(10)]
    public string? OpisPromeneKod { get; set; }

    [MaxLength(50)]
    public string? BrojDokumenta { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TekuceDuguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TekucePotrazuje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoDuguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPotrazuje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Saldo { get; set; }
}

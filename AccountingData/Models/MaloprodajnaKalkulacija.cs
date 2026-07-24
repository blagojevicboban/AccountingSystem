using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class MaloprodajnaKalkulacija
{
    [Key]
    public int MaloprodajnaKalkulacijaId { get; set; }

    public int SifraProdavnice { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojKalkulacije { get; set; } = string.Empty;

    public DateTime Datum { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string? SifraMagacinaPrima { get; set; }

    [MaxLength(20)]
    public string? SifraMagacinaDaje { get; set; }

    [MaxLength(20)]
    public string? SifraDobavljaca { get; set; }

    [MaxLength(30)]
    public string? BrojOtpremnice { get; set; }
    public DateTime? DatumOtpremnice { get; set; }

    [MaxLength(30)]
    public string? BrojRacuna { get; set; }
    public DateTime? DatumRacuna { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TransportniTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TroskoviUskladistenja { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UtovarIstovar { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal TransportnoOsiguranje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OstaliTroskovi { get; set; }

    public bool IsKnjizen { get; set; }
    public bool IsTrgovinskiKnjizen { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal RabatPri { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaNabavno { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Razlika { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Porez { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RabatIznos { get; set; }
}

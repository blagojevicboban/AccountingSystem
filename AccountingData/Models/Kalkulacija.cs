using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class Kalkulacija
{
    [Key]
    public int KalkulacijaId { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojKalkulacije { get; set; } = string.Empty;

    public DateTime Datum { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string? SifraDobavljaca { get; set; }

    [MaxLength(30)]
    public string? BrojOtpremnice { get; set; }
    public DateTime? DatumOtpremnice { get; set; }

    [MaxLength(30)]
    public string? BrojRacuna { get; set; }
    public DateTime? DatumRacuna { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

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

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaTroskovi { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SvegaNabavno { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Razlika { get; set; }

    /// <summary>Procenat trgovačke marže korišćen za obračun Razlike (nije u legacy DBF-u — čuva se radi revizije obračuna).</summary>
    [Column(TypeName = "decimal(9, 4)")]
    public decimal MarzaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Porez { get; set; }

    /// <summary>Poreska stopa (PDV %) korišćena za obračun Poreza — čuva se radi revizije obračuna.</summary>
    [Column(TypeName = "decimal(9, 4)")]
    public decimal PoreskaStopaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    [MaxLength(20)]
    public string? SifraMagacina { get; set; }

    public bool IsKnjizen { get; set; }

    public List<KalkulacijaStavka> Stavke { get; set; } = new();
}

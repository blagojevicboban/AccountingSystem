using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class Nalog
{
    [Key]
    public int NalogId { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojNaloga { get; set; } = string.Empty;

    public DateTime DatumNaloga { get; set; } = DateTime.Now;

    [MaxLength(30)]
    public string VrstaNaloga { get; set; } = "Finansijski";

    [MaxLength(250)]
    public string? Opis { get; set; }

    public bool IsKnjizen { get; set; } = false;
    public DateTime? DatumKnjiženja { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoDuguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPotrazuje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Saldo => UkupnoDuguje - UkupnoPotrazuje;

    public bool IsUuravnotezen => Math.Abs(Saldo) < 0.01m;

    public List<StavkaNaloga> Stavke { get; set; } = new();
}

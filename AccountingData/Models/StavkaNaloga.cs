using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class StavkaNaloga
{
    [Key]
    public int StavkaNalogaId { get; set; }

    public int NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public int RedniBroj { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojKonta { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? BrojDokumenta { get; set; }

    public DateTime? DatumDokumenta { get; set; }
    public DateTime? ValutaDospela { get; set; }

    [MaxLength(250)]
    public string? Opis { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Duguje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Potrazuje { get; set; }

    public int? PartnerId { get; set; }
    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    [MaxLength(20)]
    public string? StariKonto { get; set; }

    public int? PromenaKod { get; set; }
}

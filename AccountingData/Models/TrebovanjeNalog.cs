using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class TrebovanjeNalog
{
    [Key]
    public int TrebovanjeNalogId { get; set; }

    [Required]
    [MaxLength(20)]
    public string BrojNaloga { get; set; } = string.Empty;

    public DateTime Datum { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(20)]
    public string SifraMagacina { get; set; } = string.Empty;

    public bool IsKnjizen { get; set; }

    public List<TrebovanjeStavka> Stavke { get; set; } = new();
}

public class TrebovanjeStavka
{
    [Key]
    public int TrebovanjeStavkaId { get; set; }

    public int TrebovanjeNalogId { get; set; }
    [ForeignKey(nameof(TrebovanjeNalogId))]
    public TrebovanjeNalog? TrebovanjeNalog { get; set; }

    public int RedniBroj { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraArtikla { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Cena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    [MaxLength(20)]
    public string? KontoTroska { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

public class UlazNalog
{
    [Key]
    public int UlazNalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(20)]
    public string SifraMagacina { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? BrojRacuna { get; set; }

    public DateTime? DatumRacuna { get; set; }

    public bool IsKnjizen { get; set; }

    public List<UlazStavka> Stavke { get; set; } = new();
}

public class UlazStavka
{
    [Key]
    public int UlazStavkaId { get; set; }

    public int UlazNalogId { get; set; }
    [ForeignKey(nameof(UlazNalogId))]
    public UlazNalog? UlazNalog { get; set; }

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

    [NotMapped]
    public string? NazivArtikla { get; set; }
}

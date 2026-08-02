using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

public class PrimopredajaNalog
{
    [Key]
    public int PrimopredajaNalogId { get; set; }

    public int BrojNaloga { get; set; }

    public DateTime Datum { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(20)]
    public string SifraMagacinaDaje { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string SifraMagacinaPrima { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string VrstaDokumenta { get; set; } = "Primopredaja";

    public bool IsKnjizen { get; set; }

    public List<PrimopredajaStavka> Stavke { get; set; } = new();
}

public class PrimopredajaStavka
{
    [Key]
    public int PrimopredajaStavkaId { get; set; }

    public int PrimopredajaNalogId { get; set; }
    [ForeignKey(nameof(PrimopredajaNalogId))]
    public PrimopredajaNalog? PrimopredajaNalog { get; set; }

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

    [NotMapped]
    public string? JedinicaMere { get; set; }
}

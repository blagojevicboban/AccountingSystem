using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class MaloprodajnaKalkulacijaStavka
{
    [Key]
    public int MaloprodajnaKalkulacijaStavkaId { get; set; }

    public int MaloprodajnaKalkulacijaId { get; set; }
    [ForeignKey(nameof(MaloprodajnaKalkulacijaId))]
    public MaloprodajnaKalkulacija? MaloprodajnaKalkulacija { get; set; }

    public int RedniBroj { get; set; }

    [Required]
    [MaxLength(20)]
    public string SifraArtikla { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Kolicina { get; set; }

    /// <summary>Uneta nabavna cena po jedinici mere.</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCena { get; set; }

    /// <summary>Kolicina * NabavnaCena (bez zavisnih troškova).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    /// <summary>Srazmerni deo MaloprodajnaKalkulacija.SvegaTroskovi (MAT3.PRG:965 — po učešću u Iznos).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Troskovi { get; set; }

    /// <summary>Iznos + Troskovi (MAT3.PRG:968, p_m_mal->nabavna).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaIznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezIznos { get; set; }

    /// <summary>NabavnaVrednost + RazlikaIznos + PorezIznos (MAT3.PRG:976, prod_sa_p).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    /// <summary>ProdajnaVrednost / Kolicina — knjiži se u robnu karticu kao izlazna cena (MAT3.PRG:980, prod_po_jm).</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    [NotMapped]
    public string? NazivArtikla { get; set; }

    [NotMapped]
    public string? JedinicaMere { get; set; }
}

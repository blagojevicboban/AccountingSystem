using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class KalkulacijaStavka
{
    [Key]
    public int KalkulacijaStavkaId { get; set; }

    public int KalkulacijaId { get; set; }
    [ForeignKey(nameof(KalkulacijaId))]
    public Kalkulacija? Kalkulacija { get; set; }

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

    /// <summary>Srazmerni deo Kalkulacija.SvegaTroskovi (MAT6.PRG:867 — po učešću u Iznos).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Troskovi { get; set; }

    /// <summary>Iznos + Troskovi (MAT6.PRG: p_m_kal->nabavna).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal NabavnaVrednost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaIznos { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezIznos { get; set; }

    /// <summary>NabavnaVrednost + RazlikaIznos + PorezIznos (MAT6.PRG: prod_sa_p).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    /// <summary>ProdajnaVrednost / Kolicina — ovo se knjiži u robnu karticu kao Cena (MAT6.PRG: prod_po_jm).</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }
}

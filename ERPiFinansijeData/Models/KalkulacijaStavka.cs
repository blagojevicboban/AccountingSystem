using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

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

    /// <summary>KAL_NAL.KOLICINA je N(12,4) — količina se čuva sa 4 decimale da uvoz ne bi zaokruživao.</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal Kolicina { get; set; }

    /// <summary>Uneta nabavna cena po jedinici mere (KAL_NAL.CENA).</summary>
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

    /// <summary>Procenat razlike u ceni / marže na ovoj stavci (KAL_NAL.RAZLIKA_PR).</summary>
    [Column(TypeName = "decimal(18, 6)")]
    public decimal RazlikaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaIznos { get; set; }

    /// <summary>NabavnaVrednost + RazlikaIznos, tj. prodajna vrednost pre poreza (KAL_NAL.PROD_BEZ_P).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednostBezPoreza { get; set; }

    /// <summary>Poreska stopa primenjena na stavku (KAL_NAL.POREZ_PR).</summary>
    [Column(TypeName = "decimal(9, 4)")]
    public decimal PorezProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezIznos { get; set; }

    /// <summary>Stopa posebnog poreza (KAL_NAL.POS_P_PR).</summary>
    [Column(TypeName = "decimal(9, 4)")]
    public decimal PosebanPorezProcenat { get; set; }

    /// <summary>Iznos posebnog poreza (KAL_NAL.POS_P_IZ).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PosebanPorezIznos { get; set; }

    /// <summary>Preneti (prethodni) porez iz ulaznog računa dobavljača (KAL_NAL.PREN_POR).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrenetiPorez { get; set; }

    /// <summary>Preneti poseban porez iz ulaznog računa dobavljača (KAL_NAL.PREN_P_POR).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrenetiPosebanPorez { get; set; }

    /// <summary>PorezIznos − PrenetiPorez, obaveza po ovoj stavci (KAL_NAL.POR_ZA_UPL).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezZaUplatu { get; set; }

    /// <summary>NabavnaVrednost + RazlikaIznos + PorezIznos (MAT6.PRG: prod_sa_p).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    /// <summary>ProdajnaVrednost / Kolicina — ovo se knjiži u robnu karticu kao Cena (MAT6.PRG: prod_po_jm).</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    /// <summary>Prodajna cena artikla pre ove kalkulacije (KAL_NAL.STARA_CENA) — osnov za nivelaciju zaliha.</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal StaraCena { get; set; }

    /// <summary>KAL_NAL.KNJIZEN — legacy sistem knjiži stavku po stavku, ne samo zaglavlje.</summary>
    public bool IsKnjizen { get; set; }

    [NotMapped]
    public string? NazivArtikla { get; set; }

    [NotMapped]
    public string? JedinicaMere { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

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

    /// <summary>MAL_NAL.KOLICINA je N(12,4) — količina se čuva sa 4 decimale da uvoz ne bi zaokruživao.</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal Kolicina { get; set; }

    /// <summary>Uneta nabavna cena po jedinici mere (MAL_NAL.CENA).</summary>
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

    /// <summary>Procenat razlike u ceni / marže na ovoj stavci (MAL_NAL.RAZLIKA_PR).</summary>
    [Column(TypeName = "decimal(18, 6)")]
    public decimal RazlikaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaIznos { get; set; }

    /// <summary>NabavnaVrednost + RazlikaIznos, tj. prodajna vrednost pre poreza (MAL_NAL.PROD_BEZ_P).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednostBezPoreza { get; set; }

    /// <summary>Poreska stopa primenjena na stavku (MAL_NAL.POREZ_PR).</summary>
    [Column(TypeName = "decimal(9, 4)")]
    public decimal PorezProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezIznos { get; set; }

    /// <summary>Stopa posebnog poreza (MAL_NAL.POS_P_PR).</summary>
    [Column(TypeName = "decimal(9, 4)")]
    public decimal PosebanPorezProcenat { get; set; }

    /// <summary>Iznos posebnog poreza (MAL_NAL.POS_P_IZ).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PosebanPorezIznos { get; set; }

    /// <summary>Preneti (prethodni) porez iz ulaznog računa dobavljača (MAL_NAL.PREN_POR).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrenetiPorez { get; set; }

    /// <summary>Preneti poseban porez (MAL_NAL.POS_POR_PR — u maloprodajnoj strukturi zauzima mesto PREN_P_POR iz KAL_NAL).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrenetiPosebanPorez { get; set; }

    /// <summary>PorezIznos − PrenetiPorez, obaveza po ovoj stavci (MAL_NAL.POR_ZA_UPL).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal PorezZaUplatu { get; set; }

    /// <summary>Taksa na stavci (MAL_NAL.TAKSA).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Taksa { get; set; }

    /// <summary>NabavnaVrednost + RazlikaIznos + PorezIznos (MAT3.PRG:976, prod_sa_p).</summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaVrednost { get; set; }

    /// <summary>ProdajnaVrednost / Kolicina — knjiži se u robnu karticu kao izlazna cena (MAT3.PRG:980, prod_po_jm).</summary>
    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdajnaCena { get; set; }

    /// <summary>Tarifni broj artikla zapamćen u trenutku kalkulacije (MAL_NAL.TARIFNI).</summary>
    [MaxLength(10)]
    public string? TarifniBroj { get; set; }

    /// <summary>Broj razduženja kojim je stavka razdužena iz maloprodaje (MAL_NAL.BR_RAZDUZ).</summary>
    public int? BrojRazduzenja { get; set; }

    /// <summary>MAL_NAL.KNJIZEN — legacy sistem knjiži stavku po stavku, ne samo zaglavlje.</summary>
    public bool IsKnjizen { get; set; }

    /// <summary>MAL_NAL.T_KNJIZEN — zasebna oznaka trgovinskog knjiženja (razdvojeno od finansijskog).</summary>
    public bool IsTrgovinskiKnjizen { get; set; }

    /// <summary>
    /// MAL_NAL.NAZ_ROBE / JED_MERE — legacy fajl denormalizovano čuva naziv i JM na stavci.
    /// Ostaju NotMapped (izvor istine je šifarnik artikala), ali ih uvoz koristi kao fallback za prikaz.
    /// </summary>
    [NotMapped]
    public string? NazivArtikla { get; set; }

    [NotMapped]
    public string? JedinicaMere { get; set; }
}

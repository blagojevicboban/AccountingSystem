using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class NivelacijaCena
{
    [Key]
    public int NivelacijaCenaId { get; set; }

    public int BrojNivelacije { get; set; }

    public DateTime DatumNivelacije { get; set; } = DateTime.Now;

    public int? MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    [MaxLength(250)]
    public string? Opis { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoRazlika { get; set; }

    public bool IsKnjizen { get; set; }

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public List<NivelacijaStavka> Stavke { get; set; } = new();

    [NotMapped]
    public string SifraMagacina { get; set; } = string.Empty;

    [NotMapped]
    public string NazivMagacina { get; set; } = string.Empty;
}

public class NivelacijaStavka
{
    [Key]
    public int NivelacijaStavkaId { get; set; }

    public int NivelacijaCenaId { get; set; }
    [ForeignKey(nameof(NivelacijaCenaId))]
    public NivelacijaCena? NivelacijaCena { get; set; }

    public int RedniBroj { get; set; }

    public int? ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal KolicinaZaliha { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal StaraCena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal NovaCena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RazlikaPoJedinici { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnaRazlika { get; set; }

    [NotMapped]
    public string SifraArtikla { get; set; } = string.Empty;

    [NotMapped]
    public string NazivArtikla { get; set; } = string.Empty;

    [NotMapped]
    public string JedinicaMere { get; set; } = "kom";
}

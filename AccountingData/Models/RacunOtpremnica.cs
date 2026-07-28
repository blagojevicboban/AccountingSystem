using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class RacunOtpremnica
{
    [Key]
    public int RacunOtpremnicaId { get; set; }

    [Required]
    [MaxLength(30)]
    public string BrojRacuna { get; set; } = string.Empty;

    public DateTime DatumRacuna { get; set; } = DateTime.Now;
    public DateTime? RokPlacanja { get; set; }

    public int? PartnerId { get; set; }
    [ForeignKey(nameof(PartnerId))]
    public Partner? Partner { get; set; }

    public int? MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    [MaxLength(250)]
    public string? Napomena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoOsnovica { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoRabat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoZaUplatu { get; set; }

    public bool IsKnjizen { get; set; }

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public List<RacunOtpremnicaStavka> Stavke { get; set; } = new();

    [NotMapped]
    public string? BrojOtpremnice { get; set; }
    [NotMapped]
    public DateTime DatumOtpremnice { get => DatumRacuna; set => DatumRacuna = value; }
    [NotMapped]
    public string KontoKupca { get; set; } = string.Empty;
    [NotMapped]
    public int RokPlacanjaDana { get; set; } = 15;
    [NotMapped]
    public string? NacinPlacanja { get; set; }
    [NotMapped]
    public decimal IznosBezPdv { get => UkupnoOsnovica; set => UkupnoOsnovica = value; }
    [NotMapped]
    public decimal PdvIznos { get => UkupnoPdv; set => UkupnoPdv = value; }
    [NotMapped]
    public decimal UkupanIznos { get => UkupnoZaUplatu; set => UkupnoZaUplatu = value; }
}

public class RacunOtpremnicaStavka
{
    [Key]
    public int RacunOtpremnicaStavkaId { get; set; }

    public int RacunOtpremnicaId { get; set; }
    [ForeignKey(nameof(RacunOtpremnicaId))]
    public RacunOtpremnica? RacunOtpremnica { get; set; }

    public int RedniBroj { get; set; }

    public int? ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 3)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ProdajnaCena { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RabatProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal StopaPdv { get; set; } = 20m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Osnovica { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal IznosPdv { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Ukupno { get; set; }

    [NotMapped]
    public string SifraArtikla { get; set; } = string.Empty;

    [NotMapped]
    public string? NazivArtikla { get; set; }

    [NotMapped]
    public decimal Cena { get => ProdajnaCena; set => ProdajnaCena = value; }

    [NotMapped]
    public decimal PdvProcenat { get => StopaPdv; set => StopaPdv = value; }

    [NotMapped]
    public decimal IznosBezPdv { get => Osnovica; set => Osnovica = value; }

    [NotMapped]
    public decimal PdvIznos { get => IznosPdv; set => IznosPdv = value; }

    [NotMapped]
    public decimal UkupanIznos { get => Ukupno; set => Ukupno = value; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

/// <summary>
/// Model uvozne kalkulacije sa uvoznim zavisnim troškovima (carina, prevoz, špedicija).
/// </summary>
public class UvoznaKalkulacija
{
    [Key]
    public int UvoznaKalkulacijaId { get; set; }

    [Required]
    [MaxLength(50)]
    public string BrojKalkulacije { get; set; } = string.Empty;

    public DateTime DatumKalkulacije { get; set; } = DateTime.Today;

    public int InoPartnerId { get; set; }
    [ForeignKey(nameof(InoPartnerId))]
    public Partner? InoPartner { get; set; }

    [MaxLength(50)]
    public string InoBrojFakture { get; set; } = string.Empty;

    public DateTime DatumInoFakture { get; set; } = DateTime.Today;

    [MaxLength(10)]
    public string Valuta { get; set; } = "EUR";

    [Column(TypeName = "decimal(18, 4)")]
    public decimal KursValute { get; set; } = 117.20m;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoDevize { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnoFakturaRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CarinaRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SpedicijaRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal PrevozRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal OstaliZavisniTroskoviRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnaNabavnaVrednostRsd { get; set; }

    public int MagacinId { get; set; }
    [ForeignKey(nameof(MagacinId))]
    public Magacin? Magacin { get; set; }

    public bool IsKnjizeno { get; set; }

    public List<UvoznaStavka> Stavke { get; set; } = new();
}

public class UvoznaStavka
{
    [Key]
    public int UvoznaStavkaId { get; set; }

    public int UvoznaKalkulacijaId { get; set; }
    [ForeignKey(nameof(UvoznaKalkulacijaId))]
    public UvoznaKalkulacija? UvoznaKalkulacija { get; set; }

    public int ArtikalId { get; set; }
    [ForeignKey(nameof(ArtikalId))]
    public Artikal? Artikal { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Kolicina { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal InoCenaDevize { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal InoIznosDevize { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal InoIznosRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CarinaProcenat { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CarinaIznosRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal RasporedjeniZavisniTroskoviRsd { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal UkupnaNabavnaVrednostRsd { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal NabavnaCenaPoJediniciRsd { get; set; }
}

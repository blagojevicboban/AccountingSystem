using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

public class KursnaListaStavka
{
    [Key]
    public int KursnaListaStavkaId { get; set; }

    public DateTime Datum { get; set; }

    [Required]
    [MaxLength(10)]
    public string ValutaOznaka { get; set; } = string.Empty; // EUR, USD, CHF, GBP, etc.

    public int ValutaSifra { get; set; } // 978 za EUR, 840 za USD, etc.

    [MaxLength(100)]
    public string NazivValute { get; set; } = string.Empty;

    public int Jedinica { get; set; } = 1; // 1 ili 100

    [Column(TypeName = "decimal(18, 4)")]
    public decimal SrednjiKurs { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal KupovniKurs { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal ProdavniKurs { get; set; }
}

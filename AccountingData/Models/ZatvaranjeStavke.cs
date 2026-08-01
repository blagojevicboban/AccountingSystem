using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

/// <summary>
/// Parovanje (zatvaranje) dve stavke naloga — npr. faktura (Duguje) zatvorena
/// uplatom (Potražuje). M:N po stavci je moguć (delimična zatvaranja), zato je
/// ovo zasebna relaciona tabela a ne polje na StavkaNaloga.
/// </summary>
public class ZatvaranjeStavke
{
    [Key]
    public int ZatvaranjeStavkeId { get; set; }

    public int StavkaDugujeId { get; set; }
    [ForeignKey(nameof(StavkaDugujeId))]
    public StavkaNaloga? StavkaDuguje { get; set; }

    public int StavkaPotrazujeId { get; set; }
    [ForeignKey(nameof(StavkaPotrazujeId))]
    public StavkaNaloga? StavkaPotrazuje { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Iznos { get; set; }

    public DateTime DatumZatvaranja { get; set; } = DateTime.Now;

    [MaxLength(30)]
    public string VrstaZatvaranja { get; set; } = "Rucno";

    [MaxLength(250)]
    public string? Napomena { get; set; }

    public int? KorisnikId { get; set; }

    [MaxLength(100)]
    public string? KorisnickoIme { get; set; }
}

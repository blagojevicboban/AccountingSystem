using System.ComponentModel.DataAnnotations;

namespace AccountingData.Models;

/// <summary>
/// Trag ko je i kada rasknjižio (ili ponovo proknjižio) nalog. BrojNaloga i
/// KorisnickoIme su namerno denormalizovani da zapis ostane čitljiv i posle
/// eventualnog brisanja naloga ili korisnika.
/// </summary>
public class NalogAudit
{
    [Key]
    public int NalogAuditId { get; set; }

    public int NalogId { get; set; }
    public int BrojNaloga { get; set; }

    [Required]
    [MaxLength(30)]
    public string Akcija { get; set; } = string.Empty;

    public int? KorisnikId { get; set; }

    [MaxLength(100)]
    public string? KorisnickoIme { get; set; }

    public DateTime Vreme { get; set; } = DateTime.Now;
}

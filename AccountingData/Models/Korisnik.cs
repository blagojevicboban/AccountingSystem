using System.ComponentModel.DataAnnotations;

namespace AccountingData.Models;

public class Korisnik
{
    [Key]
    public int KorisnikId { get; set; }

    [Required]
    [MaxLength(50)]
    public string KorisnickoIme { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LozinkaHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ImeIPrezime { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Uloga { get; set; } = "Knjigovođa";

    public bool IsActive { get; set; } = true;
    public DateTime? PoslednjaPrijava { get; set; }
}

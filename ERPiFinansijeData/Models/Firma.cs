using System.ComponentModel.DataAnnotations;

namespace ERPiFinansijeData.Models;

public class Firma
{
    [Key]
    public int FirmaId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Sifra { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Naziv { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Adresa { get; set; }

    [MaxLength(100)]
    public string? PttIMesto { get; set; }

    [MaxLength(50)]
    public string? Telefon { get; set; }

    [MaxLength(50)]
    public string? ZiroRacun { get; set; }

    [MaxLength(30)]
    public string? Pib { get; set; }

    [MaxLength(30)]
    public string? MaticniBroj { get; set; }

    [MaxLength(20)]
    public string? JbkjsBroj { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(250)]
    public string? SefApiKey { get; set; }

    [MaxLength(20)]
    public string SefEnvironment { get; set; } = "Demo";

    [MaxLength(250)]
    public string PfrUrl { get; set; } = "http://localhost:8443";

    [MaxLength(100)]
    public string PfrPacKod { get; set; } = "123456";

    [MaxLength(100)]
    public string PfrKasirName { get; set; } = "Glavni Kasir";

    /// <summary>
    /// Dozvoljava rad sa simuliranom fiskalizacijom kada PFR nije dostupan.
    /// Podrazumevano ISKLJUČENO - simulirani računi nemaju pravnu vrednost.
    /// </summary>
    public bool PfrSimulatorMod { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime DatumKreiranja { get; set; } = DateTime.Now;
}

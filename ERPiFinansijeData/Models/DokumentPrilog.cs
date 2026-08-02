using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

/// <summary>
/// Priloženi skenirani dokument (PDF, slika ulaznog računa, ugovor) u DMS sistemu.
/// </summary>
public class DokumentPrilog
{
    [Key]
    public int DokumentPrilogId { get; set; }

    public int? NalogId { get; set; }
    [ForeignKey(nameof(NalogId))]
    public Nalog? Nalog { get; set; }

    public int? RacunOtpremnicaId { get; set; }
    [ForeignKey(nameof(RacunOtpremnicaId))]
    public RacunOtpremnica? RacunOtpremnica { get; set; }

    public int? KalkulacijaId { get; set; }
    [ForeignKey(nameof(KalkulacijaId))]
    public Kalkulacija? Kalkulacija { get; set; }

    [Required]
    [MaxLength(250)]
    public string NazivFajla { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TipDokumenta { get; set; } = "Ulazni Račun"; // Ulazni Račun, Ugovor, Zapisnik, Ostalo

    [MaxLength(500)]
    public string PutanjaFajla { get; set; } = string.Empty;

    public long VelicinaBytes { get; set; }

    public DateTime DatumPriloga { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string Korisnik { get; set; } = "Admin";
}

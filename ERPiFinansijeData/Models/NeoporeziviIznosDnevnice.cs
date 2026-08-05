using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

/// <summary>
/// Zakonski neoporeziv iznos dnevnice za službeni put u zemlji, koji važi počev od
/// <see cref="DatumOd"/>, do sledeće definisane vrednosti (ili do danas ako je poslednja).
/// Isti obrazac kao <see cref="KamatnaStopa"/> — vrednost menja propis, ne kod (Faza 3.2).
///
/// Ovo je zakonski limit, ne stvarno isplaćena dnevnica: <c>PutniNalog.IznosDnevniceRsd</c>
/// ostaje po nalogu i može biti veći. Deo isplaćene dnevnice iznad ovog limita se po zakonu
/// tretira kao deo zarade radnika i prijavljuje se kroz PPP-PD — videti
/// <c>PutniNalogService.PrekoracenjeDnevnice</c>.
///
/// Samo dnevnice u zemlji; inostranstvo ostaje van obima Faze 3.2.
/// </summary>
public class NeoporeziviIznosDnevnice
{
    [Key]
    public int NeoporeziviIznosDnevniceId { get; set; }

    public DateTime DatumOd { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal IznosZemljaRsd { get; set; }

    [MaxLength(200)]
    public string? Napomena { get; set; }
}

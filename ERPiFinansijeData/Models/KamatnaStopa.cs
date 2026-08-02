using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERPiFinansijeData.Models;

/// <summary>
/// Godišnja stopa zatezne kamate koja važi počev od <see cref="DatumOd"/>,
/// do sledeće definisane stope (ili do danas ako je poslednja). Analogno
/// legacy KAM_STOP.DBF / unos_stope proceduri iz FIN2.PRG.
/// </summary>
public class KamatnaStopa
{
    [Key]
    public int KamatnaStopaId { get; set; }

    public DateTime DatumOd { get; set; }

    [Column(TypeName = "decimal(9, 4)")]
    public decimal GodisnjaStopaProcenat { get; set; }

    [MaxLength(200)]
    public string? Napomena { get; set; }
}

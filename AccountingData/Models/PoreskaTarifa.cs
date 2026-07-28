using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingData.Models;

/// <summary>
/// Šifarnik poreskih tarifa (tarifni brojevi), analogno legacy tarife.dbf
/// (tar_broj, porez_pr, pos_p_pr, por_u_cen) iz MAT6.PRG / tarifni().
/// </summary>
public class PoreskaTarifa
{
    [Key]
    public int PoreskaTarifaId { get; set; }

    [Required]
    [MaxLength(2)]
    public string TarifniBroj { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PorezProcenat { get; set; }

    /// <summary>Posebna (dodatna) poreska stopa — legacy POS_P_PR, korišćena u ANAL1/ANAL2/UN_KAL/MAT3/MAT5.</summary>
    [Column(TypeName = "decimal(5, 2)")]
    public decimal PosebanPorezProcenat { get; set; }

    public bool PorezUCeni { get; set; }

    [NotMapped]
    public string Prikaz => string.IsNullOrEmpty(TarifniBroj)
        ? "(bez tarife)"
        : PorezUCeni ? $"{TarifniBroj} - {PorezProcenat:N0}% (u ceni)" : $"{TarifniBroj} - {PorezProcenat:N0}%";
}

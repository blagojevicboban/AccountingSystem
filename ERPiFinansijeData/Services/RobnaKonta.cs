namespace ERPiFinansijeData.Services;

/// <summary>
/// Konta na koja se knjiže robna dokumenta. Brojevi nisu izvedeni iz Kontnog okvira nego su
/// očitani iz stvarnih naloga u zatečenim bazama ovih firmi, jer se analitika (naročito
/// razlika u ceni i ukalkulisani PDV) razlikuje od firme do firme.
///
/// Maloprodajna kalkulacija — obrazac potvrđen na 123 naloga u ARHIBEL 2026
/// (opis stavke „KALKULACIJA NA MALO"), npr. nalog 31 / KALK 1:
/// <code>
///   1340  duguje    97.500,00   (prodajna vrednost SA PDV)
///   1344  potražuje 16.250,00   (ukalkulisani PDV)
///   1348  potražuje 15.259,08   (ukalkulisana razlika u ceni)
///   435xx potražuje 65.990,92   (dobavljač — svega nabavno)
/// </code>
///
/// Veleprodajna kalkulacija — obrazac potvrđen u ARHIBEL 2002
/// (opis stavke „KALKUL.VELEPRODAJE"), nalog 410 / KALK.3:
/// <code>
///   1320  duguje    78.170,00   (prodajna vrednost BEZ PDV)
///   1329  potražuje  7.475,75   (razlika u ceni)
///   432xx potražuje 70.694,25   (dobavljač — svega nabavno)
/// </code>
/// Veleprodaja nema ukalkulisani PDV — to je „korak više" koji ima samo maloprodaja, pa se
/// roba u veleprodaji vodi po prodajnoj vrednosti bez poreza.
/// </summary>
public static class RobnaKonta
{
    /// <summary>Roba u veleprodaji / stovarištu (vodi se po prodajnoj vrednosti bez PDV).</summary>
    public const string RobaVeleprodaja = "1320";

    /// <summary>Razlika u ceni robe u veleprodaji.</summary>
    public const string RazlikaUCeniVeleprodaja = "1329";

    /// <summary>Roba u maloprodaji / prodavnici (vodi se po prodajnoj vrednosti sa PDV).</summary>
    public const string RobaMaloprodaja = "1340";

    /// <summary>Ukalkulisani PDV u prometu na malo.</summary>
    public const string UkalkulisaniPdvMaloprodaja = "1344";

    /// <summary>
    /// Ukalkulisana razlika u ceni u maloprodaji. Namerno 1348, a ne 1349: u kontnom planu
    /// postoje oba („UKALKULISANA RAZLIKA U CENI U MALOPRODAJI" i „RAZLIKA U CENI ROBE U
    /// MALOPRODAJI"), ali svih 143 zatečenih knjiženja idu na 1348, dok 1349 nema nijednu stavku.
    /// </summary>
    public const string RazlikaUCeniMaloprodaja = "1348";

    /// <summary>Konto magacina prema vrsti — maloprodajni magacin nosi robu sa PDV.</summary>
    public static string RobaZaVrstuMagacina(string? vrstaMagacina)
        => vrstaMagacina == "Maloprodaja" ? RobaMaloprodaja : RobaVeleprodaja;

    /// <summary>Konto razlike u ceni prema vrsti magacina.</summary>
    public static string RazlikaZaVrstuMagacina(string? vrstaMagacina)
        => vrstaMagacina == "Maloprodaja" ? RazlikaUCeniMaloprodaja : RazlikaUCeniVeleprodaja;
}

namespace AccountingData.Models;

/// <summary>
/// Stavka Obrasca PB-1 (Poreski bilans obveznika poreza na dobit pravnih lica).
/// </summary>
public class Pb1Stavka
{
    public int RedniBroj { get; set; }
    public string Opis { get; set; } = string.Empty;
    public decimal RacunovodstveniIznos { get; set; }
    public decimal PoreskiIznos { get; set; }
    public decimal Uskladjivanje { get; set; } // Povećanje ili smanjenje oporezive dobiti
}

/// <summary>
/// Obračun Poreske Amortizacije (Obrazac OA).
/// </summary>
public class PoreskaAmortizacijaStavka
{
    public int Grupa { get; set; } // 1 do 5
    public string NazivGrupe { get; set; } = string.Empty; // I grupa (nepokretnosti), II grupa (10%), III grupa (15%), IV grupa (20%), V grupa (30%)
    public decimal PoreskaStopa { get; set; }
    public decimal NabavnaVrednost { get; set; }
    public decimal NeotpisanaPoreskaVrednost { get; set; }
    public decimal RacunovodstvenaAmortizacija { get; set; }
    public decimal PoreskaAmortizacija { get; set; }
    public decimal RazlikaAmortizacije => RacunovodstvenaAmortizacija - PoreskaAmortizacija; // Razlika za usklađivanje u PB-1
}

/// <summary>
/// Obrazac PDP — Poreska prijava za porez na dobit.
/// </summary>
public class ObrazacPdpResult
{
    public string NazivObveznika { get; set; } = string.Empty;
    public string Pib { get; set; } = string.Empty;
    public int PoreskiPeriodGodina { get; set; }
    public decimal OporezivaDobit { get; set; }
    public decimal StopaPoreza { get; set; } = 15.0m;
    public decimal ObracunatiPorez { get; set; }
    public decimal PoreskiKredit { get; set; }
    public decimal NetKonacnaPoreskaObaveza { get; set; }
    public decimal MesecnaAkontacija { get; set; }
}

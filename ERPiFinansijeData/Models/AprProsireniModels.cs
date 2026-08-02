namespace ERPiFinansijeData.Models;

/// <summary>
/// AOP Pozicija za Statistički Izveštaj (SI) APR.
/// </summary>
public class StatistickiIzvestajStavka
{
    public int Aop { get; set; }
    public string Opis { get; set; } = string.Empty;
    public string KontoGrupa { get; set; } = string.Empty;
    public decimal IznosTekuca { get; set; }
    public decimal IznosPrethodna { get; set; }
}

/// <summary>
/// AOP Pozicija za Izveštaj o Tokovima Gotovine (Cash Flow Statement).
/// </summary>
public class CashFlowStavka
{
    public int Aop { get; set; }
    public string Opis { get; set; } = string.Empty;
    public string TipAktivnosti { get; set; } = "Poslovne"; // Poslovne, Investicione, Finansijske
    public decimal Priliv { get; set; }
    public decimal Odliv { get; set; }
    public decimal NetoPrilivOdliv => Priliv - Odliv;
}

/// <summary>
/// AOP Pozicija za Izveštaj o Promenama na Kapitalu.
/// </summary>
public class PromeneNaKapitaluStavka
{
    public int Aop { get; set; }
    public string Opis { get; set; } = string.Empty;
    public decimal OsnovniKapital { get; set; }
    public decimal Rezerve { get; set; }
    public decimal NerasporedjenaDobit { get; set; }
    public decimal Gubitak { get; set; }
    public decimal UkupnoKapital => OsnovniKapital + Rezerve + NerasporedjenaDobit - Gubitak;
}

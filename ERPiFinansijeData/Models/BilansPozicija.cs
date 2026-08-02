namespace ERPiFinansijeData.Models;

public enum TipBilansa
{
    BilansStanja,
    BilansUspeha
}

public enum TipPozicijeBilansa
{
    Naslov,
    Grupa,
    AopStavka,
    Ukupno
}

public class BilansPozicija
{
    public string AopCode { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public string OpsegKonta { get; set; } = string.Empty; // Npr. "00,01,02" ili "10,11" ili "60,61"
    public TipBilansa TipBilansa { get; set; }
    public TipPozicijeBilansa TipPozicije { get; set; } = TipPozicijeBilansa.AopStavka;
    public decimal IznosTekucaGodina { get; set; }
    public decimal IznosPrethodnaGodina { get; set; }
    public bool IsDugovnaStrana { get; set; } = true; // True za Aktiva / Rashodi, False za Pasiva / Prihodi
}

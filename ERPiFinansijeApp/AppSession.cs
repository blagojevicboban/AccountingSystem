using ERPiFinansijeData.Models;

namespace ERPiFinansijeApp;

public static class AppSession
{
    public static Korisnik? TrenutniKorisnik { get; set; }

    public static event Action? TrenutnaFirmaChanged;

    private static Firma? _trenutnaFirma;
    public static Firma? TrenutnaFirma
    {
        get => _trenutnaFirma;
        set
        {
            _trenutnaFirma = value;
            TrenutnaFirmaChanged?.Invoke();
        }
    }

    public static bool IsAdministrator => TrenutniKorisnik?.Uloga == "Administrator";
}

namespace ERPiFinansijeData.Services;

/// <summary>
/// Konto koji nalog iz ERPiZarade traži, a kontni plan ga nema — sa predloženim nazivom.
/// </summary>
public sealed class KontoZaZavodjenje
{
    public required string BrojKonta { get; init; }

    /// <summary>Predlog naziva; korisnik ga vidi pre nego što potvrdi zavođenje.</summary>
    public required string NazivKonta { get; init; }

    /// <summary>Naziv je iz Kontnog okvira, a ne izveden iz opisa stavke.</summary>
    public bool IzKontnogOkvira { get; init; }

    public string Prikaz => $"{BrojKonta} — {NazivKonta}";
}

/// <summary>
/// Nazivi konta iz <b>Pravilnika o Kontnom okviru i sadržini računa za privredna društva,
/// zadruge i preduzetnike</b>, i to samo onih koja se pojavljuju u nalogu za knjiženje zarada.
///
/// Postoji zato što ERPiFinansije nema podrazumevani kontni plan — konta stižu iz DBF
/// migracije ili se unose ručno. Firma koja pređe na novu verziju ERPiZarade zato prvi uvoz
/// dočeka porukom „konto ne postoji", i mora da otvori četiri-pet konta rukom, tražeći im
/// tačne nazive u propisu. Ovde stoje unapred, pa se zavode jednim potvrđivanjem.
///
/// To su <b>predlozi</b>, ne pravilo: korisnik ih vidi pre zavođenja i posle menja u kontnom
/// planu kao i svaki drugi konto. Firma koja vodi analitiku (520-1 po jedinici) dobija naziv
/// svoje sintetike, jer se analitika ionako imenuje po organizacionom delu.
/// </summary>
public static class ZaradeKontniOkvir
{
    private static readonly Dictionary<string, string> Nazivi = new(StringComparer.Ordinal)
    {
        // ── Potraživanja ─────────────────────────────────────────────
        ["225"] = "Potraživanja za naknade zarada koje se refundiraju",

        // ── Obaveze po osnovu zarada i naknada zarada ────────────────
        ["450"] = "Obaveze za neto zarade i naknade zarada, osim naknada zarada koje se refundiraju",
        ["451"] = "Obaveze za porez na zarade i naknade zarada na teret zaposlenog",
        ["452"] = "Obaveze za doprinose na zarade i naknade zarada na teret zaposlenog",
        ["453"] = "Obaveze za poreze i doprinose na zarade i naknade zarada na teret poslodavca",
        ["454"] = "Obaveze za neto naknade zarada koje se refundiraju",
        ["455"] = "Obaveze za poreze i doprinose na naknade zarada na teret zaposlenog koje se refundiraju",
        ["456"] = "Obaveze za poreze i doprinose na naknade zarada na teret poslodavca koje se refundiraju",

        // ── Ostale obaveze ───────────────────────────────────────────
        ["465"] = "Obaveze prema fizičkim licima za naknade po ugovorima",
        ["469"] = "Ostale obaveze",
        ["489"] = "Ostale obaveze za poreze, doprinose i druge dažbine",

        // ── Troškovi ─────────────────────────────────────────────────
        ["520"] = "Troškovi zarada i naknada zarada (bruto)",
        ["521"] = "Troškovi poreza i doprinosa na zarade i naknade zarada na teret poslodavca",
        ["522"] = "Troškovi naknada po ugovoru o delu",
        ["523"] = "Troškovi naknada po autorskim ugovorima",
        ["524"] = "Troškovi naknada po ugovoru o privremenim i povremenim poslovima",
        ["525"] = "Troškovi naknada fizičkim licima po osnovu ostalih ugovora",
        ["526"] = "Troškovi naknada članovima upravnog i nadzornog odbora",
        ["529"] = "Ostali lični rashodi i naknade"
    };

    /// <summary>
    /// Predlog za zavođenje. Traži se tačan broj, pa <b>sintetika</b> (prve tri cifre) za
    /// analitička konta; ako ni to ne pomogne, uzima se opis stavke iz naloga, koji je
    /// ERPiZarade već upisao — bolji je od praznog naziva, a korisnik ga ionako potvrđuje.
    /// </summary>
    public static KontoZaZavodjenje Predlozi(string brojKonta, string? opisStavke)
    {
        string broj = (brojKonta ?? string.Empty).Trim();

        if (Nazivi.TryGetValue(broj, out string? tacan))
            return new KontoZaZavodjenje { BrojKonta = broj, NazivKonta = tacan, IzKontnogOkvira = true };

        if (broj.Length > 3 && Nazivi.TryGetValue(broj[..3], out string? sintetika))
            return new KontoZaZavodjenje { BrojKonta = broj, NazivKonta = sintetika, IzKontnogOkvira = true };

        string izOpisa = (opisStavke ?? string.Empty).Trim();

        return new KontoZaZavodjenje
        {
            BrojKonta = broj,
            NazivKonta = izOpisa.Length > 0 ? izOpisa : $"Konto {broj}",
            IzKontnogOkvira = false
        };
    }
}

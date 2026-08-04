using System.Globalization;
using System.Text;
using ERPiFinansijeData.Models;
using DbfDataReader;

namespace ERPiFinansijeData.Services;

/// <summary>
/// Jedino mesto gde se pojavljuju stvarna imena DBF kolona iz legacy Clipper sistema
/// (KONTPLAN, ANKONT, MAGACIN, ARTIKLI, NALOG). Koriste ga i ERPiFinansijeApp (uvoz iz UI)
/// i ERPiFinansijeMigration (samostalni alat), da mapiranje ne bi divergiralo na dva mesta.
/// </summary>
public static class DbfImportService
{
    public static List<Dictionary<string, string>> ReadRows(string filepath)
    {
        var list = new List<Dictionary<string, string>>();
        if (!File.Exists(filepath)) return list;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            var encoding = Encoding.GetEncoding(852);
            var opts = new DbfDataReaderOptions { Encoding = encoding };

            using var reader = new DbfDataReader.DbfDataReader(filepath, opts);
            var colNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                colNames.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var val = reader.GetValue(i)?.ToString()?.Trim() ?? "";
                    row[colNames[i]] = val;
                }
                list.Add(row);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Greška pri čitanju DBF fajla {Putanja}", filepath);
        }

        return list;
    }

    private static string Get(Dictionary<string, string> row, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }
        return "";
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>KONTPLAN.DBF → Konto. Vraća null ako red nema broj konta (KONTO).</summary>
    public static Konto? MapKonto(Dictionary<string, string> row)
    {
        string broj = Get(row, "KONTO");
        if (string.IsNullOrWhiteSpace(broj)) return null;

        string naziv = Get(row, "OPIS_KONTA");
        int klasa = 0;
        if (broj.Length > 0 && char.IsDigit(broj[0])) klasa = broj[0] - '0';

        return new Konto
        {
            BrojKonta = broj,
            NazivKonta = string.IsNullOrWhiteSpace(naziv) ? $"Konto {broj}" : naziv,
            Klasa = klasa,
            IsSintetika = broj.Length <= 3,
            StariKonto = NullIfEmpty(Get(row, "ST_KON")),
            Ulica = NullIfEmpty(Get(row, "ULICA_I_BR")),
            Mesto = NullIfEmpty(Get(row, "MESTO_I_BR")),
            ZiroRacun = NullIfEmpty(Get(row, "ZIRO_RACUN")),
            Telefon = NullIfEmpty(Get(row, "TELEFON"))
        };
    }

    /// <summary>ANKONT.DBF → Partner. Vraća null ako red nema naziv (OPIS_KONTA).</summary>
    public static Partner? MapPartner(Dictionary<string, string> row, int fallbackBroj)
    {
        string naziv = Get(row, "OPIS_KONTA");
        if (string.IsNullOrWhiteSpace(naziv)) return null;

        string sifra = Get(row, "KONTO");
        if (string.IsNullOrWhiteSpace(sifra)) sifra = fallbackBroj.ToString("D4");

        return new Partner
        {
            SifraPartnera = sifra,
            Naziv = naziv,
            Adresa = NullIfEmpty(Get(row, "ULICA_I_BR")),
            PttIMesto = NullIfEmpty(Get(row, "MESTO_I_BR")),
            ZiroRacun = NullIfEmpty(Get(row, "ZIRO_RACUN")),
            Telefon = NullIfEmpty(Get(row, "TELEFON")),
            KontoPartnera = sifra
        };
    }

    /// <summary>MAGACIN.DBF → Magacin.</summary>
    public static Magacin? MapMagacin(Dictionary<string, string> row)
    {
        string sifra = Get(row, "SIFRA", "KOD");
        if (string.IsNullOrWhiteSpace(sifra)) return null;

        // U MAGACIN.DBF polje RACUNOPOL sadrži naziv računopolagača/magacina (npr. "CENTRALNI MAGACIN", "SUMSKO")
        string naziv = Get(row, "RACUNOPOL", "NAZIV", "IME", "OPIS");
        if (string.IsNullOrWhiteSpace(naziv)) naziv = $"Magacin {sifra}";

        string odgLice = Get(row, "ODG_LICE", "ODGOVORNO", "LICE");

        return new Magacin
        {
            SifraMagacina = sifra,
            NazivMagacina = naziv,
            OdgovornoLice = NullIfEmpty(odgLice),
            VrstaMagacina = VrstaIzNaziva(naziv)
        };
    }

    /// <summary>
    /// MAGACIN.DBF ima samo SIFRA i RACUNOPOL — nema polje za vrstu magacina, pa se ona čita
    /// iz naziva („Magacin maloprodaje", „Prodavnica br.1"). Ranije je svaki uvezeni magacin
    /// bio „Veleprodaja", zbog čega su maloprodajni dokumenti knjiženi na veleprodajna konta.
    /// Kad naziv ništa ne kaže, ostaje veleprodaja — to je bilo dotadašnje ponašanje.
    /// </summary>
    public static string VrstaIzNaziva(string naziv)
    {
        string n = naziv.ToLowerInvariant();
        bool maloprodaja = n.Contains("maloprodaj") || n.Contains("prodavnic") || n.Contains("malopr.");
        return maloprodaja ? "Maloprodaja" : "Veleprodaja";
    }

    /// <summary>ARTIKLI.DBF → Artikal (Robno). Vraća null ako red nema šifru.</summary>
    public static Artikal? MapArtikal(Dictionary<string, string> row)
    {
        string sifra = Get(row, "SIFRA", "KOD", "SIFR");
        if (string.IsNullOrWhiteSpace(sifra)) return null;

        string naziv = Get(row, "NAZIV", "IME", "OPIS", "ARTIKAL");
        string jm = Get(row, "JED_MERE", "JM", "JEDINICA");
        string selektovanStr = Get(row, "SELEKTOVAN").ToUpperInvariant();

        return new Artikal
        {
            SifraArtikla = sifra,
            Naziv = string.IsNullOrWhiteSpace(naziv) ? $"Artikal {sifra}" : naziv,
            JedinicaMere = string.IsNullOrWhiteSpace(jm) ? "kom" : jm,
            Pakovanje = NullIfEmpty(Get(row, "PAKOVANJE", "PAK")),
            TarifniBroj = NullIfEmpty(Get(row, "TAR_BROJ", "TARIFNI", "TAR_BR")),
            KlasifikacionaSifra = NullIfEmpty(Get(row, "KLAS_SIFRA", "KLASIFIKAC")),
            Selektovan = selektovanStr is "T" or "1" or "TRUE" or "Y"
        };
    }

    /// <summary>M_SIFR.DBF → Materijal (Materijalno, nezavisna šifarnička serija od ARTIKLI.DBF). Vraća null ako red nema šifru.</summary>
    public static Materijal? MapMaterijal(Dictionary<string, string> row)
    {
        string sifra = Get(row, "SIFRA", "KOD", "SIFR");
        if (string.IsNullOrWhiteSpace(sifra)) return null;

        string naziv = Get(row, "NAZIV", "IME", "OPIS", "ARTIKAL");
        string jm = Get(row, "JED_MERE", "JM", "JEDINICA");

        return new Materijal
        {
            SifraArtikla = sifra,
            Naziv = string.IsNullOrWhiteSpace(naziv) ? $"Materijal {sifra}" : naziv,
            JedinicaMere = string.IsNullOrWhiteSpace(jm) ? "kom" : jm,
            Pakovanje = NullIfEmpty(Get(row, "PAKOVANJE", "PAK"))
        };
    }

    /// <summary>TARIFE.DBF → PoreskaTarifa. Vraća null ako red nema važeći tarifni broj (TAR_BROJ).</summary>
    public static PoreskaTarifa? MapPoreskaTarifa(Dictionary<string, string> row)
    {
        string tarBrojStr = Get(row, "TAR_BROJ");
        if (!int.TryParse(tarBrojStr, out int tarBroj) || tarBroj <= 0) return null;

        string porUCeni = Get(row, "POR_U_CEN").ToUpperInvariant();

        return new PoreskaTarifa
        {
            TarifniBroj = tarBroj.ToString(CultureInfo.InvariantCulture),
            PorezProcenat = Math.Abs(ParseDecimal(Get(row, "POREZ_PR"))),
            PosebanPorezProcenat = Math.Abs(ParseDecimal(Get(row, "POS_P_PR"))),
            PorezUCeni = porUCeni == "DA"
        };
    }

    /// <summary>PROMENE.DBF → Promena. Vraća null ako red nema šifru (SIFRA) ili opis (PROMENA).</summary>
    public static Promena? MapPromena(Dictionary<string, string> row)
    {
        string sifraStr = Get(row, "SIFRA");
        string opis = Get(row, "PROMENA");
        if (!int.TryParse(sifraStr, out int sifra) || string.IsNullOrWhiteSpace(opis)) return null;

        return new Promena
        {
            Sifra = sifra,
            Opis = opis
        };
    }

    /// <summary>MAT_KART.DBF / M_KART.DBF → MaterijalnaKartica.</summary>
    public static MaterijalnaKartica? MapMaterijalnaKartica(Dictionary<string, string> row, int defaultRedniBroj = 1)
    {
        string mag = Get(row, "MAG", "MAGACIN", "SIFRA_MAG");
        string art = Get(row, "ARTIKAL", "SIFRA_ART", "ARTIKL");
        if (string.IsNullOrWhiteSpace(mag) || string.IsNullOrWhiteSpace(art)) return null;

        int.TryParse(Get(row, "R_BR", "RED_BROJ", "RED_BR"), out int redBr);
        if (redBr <= 0) redBr = defaultRedniBroj;

        return new MaterijalnaKartica
        {
            SifraMagacina = mag,
            SifraArtikla = art,
            RedniBroj = redBr,
            DatumPromene = ParseDate(Get(row, "DAT_PROMEN", "DAT_PROM", "DATUM", "DAT_PROMENE")),
            OpisPromene = NullIfEmpty(Get(row, "OPIS", "OPIS_PROM", "OPIS_PROMENE")),
            Ulaz = ParseDecimal(Get(row, "ULAZ")),
            Izlaz = ParseDecimal(Get(row, "IZLAZ")),
            Stanje = ParseDecimal(Get(row, "STANJE")),
            Cena = ParseDecimal(Get(row, "CENA", "CENA_UL")),
            CenaIzlaz = ParseDecimal(Get(row, "CENA_IZL", "CENA_IZLAZ")),
            Duguje = ParseDecimal(Get(row, "DUG", "DUGUJE")),
            Potrazuje = ParseDecimal(Get(row, "POT", "POTRAZUJE")),
            Saldo = ParseDecimal(Get(row, "SALDO"))
        };
    }

    /// <summary>Grupiše NALOG.DBF redove po broju naloga (BR_NALOGA), izbacuje prazne/nulte brojeve.</summary>
    public static List<(int BrojNaloga, List<Dictionary<string, string>> Redovi)> GroupNalogRows(List<Dictionary<string, string>> rows)
    {
        return rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA") })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture))
            .Select(g => (g.Key, g.Select(x => x.Row).ToList()))
            .ToList();
    }

    /// <summary>Grupa redova NALOG.DBF (isti BR_NALOGA) → Nalog sa stavkama.</summary>
    public static Nalog? MapNalogGrupa(int brojNaloga, List<Dictionary<string, string>> redovi, Dictionary<int, string>? promeneMap = null)
    {
        if (redovi.Count == 0) return null;

        var first = redovi[0];
        DateTime datum = ParseDate(Get(first, "DAT_NALOGA"));
        bool knjizen = Get(first, "KNJIZEN") == "1";
        string prviOpis = Get(first, "BR_DOKUM");

        var nalog = new Nalog
        {
            BrojNaloga = brojNaloga,
            DatumNaloga = datum,
            Opis = string.IsNullOrWhiteSpace(prviOpis) ? $"Nalog {brojNaloga}" : prviOpis,
            IsKnjizen = knjizen,
            DatumKnjiženja = knjizen ? datum : null
        };

        int rbFallback = 1;
        foreach (var row in redovi)
        {
            string konto = Get(row, "KONTO");
            string brDokum = Get(row, "BR_DOKUM");
            decimal dug = ParseDecimal(Get(row, "DUGUJE"));
            decimal pot = ParseDecimal(Get(row, "POTRAZUJE"));

            if (string.IsNullOrWhiteSpace(konto) && dug == 0 && pot == 0) continue;

            int.TryParse(Get(row, "RED_BROJ"), out int redBr);
            int.TryParse(Get(row, "PROMENA"), out int promena);

            string opisStavke;
            if (promena > 0 && promeneMap != null && promeneMap.TryGetValue(promena, out var textIzPromene) && !string.IsNullOrWhiteSpace(textIzPromene))
            {
                opisStavke = textIzPromene;
            }
            else
            {
                opisStavke = string.IsNullOrWhiteSpace(brDokum) ? nalog.Opis : brDokum;
            }

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = redBr > 0 ? redBr : rbFallback,
                BrojKonta = konto,
                BrojDokumenta = NullIfEmpty(brDokum),
                Opis = opisStavke,
                Duguje = dug,
                Potrazuje = pot,
                StariKonto = NullIfEmpty(Get(row, "ST_KON")),
                PromenaKod = promena > 0 ? promena : null
            });
            rbFallback++;
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);
        return nalog;
    }

    /// <summary>KALKULAC.DBF → Kalkulacija.</summary>
    public static Kalkulacija? MapKalkulacija(Dictionary<string, string> row)
    {
        string broj = Get(row, "BR_KALKUL", "BR_KAL", "BROJ", "BR_NALOGA").Trim();
        if (string.IsNullOrWhiteSpace(broj) || broj == "0" || broj.TrimStart('0') == "" || !int.TryParse(broj, out int brojKalk)) return null;

        decimal svegaNab = ParseDecimal(Get(row, "SVEGA_NAB", "NABAVNA"));
        decimal razlika = ParseDecimal(Get(row, "RAZLIKA", "RUC"));
        decimal porez = ParseDecimal(Get(row, "POREZ", "PDV"));

        return new Kalkulacija
        {
            BrojKalkulacije = brojKalk,
            Datum = ParseDate(Get(row, "DATUM", "DAT_KAL")),
            SifraDobavljaca = NullIfEmpty(Get(row, "DOBAVLJAC", "KUPAC", "KONTO")),
            BrojOtpremnice = NullIfEmpty(Get(row, "OTPREM_BR", "BR_OTP", "OTPREMNICA")),
            DatumOtpremnice = ParseDateOrNull(Get(row, "OTPREM_DAT", "DAT_OTP")),
            BrojRacuna = NullIfEmpty(Get(row, "RACUN_BR", "BR_RAC", "RACUN")),
            DatumRacuna = ParseDateOrNull(Get(row, "RACUN_DAT", "DAT_RAC")),
            NabavnaVrednost = ParseDecimal(Get(row, "NABAVNA", "NABAV_VRED", "NAB_VRED")),
            TransportniTroskovi = ParseDecimal(Get(row, "TRANS_TROS", "TRANSP_TRO", "TROSKOVI")),
            TroskoviUskladistenja = ParseDecimal(Get(row, "TROS_USKL")),
            UtovarIstovar = ParseDecimal(Get(row, "UTOV_ISTOV")),
            TransportnoOsiguranje = ParseDecimal(Get(row, "TR_OSIGUR")),
            OstaliTroskovi = ParseDecimal(Get(row, "OSTALI")),
            SvegaTroskovi = ParseDecimal(Get(row, "TROSKOVI", "SVEGA_TROS")),
            SvegaNabavno = svegaNab,
            Razlika = razlika,
            // KALKULAC.DBF ne čuva procente — legacy ih drži samo po stavci (KAL_NAL.RAZLIKA_PR /
            // POREZ_PR). Zbirni procenat se izvodi iz iznosa istim formulama kao MAT6.PRG:855/873.
            MarzaProcenat = ProcenatOd(razlika, svegaNab),
            Porez = porez,
            PoreskaStopaProcenat = ProcenatOd(porez, svegaNab + razlika),
            ProdajnaVrednost = ParseDecimal(Get(row, "PRODAJNA", "PROD_VRED")),
            SifraMagacina = NullIfEmpty(Get(row, "MAG_PRIMA", "MAGACIN", "MAG")),
            IsKnjizen = Get(row, "KNJIZEN") == "1"
        };
    }

    /// <summary>100 * deo / osnovica, zaokruženo na 4 decimale; 0 kad je osnovica nula.</summary>
    private static decimal ProcenatOd(decimal deo, decimal osnovica)
        => osnovica == 0 ? 0m : Math.Round(100m * deo / osnovica, 4);

    /// <summary>
    /// KAL_NAL.DBF → KalkulacijaStavka. Stvarna imena kolona (potvrđena na
    /// C:\FIRME\ARHSTO\Radni\kor01\KAL_NAL.DBF): BR_KALKUL, DATUM, MAG_PRIMA, RED_BROJ, ARTIKAL,
    /// KOLICINA, CENA, IZNOS, TROSKOVI, NABAVNA, RAZLIKA_PR, RAZLIKA_IZ, PROD_BEZ_P, POREZ_PR,
    /// POREZ_IZ, POS_P_PR, POS_P_IZ, PREN_POR, PREN_P_POR, PROD_SA_P, PROD_PO_JM, KNJIZEN,
    /// STARA_CENA, POR_ZA_UPL.
    /// </summary>
    public static KalkulacijaStavka? MapKalkulacijaStavka(Dictionary<string, string> row, int defaultRedniBroj = 1)
    {
        string art = Get(row, "ARTIKAL", "SIFRA", "SIFRA_ART");
        if (string.IsNullOrWhiteSpace(art)) return null;

        int.TryParse(Get(row, "RED_BROJ", "RBR", "R_BR"), out int rbr);
        if (rbr <= 0) rbr = defaultRedniBroj;

        decimal kol = ParseDecimal(Get(row, "KOLICINA", "KOL"));
        decimal cena = ParseDecimal(Get(row, "CENA", "NAB_CENA"));
        decimal iznos = ParseDecimal(Get(row, "IZNOS", "NAB_VRED"));
        if (iznos == 0 && kol != 0 && cena != 0) iznos = kol * cena;

        decimal nabavna = ParseDecimal(Get(row, "NABAVNA", "NAB_VRED"));
        if (nabavna == 0) nabavna = iznos + ParseDecimal(Get(row, "TROSKOVI", "TROS"));

        decimal razlikaIz = ParseDecimal(Get(row, "RAZLIKA_IZ", "RAZLIKA", "RUC"));
        decimal porezIz = ParseDecimal(Get(row, "POREZ_IZ", "POREZ", "PDV"));

        // PROD_SA_P je prodajna vrednost sa porezom; PROD_BEZ_P je bez poreza.
        decimal prodSaP = ParseDecimal(Get(row, "PROD_SA_P", "PROD_VRED", "PRODAJNA"));
        decimal prodBezP = ParseDecimal(Get(row, "PROD_BEZ_P"));
        if (prodBezP == 0) prodBezP = nabavna + razlikaIz;
        if (prodSaP == 0) prodSaP = prodBezP + porezIz;

        decimal prodPoJm = ParseDecimal(Get(row, "PROD_PO_JM", "PROD_CENA", "CENA_PROD"));
        if (prodPoJm == 0 && kol != 0) prodPoJm = prodSaP / kol;

        decimal prenPor = ParseDecimal(Get(row, "PREN_POR"));
        decimal porZaUpl = ParseDecimal(Get(row, "POR_ZA_UPL"));
        if (porZaUpl == 0) porZaUpl = porezIz - prenPor;

        return new KalkulacijaStavka
        {
            RedniBroj = rbr,
            SifraArtikla = art,
            Kolicina = kol,
            NabavnaCena = cena,
            Iznos = iznos,
            Troskovi = ParseDecimal(Get(row, "TROSKOVI", "TROS")),
            NabavnaVrednost = nabavna,
            RazlikaProcenat = ParseDecimal(Get(row, "RAZLIKA_PR")),
            RazlikaIznos = razlikaIz,
            ProdajnaVrednostBezPoreza = prodBezP,
            PorezProcenat = ParseDecimal(Get(row, "POREZ_PR")),
            PorezIznos = porezIz,
            PosebanPorezProcenat = ParseDecimal(Get(row, "POS_P_PR")),
            PosebanPorezIznos = ParseDecimal(Get(row, "POS_P_IZ")),
            PrenetiPorez = prenPor,
            PrenetiPosebanPorez = ParseDecimal(Get(row, "PREN_P_POR")),
            PorezZaUplatu = porZaUpl,
            ProdajnaVrednost = prodSaP,
            ProdajnaCena = prodPoJm,
            StaraCena = ParseDecimal(Get(row, "STARA_CENA")),
            IsKnjizen = Get(row, "KNJIZEN") is "1" or "T" or "TRUE" or "Y"
        };
    }

    /// <summary>Grupiše KAL_NAL.DBF / MAL_NAL.DBF redove po broju kalkulacije. Red 0 je legacy brojač zapisa, ne stavka.</summary>
    public static Dictionary<int, List<Dictionary<string, string>>> GroupKalkulacijaStavke(List<Dictionary<string, string>> rows)
    {
        return rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_KALKUL", "BR_KAL", "BR_NALOGA", "BROJ") })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && int.TryParse(x.Broj, out int b) && b > 0)
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Row).ToList());
    }

    /// <summary>
    /// MALKULAC.DBF → MaloprodajnaKalkulacija. Kolone: PRODAVNICA, BR_KALKUL, DATUM, MAG_PRIMA,
    /// MAG_DAJE, DOBAVLJAC, OTPREM_BR, OTPREM_DAT, RACUN_BR, RACUN_DAT, TRANS_TROS, TROS_USKL,
    /// UTOV_ISTOV, TR_OSIGUR, OSTALI, KNJIZEN, T_KNJIZEN, SVEGA_TROS, RABAT_PR, NAB_VRED,
    /// SVEGA_NAB, RAZLIKA, POREZ, PROD_VRED, RABAT_IZ.
    /// </summary>
    public static MaloprodajnaKalkulacija? MapMaloprodajnaKalkulacija(Dictionary<string, string> row)
    {
        string broj = Get(row, "BR_KALKUL", "BR_KAL", "BROJ", "BR_NALOGA").Trim();
        if (string.IsNullOrWhiteSpace(broj) || broj == "0" || broj.TrimStart('0') == "" || !int.TryParse(broj, out int brojKalk)) return null;

        int.TryParse(Get(row, "PRODAVNICA"), out int prodavnica);

        decimal svegaNab = ParseDecimal(Get(row, "SVEGA_NAB"));
        decimal razlika = ParseDecimal(Get(row, "RAZLIKA"));
        decimal porez = ParseDecimal(Get(row, "POREZ"));

        return new MaloprodajnaKalkulacija
        {
            SifraProdavnice = prodavnica,
            BrojKalkulacije = brojKalk,
            Datum = ParseDate(Get(row, "DATUM", "DAT_KAL")),
            SifraMagacinaPrima = NullIfEmpty(Get(row, "MAG_PRIMA")),
            SifraMagacinaDaje = NullIfEmpty(Get(row, "MAG_DAJE")),
            SifraDobavljaca = NullIfEmpty(Get(row, "DOBAVLJAC", "KONTO")),
            BrojOtpremnice = NullIfEmpty(Get(row, "OTPREM_BR")),
            DatumOtpremnice = ParseDateOrNull(Get(row, "OTPREM_DAT")),
            BrojRacuna = NullIfEmpty(Get(row, "RACUN_BR")),
            DatumRacuna = ParseDateOrNull(Get(row, "RACUN_DAT")),
            TransportniTroskovi = ParseDecimal(Get(row, "TRANS_TROS")),
            TroskoviUskladistenja = ParseDecimal(Get(row, "TROS_USKL")),
            UtovarIstovar = ParseDecimal(Get(row, "UTOV_ISTOV")),
            TransportnoOsiguranje = ParseDecimal(Get(row, "TR_OSIGUR")),
            OstaliTroskovi = ParseDecimal(Get(row, "OSTALI")),
            SvegaTroskovi = ParseDecimal(Get(row, "SVEGA_TROS")),
            RabatPri = ParseDecimal(Get(row, "RABAT_PR")),
            NabavnaVrednost = ParseDecimal(Get(row, "NAB_VRED")),
            SvegaNabavno = svegaNab,
            Razlika = razlika,
            // MALKULAC.DBF, kao i KALKULAC.DBF, ne čuva procente — izvode se iz iznosa.
            MarzaProcenat = ProcenatOd(razlika, svegaNab),
            Porez = porez,
            PoreskaStopaProcenat = ProcenatOd(porez, svegaNab + razlika),
            ProdajnaVrednost = ParseDecimal(Get(row, "PROD_VRED")),
            RabatIznos = ParseDecimal(Get(row, "RABAT_IZ")),
            IsKnjizen = Get(row, "KNJIZEN") is "1" or "T" or "TRUE" or "Y",
            IsTrgovinskiKnjizen = Get(row, "T_KNJIZEN") is "1" or "T" or "TRUE" or "Y"
        };
    }

    /// <summary>
    /// Legacy zaglavlje ume da ostavi zbirove na nuli iako stavke postoje (u ARHSTO\kor03 to je
    /// slučaj kod 22 od 409 maloprodajnih kalkulacija). Dopunjuje isključivo polja koja su nula,
    /// da dokument u pregledu ne bi izgledao prazan; popunjena legacy zaglavlja ostaju netaknuta.
    /// </summary>
    public static void DopuniZbiroveIzStavki(Kalkulacija kalkulacija)
    {
        if (kalkulacija.Stavke.Count == 0) return;

        if (kalkulacija.NabavnaVrednost == 0) kalkulacija.NabavnaVrednost = kalkulacija.Stavke.Sum(s => s.Iznos);
        if (kalkulacija.SvegaTroskovi == 0) kalkulacija.SvegaTroskovi = kalkulacija.Stavke.Sum(s => s.Troskovi);
        if (kalkulacija.SvegaNabavno == 0) kalkulacija.SvegaNabavno = kalkulacija.Stavke.Sum(s => s.NabavnaVrednost);
        if (kalkulacija.Razlika == 0) kalkulacija.Razlika = kalkulacija.Stavke.Sum(s => s.RazlikaIznos);
        if (kalkulacija.Porez == 0) kalkulacija.Porez = kalkulacija.Stavke.Sum(s => s.PorezIznos);
        if (kalkulacija.ProdajnaVrednost == 0) kalkulacija.ProdajnaVrednost = kalkulacija.Stavke.Sum(s => s.ProdajnaVrednost);
        if (kalkulacija.MarzaProcenat == 0) kalkulacija.MarzaProcenat = ProcenatOd(kalkulacija.Razlika, kalkulacija.SvegaNabavno);
        if (kalkulacija.PoreskaStopaProcenat == 0) kalkulacija.PoreskaStopaProcenat = ProcenatOd(kalkulacija.Porez, kalkulacija.SvegaNabavno + kalkulacija.Razlika);
    }

    /// <inheritdoc cref="DopuniZbiroveIzStavki(Kalkulacija)"/>
    public static void DopuniZbiroveIzStavki(MaloprodajnaKalkulacija kalkulacija)
    {
        if (kalkulacija.Stavke.Count == 0) return;

        if (kalkulacija.NabavnaVrednost == 0) kalkulacija.NabavnaVrednost = kalkulacija.Stavke.Sum(s => s.Iznos);
        if (kalkulacija.SvegaTroskovi == 0) kalkulacija.SvegaTroskovi = kalkulacija.Stavke.Sum(s => s.Troskovi);
        if (kalkulacija.SvegaNabavno == 0) kalkulacija.SvegaNabavno = kalkulacija.Stavke.Sum(s => s.NabavnaVrednost);
        if (kalkulacija.Razlika == 0) kalkulacija.Razlika = kalkulacija.Stavke.Sum(s => s.RazlikaIznos);
        if (kalkulacija.Porez == 0) kalkulacija.Porez = kalkulacija.Stavke.Sum(s => s.PorezIznos);
        if (kalkulacija.ProdajnaVrednost == 0) kalkulacija.ProdajnaVrednost = kalkulacija.Stavke.Sum(s => s.ProdajnaVrednost);
        if (kalkulacija.MarzaProcenat == 0) kalkulacija.MarzaProcenat = ProcenatOd(kalkulacija.Razlika, kalkulacija.SvegaNabavno);
        if (kalkulacija.PoreskaStopaProcenat == 0) kalkulacija.PoreskaStopaProcenat = ProcenatOd(kalkulacija.Porez, kalkulacija.SvegaNabavno + kalkulacija.Razlika);
    }

    /// <summary>
    /// Grupiše MAL_NAL.DBF redove po (PRODAVNICA, BR_KALKUL). Za razliku od veleprodaje, broj
    /// maloprodajne kalkulacije je jedinstven samo u okviru prodavnice, pa je ključ složen.
    /// </summary>
    public static Dictionary<(int Prodavnica, int Broj), List<Dictionary<string, string>>> GroupMaloprodajnaKalkulacijaStavke(List<Dictionary<string, string>> rows)
    {
        return rows
            .Select(r =>
            {
                int.TryParse(Get(r, "PRODAVNICA"), out int prod);
                bool ok = int.TryParse(Get(r, "BR_KALKUL", "BR_KAL", "BROJ", "BR_NALOGA"), out int broj);
                return new { Row = r, Prodavnica = prod, Broj = broj, Ok = ok && broj > 0 };
            })
            .Where(x => x.Ok)
            .GroupBy(x => (x.Prodavnica, x.Broj))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Row).ToList());
    }

    /// <summary>
    /// MAL_NAL.DBF → MaloprodajnaKalkulacijaStavka. Ista jezgra kolona kao KAL_NAL, uz maloprodajne
    /// dodatke: POS_POR_PR (umesto PREN_P_POR), NAZ_ROBE, JED_MERE, TARIFNI, TAKSA, BR_RAZDUZ, T_KNJIZEN.
    /// </summary>
    public static MaloprodajnaKalkulacijaStavka? MapMaloprodajnaKalkulacijaStavka(Dictionary<string, string> row, int defaultRedniBroj = 1)
    {
        string art = Get(row, "ARTIKAL", "SIFRA", "SIFRA_ART");
        if (string.IsNullOrWhiteSpace(art)) return null;

        int.TryParse(Get(row, "RED_BROJ", "RBR", "R_BR"), out int rbr);
        if (rbr <= 0) rbr = defaultRedniBroj;

        decimal kol = ParseDecimal(Get(row, "KOLICINA", "KOL"));
        decimal cena = ParseDecimal(Get(row, "CENA", "NAB_CENA"));
        decimal iznos = ParseDecimal(Get(row, "IZNOS", "NAB_VRED"));
        if (iznos == 0 && kol != 0 && cena != 0) iznos = kol * cena;

        decimal nabavna = ParseDecimal(Get(row, "NABAVNA", "NAB_VRED"));
        if (nabavna == 0) nabavna = iznos + ParseDecimal(Get(row, "TROSKOVI", "TROS"));

        decimal razlikaIz = ParseDecimal(Get(row, "RAZLIKA_IZ", "RAZLIKA", "RUC"));
        decimal porezIz = ParseDecimal(Get(row, "POREZ_IZ", "POREZ", "PDV"));

        decimal prodSaP = ParseDecimal(Get(row, "PROD_SA_P", "PROD_VRED", "PRODAJNA"));
        decimal prodBezP = ParseDecimal(Get(row, "PROD_BEZ_P"));
        if (prodBezP == 0) prodBezP = nabavna + razlikaIz;
        if (prodSaP == 0) prodSaP = prodBezP + porezIz;

        decimal prodPoJm = ParseDecimal(Get(row, "PROD_PO_JM", "PROD_CENA", "CENA_PROD"));
        if (prodPoJm == 0 && kol != 0) prodPoJm = prodSaP / kol;

        decimal prenPor = ParseDecimal(Get(row, "PREN_POR"));
        decimal porZaUpl = ParseDecimal(Get(row, "POR_ZA_UPL"));
        if (porZaUpl == 0) porZaUpl = porezIz - prenPor;

        int.TryParse(Get(row, "BR_RAZDUZ"), out int brRazduz);
        string tarifni = Get(row, "TARIFNI");

        return new MaloprodajnaKalkulacijaStavka
        {
            RedniBroj = rbr,
            SifraArtikla = art,
            Kolicina = kol,
            NabavnaCena = cena,
            Iznos = iznos,
            Troskovi = ParseDecimal(Get(row, "TROSKOVI", "TROS")),
            NabavnaVrednost = nabavna,
            RazlikaProcenat = ParseDecimal(Get(row, "RAZLIKA_PR")),
            RazlikaIznos = razlikaIz,
            ProdajnaVrednostBezPoreza = prodBezP,
            PorezProcenat = ParseDecimal(Get(row, "POREZ_PR")),
            PorezIznos = porezIz,
            PosebanPorezProcenat = ParseDecimal(Get(row, "POS_P_PR")),
            PosebanPorezIznos = ParseDecimal(Get(row, "POS_P_IZ")),
            PrenetiPorez = prenPor,
            PrenetiPosebanPorez = ParseDecimal(Get(row, "POS_POR_PR", "PREN_P_POR")),
            PorezZaUplatu = porZaUpl,
            Taksa = ParseDecimal(Get(row, "TAKSA")),
            ProdajnaVrednost = prodSaP,
            ProdajnaCena = prodPoJm,
            TarifniBroj = (!string.IsNullOrWhiteSpace(tarifni) && tarifni != "0") ? tarifni : null,
            BrojRazduzenja = brRazduz > 0 ? brRazduz : null,
            IsKnjizen = Get(row, "KNJIZEN") is "1" or "T" or "TRUE" or "Y",
            IsTrgovinskiKnjizen = Get(row, "T_KNJIZEN") is "1" or "T" or "TRUE" or "Y",
            NazivArtikla = NullIfEmpty(Get(row, "NAZ_ROBE")),
            JedinicaMere = NullIfEmpty(Get(row, "JED_MERE"))
        };
    }

    /// <summary>MAT_NAL.DBF / ZADUZ.DBF / RAZDUZ.DBF → PrimopredajaNalog i stavke.</summary>
    public static List<PrimopredajaNalog> MapPrimopredajaNalozi(List<Dictionary<string, string>> rows, string vrstaDokumenta = "Primopredaja")
    {
        var result = new List<PrimopredajaNalog>();

        var grouped = rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA", "BR_NAL", "BROJ").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture));

        foreach (var group in grouped)
        {
            var firstRow = group.First();
            int brNaloga = group.Key;
            string magDaje = Get(firstRow.Row, "MAG_DAJE", "MAG_IZ", "MAGACIN", "MAG");
            string magPrima = Get(firstRow.Row, "MAG_PRIMA", "MAG_U", "KORISNIK", "KONTO");
            string knjiStr = Get(firstRow.Row, "KNJIZEN", "KNJIZ");
            DateTime datum = ParseDate(Get(firstRow.Row, "DATUMOGA", "DATUM", "DAT_NALOGA"));

            var nalog = new PrimopredajaNalog
            {
                BrojNaloga = brNaloga,
                Datum = datum,
                SifraMagacinaDaje = magDaje,
                SifraMagacinaPrima = magPrima,
                VrstaDokumenta = vrstaDokumenta,
                IsKnjizen = knjiStr is "T" or "1" or "TRUE" or "Y"
            };

            foreach (var r in group)
            {
                string art = Get(r.Row, "ARTIKAL", "SIFRA", "ART");
                if (string.IsNullOrWhiteSpace(art)) continue;

                int.TryParse(Get(r.Row, "RED_BROJ", "RBR"), out int rbr);
                decimal kol = ParseDecimal(Get(r.Row, "KOLICINA", "KOL"));
                decimal cena = ParseDecimal(Get(r.Row, "CENAINA", "CENA"));
                decimal iznos = ParseDecimal(Get(r.Row, "IZNOSNA", "IZNOS"));

                nalog.Stavke.Add(new PrimopredajaStavka
                {
                    RedniBroj = rbr > 0 ? rbr : nalog.Stavke.Count + 1,
                    SifraArtikla = art,
                    Kolicina = kol,
                    Cena = cena,
                    Iznos = iznos > 0 ? iznos : kol * cena
                });
            }

            if (nalog.Stavke.Count > 0)
            {
                result.Add(nalog);
            }
        }

        return result;
    }

    /// <summary>RAC_OTP.DBF i RAC_POD.DBF → RacunOtpremnica i stavke.</summary>
    public static List<RacunOtpremnica> MapRacunOtpremnice(
        List<Dictionary<string, string>> racOtpRows,
        List<Dictionary<string, string>> racPodRows,
        Dictionary<string, int>? magaciniMap = null,
        Dictionary<string, int>? artikliMap = null)
    {
        var result = new List<RacunOtpremnica>();

        var podMap = racPodRows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA", "BR_NAL", "BROJ").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture))
            .ToDictionary(g => g.Key, g => g.First().Row);

        var grouped = racOtpRows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA", "BR_NAL", "BROJ").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture));

        foreach (var group in grouped)
        {
            var firstRow = group.First().Row;
            int brNaloga = group.Key;
            DateTime datum = ParseDate(Get(firstRow, "DATUMOGA", "DATUM"));
            string magDaje = Get(firstRow, "MAG_DAJE", "MAGACIN");
            string kontoKupca = Get(firstRow, "KONTOZNOS", "KONTO", "KUPAC");
            string knjiStr = Get(firstRow, "KNJIZENOS", "KNJIZEN");

            int rokDana = 0;
            string brOtprem = brNaloga.ToString(CultureInfo.InvariantCulture);
            if (podMap.TryGetValue(brNaloga, out var podRow))
            {
                int.TryParse(Get(podRow, "ROKALOGA", "ROK"), out rokDana);
                string o = Get(podRow, "BR_OTPREM", "OTPREMNICA");
                if (!string.IsNullOrWhiteSpace(o)) brOtprem = o;
            }

            int? magId = null;
            if (magaciniMap != null && !string.IsNullOrWhiteSpace(magDaje) && magaciniMap.TryGetValue(magDaje, out int mId))
            {
                magId = mId;
            }

            var racun = new RacunOtpremnica
            {
                BrojRacuna = brNaloga,
                BrojOtpremnice = brOtprem,
                DatumRacuna = datum,
                DatumOtpremnice = datum,
                KontoKupca = kontoKupca,
                RokPlacanjaDana = rokDana,
                MagacinId = magId,
                IsKnjizen = knjiStr is "T" or "1" or "TRUE" or "Y"
            };

            decimal svegaBezPdv = 0m;
            decimal svegaPdv = 0m;
            decimal svegaUkupno = 0m;

            foreach (var r in group)
            {
                string art = Get(r.Row, "ARTIKAL", "SIFRA", "ART");
                if (string.IsNullOrWhiteSpace(art)) continue;

                int.TryParse(Get(r.Row, "RED_BROJ", "RBR"), out int rbr);
                decimal kol = ParseDecimal(Get(r.Row, "KOLICINA", "KOL"));
                decimal cena = ParseDecimal(Get(r.Row, "CENAINA", "CENA"));
                decimal iznBezPdv = ParseDecimal(Get(r.Row, "IZN_BEZ_RA", "IZNOS_CEN", "IZNOS"));
                if (iznBezPdv == 0 && kol != 0 && cena != 0) iznBezPdv = kol * cena;

                decimal rabatPct = ParseDecimal(Get(r.Row, "RABAT_CEN", "RABAT"));
                decimal pdvPct = ParseDecimal(Get(r.Row, "POREZ_PRA", "POREZ_PR"));
                decimal pdvIznos = ParseDecimal(Get(r.Row, "POREZ_IZA", "POREZ_IZN"));
                if (pdvIznos == 0 && pdvPct > 0) pdvIznos = Math.Round(iznBezPdv * (pdvPct / 100m), 2);

                decimal ukupanIznos = ParseDecimal(Get(r.Row, "UKUP_IZNOS", "UKUPNO"));
                if (ukupanIznos == 0) ukupanIznos = iznBezPdv + pdvIznos;

                svegaBezPdv += iznBezPdv;
                svegaPdv += pdvIznos;
                svegaUkupno += ukupanIznos;

                int? aId = null;
                if (artikliMap != null && artikliMap.TryGetValue(art, out int idVal))
                {
                    aId = idVal;
                }

                racun.Stavke.Add(new RacunOtpremnicaStavka
                {
                    RedniBroj = rbr > 0 ? rbr : racun.Stavke.Count + 1,
                    SifraArtikla = art,
                    ArtikalId = aId,
                    Kolicina = kol,
                    Cena = cena,
                    RabatProcenat = rabatPct,
                    PdvProcenat = pdvPct,
                    IznosBezPdv = iznBezPdv,
                    PdvIznos = pdvIznos,
                    UkupanIznos = ukupanIznos
                });
            }

            racun.IznosBezPdv = svegaBezPdv;
            racun.PdvIznos = svegaPdv;
            racun.UkupanIznos = svegaUkupno;

            if (racun.Stavke.Count > 0)
            {
                result.Add(racun);
            }
        }

        return result;
    }

    /// <summary>NIV_NAL.DBF i P_M_NIV.DBF → NivelacijaCena i stavke.</summary>
    public static List<NivelacijaCena> MapNivelacijeCena(
        List<Dictionary<string, string>> nivNalRows,
        List<Dictionary<string, string>> pmNivRows,
        Dictionary<string, int>? magaciniMap = null,
        Dictionary<string, int>? artikliMap = null)
    {
        var result = new List<NivelacijaCena>();

        var allRows = nivNalRows.Concat(pmNivRows).ToList();

        var grouped = allRows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA", "BR_NIV", "BR_KALKUL", "BROJ").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture));

        foreach (var group in grouped)
        {
            var firstRow = group.First().Row;
            int brNivelacije = group.Key;
            DateTime datum = ParseDate(Get(firstRow, "DATUMOGA", "DATUM", "DAT_NIV"));
            string magSifra = Get(firstRow, "MAGACIN", "MAG", "MAG_DAJE");
            string opis = Get(firstRow, "OPIS", "NAPOMENA");
            string knjiStr = Get(firstRow, "KNJIZENOS", "KNJIZEN", "KNJIZ");

            int? magId = null;
            if (magaciniMap != null && !string.IsNullOrWhiteSpace(magSifra) && magaciniMap.TryGetValue(magSifra, out int mId))
            {
                magId = mId;
            }

            var niv = new NivelacijaCena
            {
                BrojNivelacije = brNivelacije,
                DatumNivelacije = datum,
                SifraMagacina = magSifra,
                MagacinId = magId,
                Opis = NullIfEmpty(opis),
                IsKnjizen = knjiStr is "T" or "1" or "TRUE" or "Y"
            };

            decimal ukupnaRazlikaNiv = 0m;

            foreach (var r in group)
            {
                string art = Get(r.Row, "ARTIKAL", "SIFRA", "ART");
                if (string.IsNullOrWhiteSpace(art)) continue;

                int.TryParse(Get(r.Row, "RED_BROJ", "RBR"), out int rbr);
                decimal kol = ParseDecimal(Get(r.Row, "KOLICINA", "KOL"));
                decimal staraCena = ParseDecimal(Get(r.Row, "STARA_CENA", "CENA", "CENA_STARA"));
                decimal novaCena = ParseDecimal(Get(r.Row, "NOVA_CENA", "N_CENA", "CENA_NOVA"));
                decimal razlikaPoJed = ParseDecimal(Get(r.Row, "RAZLIKA_C", "RAZ_CENA"));
                if (razlikaPoJed == 0 && (staraCena != 0 || novaCena != 0)) razlikaPoJed = novaCena - staraCena;

                decimal ukupnaRazlikaStavke = ParseDecimal(Get(r.Row, "RAZLIKA_IZ", "N_IZNOS", "RAZLIKA"));
                if (ukupnaRazlikaStavke == 0 && kol != 0) ukupnaRazlikaStavke = kol * razlikaPoJed;

                ukupnaRazlikaNiv += ukupnaRazlikaStavke;

                int? aId = null;
                if (artikliMap != null && artikliMap.TryGetValue(art, out int idVal))
                {
                    aId = idVal;
                }

                niv.Stavke.Add(new NivelacijaStavka
                {
                    RedniBroj = rbr > 0 ? rbr : niv.Stavke.Count + 1,
                    SifraArtikla = art,
                    ArtikalId = aId,
                    KolicinaZaliha = kol,
                    StaraCena = staraCena,
                    NovaCena = novaCena,
                    RazlikaPoJedinici = razlikaPoJed,
                    UkupnaRazlika = ukupnaRazlikaStavke
                });
            }

            niv.UkupnoRazlika = ukupnaRazlikaNiv;

            if (niv.Stavke.Count > 0)
            {
                result.Add(niv);
            }
        }

        return result;
    }

    /// <summary>ULAZ.DBF → UlazNalog i stavke, grupisano po broju naloga (BR_NALOGA).</summary>
    public static List<UlazNalog> MapUlazNalozi(List<Dictionary<string, string>> rows)
    {
        var result = new List<UlazNalog>();

        var grouped = rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture));

        foreach (var group in grouped)
        {
            var firstRow = group.First().Row;
            string mag = Get(firstRow, "MAG_PRIMA", "MAGACIN", "MAG");
            string knjiStr = Get(firstRow, "KNJIZEN");
            string datRacunaStr = Get(firstRow, "DAT_RACUNA");

            var nalog = new UlazNalog
            {
                BrojNaloga = group.Key,
                Datum = ParseDate(Get(firstRow, "DATUM")),
                SifraMagacina = mag,
                BrojRacuna = NullIfEmpty(Get(firstRow, "BR_RACUNA")),
                DatumRacuna = string.IsNullOrWhiteSpace(datRacunaStr) ? null : ParseDate(datRacunaStr),
                IsKnjizen = knjiStr is "T" or "1" or "TRUE" or "Y"
            };

            foreach (var r in group)
            {
                string art = Get(r.Row, "ARTIKAL", "SIFRA", "ART");
                if (string.IsNullOrWhiteSpace(art)) continue;

                int.TryParse(Get(r.Row, "RED_BROJ", "RBR"), out int rbr);
                decimal kol = ParseDecimal(Get(r.Row, "KOLICINA", "KOL"));
                decimal cena = ParseDecimal(Get(r.Row, "CENA"));
                decimal iznos = ParseDecimal(Get(r.Row, "IZNOS"));
                if (iznos == 0 && kol != 0 && cena != 0) iznos = kol * cena;

                nalog.Stavke.Add(new UlazStavka
                {
                    RedniBroj = rbr > 0 ? rbr : nalog.Stavke.Count + 1,
                    SifraArtikla = art,
                    Kolicina = kol,
                    Cena = cena,
                    Iznos = iznos
                });
            }

            if (nalog.Stavke.Count > 0)
            {
                result.Add(nalog);
            }
        }

        return result;
    }

    /// <summary>TREBOV.DBF → TrebovanjeNalog i stavke, grupisano po broju naloga (BR_NALOGA).</summary>
    public static List<TrebovanjeNalog> MapTrebovanjeNalozi(List<Dictionary<string, string>> rows)
    {
        var result = new List<TrebovanjeNalog>();

        var grouped = rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "" && int.TryParse(x.Broj, out _))
            .GroupBy(x => int.Parse(x.Broj, CultureInfo.InvariantCulture));

        foreach (var group in grouped)
        {
            var firstRow = group.First().Row;
            string mag = Get(firstRow, "MAG_DAJE", "MAGACIN", "MAG");
            string knjiStr = Get(firstRow, "KNJIZEN");

            var nalog = new TrebovanjeNalog
            {
                BrojNaloga = group.Key,
                Datum = ParseDate(Get(firstRow, "DATUM")),
                SifraMagacina = mag,
                IsKnjizen = knjiStr is "T" or "1" or "TRUE" or "Y"
            };

            foreach (var r in group)
            {
                string art = Get(r.Row, "ARTIKAL", "SIFRA", "ART");
                if (string.IsNullOrWhiteSpace(art)) continue;

                int.TryParse(Get(r.Row, "RED_BROJ", "RBR"), out int rbr);
                decimal kol = ParseDecimal(Get(r.Row, "KOLICINA", "KOL"));
                decimal cena = ParseDecimal(Get(r.Row, "CENA"));
                decimal iznos = ParseDecimal(Get(r.Row, "IZNOS"));
                if (iznos == 0 && kol != 0 && cena != 0) iznos = kol * cena;
                string konto = Get(r.Row, "KONTO");

                nalog.Stavke.Add(new TrebovanjeStavka
                {
                    RedniBroj = rbr > 0 ? rbr : nalog.Stavke.Count + 1,
                    SifraArtikla = art,
                    Kolicina = kol,
                    Cena = cena,
                    Iznos = iznos,
                    KontoTroska = (!string.IsNullOrWhiteSpace(konto) && konto != "0") ? konto : null
                });
            }

            if (nalog.Stavke.Count > 0)
            {
                result.Add(nalog);
            }
        }

        return result;
    }

    private static decimal ParseDecimal(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return 0m;
        return decimal.TryParse(str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val) ? val : 0m;
    }

    // Legacy Clipper datumi dolaze kao "d.M.yyyy. H:mm:ss" (npr. "13.2.2002. 00:00:00").
    // Generički DateTime.TryParse pod InvariantCulture čita ovo kao M.d.yyyy (pogrešno
    // zamenjuje dan i mesec za dane <=12, a potpuno ne uspeva za dane >12), pa se prvo
    // pokušava tačan legacy format.
    private static readonly string[] LegacyDbfDateFormats =
    {
        "d.M.yyyy. H:mm:ss",
        "d.M.yyyy.",
        "d.M.yyyy H:mm:ss",
        "d.M.yyyy"
    };

    /// <summary>Za opciona datumska polja (otpremnica, račun) — prazan legacy datum ostaje null umesto da postane danas.</summary>
    private static DateTime? ParseDateOrNull(string str)
        => string.IsNullOrWhiteSpace(str) ? null : ParseDate(str);

    private static DateTime ParseDate(string str)
    {
        if (str.Length == 8 && DateTime.TryParseExact(str, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            return dt;
        if (DateTime.TryParseExact(str, LegacyDbfDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dtExact))
            return dtExact;
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt2))
            return dt2;
        return DateTime.Now;
    }
}

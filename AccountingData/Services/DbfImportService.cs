using System.Globalization;
using System.Text;
using AccountingData.Models;
using DbfDataReader;

namespace AccountingData.Services;

/// <summary>
/// Jedino mesto gde se pojavljuju stvarna imena DBF kolona iz legacy Clipper sistema
/// (KONTPLAN, ANKONT, MAGACIN, ARTIKLI, NALOG). Koriste ga i AccountingApp (uvoz iz UI)
/// i AccountingMigration (samostalni alat), da mapiranje ne bi divergiralo na dva mesta.
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
            System.Diagnostics.Debug.WriteLine($"Greška pri čitanju DBF fajla '{filepath}': {ex.Message}");
        }

        return list;
    }

    private static string Get(Dictionary<string, string> row, string key)
        => row.TryGetValue(key, out var v) ? v.Trim() : "";

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

    /// <summary>MAGACIN.DBF → Magacin. Vraća null ako red nema šifru. Tabela nema polje za naziv magacina.</summary>
    public static Magacin? MapMagacin(Dictionary<string, string> row)
    {
        string sifra = Get(row, "SIFRA");
        if (string.IsNullOrWhiteSpace(sifra)) return null;

        return new Magacin
        {
            SifraMagacina = sifra,
            NazivMagacina = $"Magacin {sifra}",
            OdgovornoLice = NullIfEmpty(Get(row, "RACUNOPOL")),
            VrstaMagacina = "Veleprodaja"
        };
    }

    /// <summary>ARTIKLI.DBF → Artikal. Vraća null ako red nema šifru. Tabela nema polje za cenu.</summary>
    public static Artikal? MapArtikal(Dictionary<string, string> row)
    {
        string sifra = Get(row, "SIFRA");
        if (string.IsNullOrWhiteSpace(sifra)) return null;

        string naziv = Get(row, "NAZIV");
        string jm = Get(row, "JED_MERE");
        string selektovanStr = Get(row, "SELEKTOVAN").ToUpperInvariant();

        return new Artikal
        {
            SifraArtikla = sifra,
            Naziv = string.IsNullOrWhiteSpace(naziv) ? $"Artikal {sifra}" : naziv,
            JedinicaMere = string.IsNullOrWhiteSpace(jm) ? "kom" : jm,
            Pakovanje = NullIfEmpty(Get(row, "PAKOVANJE")),
            TarifniBroj = NullIfEmpty(Get(row, "TAR_BROJ")),
            KlasifikacionaSifra = NullIfEmpty(Get(row, "KLAS_SIFRA")),
            Selektovan = selektovanStr is "T" or "1" or "TRUE" or "Y",
            Vrsta = "Roba"
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

    /// <summary>Grupiše NALOG.DBF redove po broju naloga (BR_NALOGA), izbacuje prazne/nulte brojeve.</summary>
    public static List<(string BrojNaloga, List<Dictionary<string, string>> Redovi)> GroupNalogRows(List<Dictionary<string, string>> rows)
    {
        return rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA") })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj.TrimStart('0') != "")
            .GroupBy(x => x.Broj)
            .Select(g => (g.Key, g.Select(x => x.Row).ToList()))
            .ToList();
    }

    /// <summary>Grupa redova NALOG.DBF (isti BR_NALOGA) → Nalog sa stavkama.</summary>
    public static Nalog? MapNalogGrupa(string brojNaloga, List<Dictionary<string, string>> redovi)
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

            nalog.Stavke.Add(new StavkaNaloga
            {
                RedniBroj = redBr > 0 ? redBr : rbFallback,
                BrojKonta = konto,
                BrojDokumenta = NullIfEmpty(brDokum),
                Opis = string.IsNullOrWhiteSpace(brDokum) ? nalog.Opis : brDokum,
                Duguje = dug,
                Potrazuje = pot,
                StariKonto = NullIfEmpty(Get(row, "ST_KON")),
                PromenaKod = promena
            });
            rbFallback++;
        }

        nalog.UkupnoDuguje = nalog.Stavke.Sum(s => s.Duguje);
        nalog.UkupnoPotrazuje = nalog.Stavke.Sum(s => s.Potrazuje);
        return nalog;
    }

    private static decimal ParseDecimal(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return 0m;
        return decimal.TryParse(str.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val) ? val : 0m;
    }

    private static DateTime ParseDate(string str)
    {
        if (str.Length == 8 && DateTime.TryParseExact(str, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            return dt;
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt2))
            return dt2;
        return DateTime.Now;
    }
}

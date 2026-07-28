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

        string naziv = Get(row, "NAZIV", "IME", "OPIS");
        if (string.IsNullOrWhiteSpace(naziv)) naziv = $"Magacin {sifra}";

        return new Magacin
        {
            SifraMagacina = sifra,
            NazivMagacina = naziv,
            OdgovornoLice = NullIfEmpty(Get(row, "RACUNOPOL", "ODG_LICE", "LICE")),
            VrstaMagacina = "Veleprodaja"
        };
    }

    /// <summary>ARTIKLI.DBF / M_SIFR.DBF → Artikal. Vraća null ako red nema šifru.</summary>
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
            Selektovan = selektovanStr is "T" or "1" or "TRUE" or "Y",
            Vrsta = "Roba"
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
            DatumPromene = ParseDate(Get(row, "DAT_PROM", "DATUM", "DAT_PROMENE")),
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
    public static Nalog? MapNalogGrupa(string brojNaloga, List<Dictionary<string, string>> redovi, Dictionary<int, string>? promeneMap = null)
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
        if (string.IsNullOrWhiteSpace(broj) || broj == "0" || broj.TrimStart('0') == "") return null;

        return new Kalkulacija
        {
            BrojKalkulacije = broj,
            Datum = ParseDate(Get(row, "DATUM", "DAT_KAL")),
            SifraDobavljaca = NullIfEmpty(Get(row, "DOBAVLJAC", "KUPAC", "KONTO")),
            BrojOtpremnice = NullIfEmpty(Get(row, "BR_OTP", "OTPREMNICA")),
            DatumOtpremnice = ParseDate(Get(row, "DAT_OTP")),
            BrojRacuna = NullIfEmpty(Get(row, "BR_RAC", "RACUN")),
            DatumRacuna = ParseDate(Get(row, "DAT_RAC")),
            NabavnaVrednost = ParseDecimal(Get(row, "NABAVNA", "NABAV_VRED", "NAB_VRED")),
            TransportniTroskovi = ParseDecimal(Get(row, "TRANSP_TRO", "TROSKOVI")),
            SvegaTroskovi = ParseDecimal(Get(row, "TROSKOVI", "SVEGA_TROS")),
            SvegaNabavno = ParseDecimal(Get(row, "SVEGA_NAB", "NABAVNA")),
            Razlika = ParseDecimal(Get(row, "RAZLIKA", "RUC")),
            Porez = ParseDecimal(Get(row, "POREZ", "PDV")),
            ProdajnaVrednost = ParseDecimal(Get(row, "PRODAJNA", "PROD_VRED")),
            SifraMagacina = NullIfEmpty(Get(row, "MAGACIN", "MAG")),
            IsKnjizen = Get(row, "KNJIZEN") == "1"
        };
    }

    /// <summary>KAL_NAL.DBF → KalkulacijaStavka.</summary>
    public static KalkulacijaStavka? MapKalkulacijaStavka(Dictionary<string, string> row, int defaultRedniBroj = 1)
    {
        string art = Get(row, "ARTIKAL", "SIFRA", "SIFRA_ART");
        if (string.IsNullOrWhiteSpace(art)) return null;

        int.TryParse(Get(row, "RBR", "RED_BROJ", "R_BR"), out int rbr);
        if (rbr <= 0) rbr = defaultRedniBroj;

        decimal kol = ParseDecimal(Get(row, "KOLICINA", "KOL"));
        decimal cena = ParseDecimal(Get(row, "CENA", "NAB_CENA"));
        decimal iznos = ParseDecimal(Get(row, "IZNOS", "NAB_VRED"));
        if (iznos == 0 && kol != 0 && cena != 0) iznos = kol * cena;

        return new KalkulacijaStavka
        {
            RedniBroj = rbr,
            SifraArtikla = art,
            Kolicina = kol,
            NabavnaCena = cena,
            Iznos = iznos,
            Troskovi = ParseDecimal(Get(row, "TROSKOVI", "TROS")),
            NabavnaVrednost = ParseDecimal(Get(row, "NABAVNA", "NAB_VRED")),
            RazlikaIznos = ParseDecimal(Get(row, "RAZLIKA", "RUC")),
            PorezIznos = ParseDecimal(Get(row, "POREZ", "PDV")),
            ProdajnaVrednost = ParseDecimal(Get(row, "PROD_VRED", "PRODAJNA")),
            ProdajnaCena = ParseDecimal(Get(row, "PROD_CENA", "CENA_PROD"))
        };
    }

    /// <summary>Grupiše KAL_NAL.DBF redove po broju kalkulacije.</summary>
    public static Dictionary<string, List<Dictionary<string, string>>> GroupKalkulacijaStavke(List<Dictionary<string, string>> rows)
    {
        return rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_KALKUL", "BR_KAL", "BR_NALOGA", "BROJ") })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj))
            .GroupBy(x => x.Broj, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Row).ToList(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>MAT_NAL.DBF / ZADUZ.DBF / RAZDUZ.DBF → PrimopredajaNalog i stavke.</summary>
    public static List<PrimopredajaNalog> MapPrimopredajaNalozi(List<Dictionary<string, string>> rows, string vrstaDokumenta = "Primopredaja")
    {
        var result = new List<PrimopredajaNalog>();

        var grouped = rows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA", "BR_NAL", "BROJ").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "")
            .GroupBy(x => x.Broj, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var firstRow = group.First();
            string brNaloga = group.Key;
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
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "")
            .GroupBy(x => x.Broj, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Row, StringComparer.OrdinalIgnoreCase);

        var grouped = racOtpRows
            .Select(r => new { Row = r, Broj = Get(r, "BR_NALOGA", "BR_NAL", "BROJ").Trim() })
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "")
            .GroupBy(x => x.Broj, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var firstRow = group.First().Row;
            string brNaloga = group.Key;
            DateTime datum = ParseDate(Get(firstRow, "DATUMOGA", "DATUM"));
            string magDaje = Get(firstRow, "MAG_DAJE", "MAGACIN");
            string kontoKupca = Get(firstRow, "KONTOZNOS", "KONTO", "KUPAC");
            string knjiStr = Get(firstRow, "KNJIZENOS", "KNJIZEN");

            int rokDana = 0;
            string brOtprem = brNaloga;
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
            .Where(x => !string.IsNullOrWhiteSpace(x.Broj) && x.Broj != "0" && x.Broj.TrimStart('0') != "")
            .GroupBy(x => x.Broj, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var firstRow = group.First().Row;
            string brNivelacije = group.Key;
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

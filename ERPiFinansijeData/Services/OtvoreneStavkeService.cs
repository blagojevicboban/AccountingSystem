using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class OtvoreneStavkeService
{
    private readonly AccountingDbContext _db;

    public OtvoreneStavkeService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<Partner>> GetPartneriAsync(string? search = null)
    {
        var query = _db.Partneri.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.SifraPartnera.Contains(search) || p.Naziv.Contains(search));
        }
        return await query.OrderBy(p => p.Naziv).ToListAsync();
    }

    /// <summary>
    /// Otvorene stavke (izvod) za partnera — hronološki, sa kumulativnim saldom,
    /// analogno legacy gk91/otv_st_zag proceduri iz FIN2.PRG, ali vezano preko
    /// StavkaNaloga.PartnerId (ne preko konta, jer legacy ANAL modul za ovu firmu
    /// nije korišćen pa nema podataka za uparivanje po kontu partnera).
    /// </summary>
    public async Task<List<KarticaRed>> GetOtvoreneStavkeAsync(int partnerId)
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Where(s => s.PartnerId == partnerId && s.Nalog != null && s.Nalog.IsKnjizen)
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        var rezultat = new List<KarticaRed>();
        decimal saldo = 0m;

        foreach (var s in stavke)
        {
            saldo += s.Duguje - s.Potrazuje;
            rezultat.Add(new KarticaRed
            {
                Datum = s.Nalog!.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis) : s.Opis,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = saldo
            });
        }

        return rezultat;
    }

    /// <summary>
    /// Bruto bilans analitike — promet i saldo po partneru (umesto po kontu), iz
    /// proknjiženih naloga sa dodeljenim partnerom. U legacy DOS sistemu ovo bi bio
    /// poseban ANAL modul izveštaj (A_brut_bil iz ANAL2.PRG) nad zasebnim ANNAL.DBF
    /// fajlom; ovde je isti podatak (StavkaNaloga.PartnerId) samo grupisan drugačije
    /// od finansijskog bruto bilansa (BrutoBilansService, grupisanog po kontu).
    /// </summary>
    public async Task<List<BrutoBilansAnalitikeRed>> GetBrutoBilansAnalitikeAsync()
    {
        var stavke = await _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Where(s => s.PartnerId != null && s.Nalog != null && s.Nalog.IsKnjizen)
            .ToListAsync();

        return stavke
            .GroupBy(s => s.PartnerId!.Value)
            .Select(g =>
            {
                var partner = g.First().Partner;
                decimal duguje = g.Sum(x => x.Duguje);
                decimal potrazuje = g.Sum(x => x.Potrazuje);
                return new BrutoBilansAnalitikeRed
                {
                    SifraPartnera = partner?.SifraPartnera ?? "?",
                    NazivPartnera = partner?.Naziv ?? "?",
                    Duguje = duguje,
                    Potrazuje = potrazuje,
                    Saldo = duguje - potrazuje
                };
            })
            .OrderBy(r => r.NazivPartnera)
            .ToList();
    }

    /// <summary>
    /// Dohvata izveštaj otvorenih stavki (IOS) po partnerima/kontima,
    /// sa mogućnošću filtriranja po opsegu konta (odKonta-doKonta, npr. 202 - 2029999 kao u legacy gk91)
    /// i vremenskom periodu.
    /// </summary>
    public async Task<List<IosPartnerGrupa>> GetIosIzvestajAsync(
        string? odKonta = null,
        string? doKonta = null,
        DateTime? odDatuma = null,
        DateTime? doDatuma = null,
        bool samoSaSaldom = true,
        bool koristiZatvaranje = false)
    {
        var query = _db.StavkeNaloga
            .Include(s => s.Nalog)
            .Include(s => s.Partner)
            .Where(s => s.Nalog != null && s.Nalog.IsKnjizen);

        if (odDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga >= odDatuma.Value);

        if (doDatuma.HasValue)
            query = query.Where(s => s.Nalog!.DatumNaloga <= doDatuma.Value);

        var stavke = await query
            .OrderBy(s => s.Nalog!.DatumNaloga)
            .ThenBy(s => s.Nalog!.NalogId)
            .ThenBy(s => s.RedniBroj)
            .ToListAsync();

        // Filtriranje opsega konta u memoriji radi tačnosti prefix/range poređenja
        string odK = (odKonta ?? "").Trim();
        string doK = (doKonta ?? "").Trim();

        if (!string.IsNullOrEmpty(odK) || !string.IsNullOrEmpty(doK))
        {
            stavke = stavke.Where(s =>
            {
                if (string.IsNullOrEmpty(s.BrojKonta)) return false;
                string k = s.BrojKonta.Trim();

                if (!string.IsNullOrEmpty(odK) && string.IsNullOrEmpty(doK))
                {
                    return k.StartsWith(odK, StringComparison.OrdinalIgnoreCase) ||
                           string.Compare(k, odK, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (string.IsNullOrEmpty(odK) && !string.IsNullOrEmpty(doK))
                {
                    return k.StartsWith(doK, StringComparison.OrdinalIgnoreCase) ||
                           string.Compare(k, doK, StringComparison.OrdinalIgnoreCase) <= 0;
                }

                if (string.Equals(odK, doK, StringComparison.OrdinalIgnoreCase))
                {
                    return k.StartsWith(odK, StringComparison.OrdinalIgnoreCase);
                }

                bool okOd = k.StartsWith(odK, StringComparison.OrdinalIgnoreCase) || string.Compare(k, odK, StringComparison.OrdinalIgnoreCase) >= 0;
                bool okDo = k.StartsWith(doK, StringComparison.OrdinalIgnoreCase) || string.Compare(k, doK, StringComparison.OrdinalIgnoreCase) <= 0;

                return okOd && okDo;
            }).ToList();
        }

        var kontaMap = await _db.Konta
            .AsNoTracking()
            .ToDictionaryAsync(k => k.BrojKonta.Trim(), k => k.NazivKonta, StringComparer.OrdinalIgnoreCase);

        Dictionary<int, decimal> zatvorenoPoDuguje = new();
        Dictionary<int, decimal> zatvorenoPoPotrazuje = new();
        if (koristiZatvaranje)
        {
            var stavkaIds = stavke.Select(s => s.StavkaNalogaId).ToList();
            var zatvaranja = await _db.ZatvaranjaStavki
                .Where(z => stavkaIds.Contains(z.StavkaDugujeId) || stavkaIds.Contains(z.StavkaPotrazujeId))
                .ToListAsync();
            zatvorenoPoDuguje = zatvaranja.GroupBy(z => z.StavkaDugujeId).ToDictionary(g => g.Key, g => g.Sum(z => z.Iznos));
            zatvorenoPoPotrazuje = zatvaranja.GroupBy(z => z.StavkaPotrazujeId).ToDictionary(g => g.Key, g => g.Sum(z => z.Iznos));
        }

        var grupeDict = new Dictionary<string, IosPartnerGrupa>();

        foreach (var s in stavke)
        {
            string key = s.PartnerId.HasValue
                ? $"P_{s.PartnerId.Value}_{s.BrojKonta}"
                : $"K_{s.BrojKonta}";

            if (!grupeDict.TryGetValue(key, out var grupa))
            {
                string nazivVal = s.Partner != null && !string.IsNullOrWhiteSpace(s.Partner.Naziv)
                    ? s.Partner.Naziv
                    : (kontaMap.TryGetValue(s.BrojKonta.Trim(), out var kNaziv) && !string.IsNullOrWhiteSpace(kNaziv)
                        ? kNaziv
                        : $"Konto {s.BrojKonta}");

                var partnerObj = s.Partner ?? new Partner
                {
                    PartnerId = s.PartnerId ?? 0,
                    SifraPartnera = string.IsNullOrWhiteSpace(s.BrojKonta) ? "---" : s.BrojKonta,
                    Naziv = nazivVal,
                    KontoPartnera = s.BrojKonta
                };

                grupa = new IosPartnerGrupa
                {
                    SifraPartnera = partnerObj.SifraPartnera,
                    NazivPartnera = nazivVal,
                    Konto = s.BrojKonta,
                    Adresa = partnerObj.Adresa,
                    PttIMesto = partnerObj.PttIMesto,
                    Pib = partnerObj.Pib,
                    Partner = partnerObj,
                    Stavke = new List<KarticaRed>()
                };

                grupeDict[key] = grupa;
            }

            decimal prethodniSaldo = grupa.Stavke.Count > 0 ? grupa.Stavke[^1].Saldo : 0m;
            decimal noviSaldo = prethodniSaldo + s.Duguje - s.Potrazuje;

            decimal? preostalo = null;
            string? statusZatvaranja = null;
            int? danaKasnjenja = null;

            if (koristiZatvaranje)
            {
                if (s.Duguje > 0)
                {
                    decimal zatvoreno = zatvorenoPoDuguje.TryGetValue(s.StavkaNalogaId, out var z1) ? z1 : 0m;
                    (preostalo, statusZatvaranja) = ZatvaranjeStavkiService.IzracunajPreostaloIStatus(s.Duguje, zatvoreno);
                }
                else if (s.Potrazuje > 0)
                {
                    decimal zatvoreno = zatvorenoPoPotrazuje.TryGetValue(s.StavkaNalogaId, out var z2) ? z2 : 0m;
                    (preostalo, statusZatvaranja) = ZatvaranjeStavkiService.IzracunajPreostaloIStatus(s.Potrazuje, zatvoreno);
                }

                if (s.ValutaDospela.HasValue && preostalo.HasValue && preostalo.Value > 0.01m)
                {
                    danaKasnjenja = Math.Max(0, (DateTime.Now.Date - s.ValutaDospela.Value.Date).Days);
                }
            }

            grupa.Stavke.Add(new KarticaRed
            {
                Datum = s.Nalog!.DatumNaloga,
                BrojNaloga = s.Nalog.BrojNaloga,
                Opis = string.IsNullOrWhiteSpace(s.Opis) ? (s.BrojDokumenta ?? s.Nalog.Opis) : s.Opis,
                OpisPromene = s.BrojDokumenta,
                Duguje = s.Duguje,
                Potrazuje = s.Potrazuje,
                Saldo = noviSaldo,
                Preostalo = preostalo,
                StatusZatvaranja = statusZatvaranja,
                ValutaDospela = s.ValutaDospela,
                DanaKasnjenja = danaKasnjenja
            });
        }

        var rezultat = grupeDict.Values.ToList();

        if (samoSaSaldom)
        {
            rezultat = rezultat.Where(g => g.Saldo != 0m || (g.Stavke.Count > 0 && g.Stavke.Any(st => st.Saldo != 0m))).ToList();
        }

        return rezultat
            .OrderBy(g => g.Konto)
            .ThenBy(g => g.NazivPartnera)
            .ToList();
    }
}

public class IosPartnerGrupa : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public string SifraPartnera { get; set; } = string.Empty;
    public string NazivPartnera { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public string? Adresa { get; set; }
    public string? PttIMesto { get; set; }
    public string? Pib { get; set; }
    public Partner Partner { get; set; } = null!;
    public List<KarticaRed> Stavke { get; set; } = new();
    public decimal UkupnoDuguje => Stavke.Sum(s => s.Duguje);
    public decimal UkupnoPotrazuje => Stavke.Sum(s => s.Potrazuje);
    public decimal Saldo => Stavke.Count > 0 ? Stavke[^1].Saldo : 0m;
    public int BrojStavki => Stavke.Count;

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}

public class BrutoBilansAnalitikeRed
{
    public string SifraPartnera { get; set; } = string.Empty;
    public string NazivPartnera { get; set; } = string.Empty;
    public decimal Duguje { get; set; }
    public decimal Potrazuje { get; set; }
    public decimal Saldo { get; set; }
}


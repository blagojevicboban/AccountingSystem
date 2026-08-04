using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeData.Services;

public class PrimopredajaService
{
    private readonly AccountingDbContext _db;

    public PrimopredajaService(AccountingDbContext db)
    {
        _db = db;
    }

    public async Task<List<PrimopredajaNalog>> GetPrimopredajeAsync(string? search = null)
    {
        var query = _db.PrimopredajaNalozi
            .Include(p => p.Stavke)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.BrojNaloga.ToString().Contains(search) || p.SifraMagacinaDaje.Contains(search) || p.SifraMagacinaPrima.Contains(search));
        }

        return await query.OrderByDescending(p => p.Datum).ThenByDescending(p => p.PrimopredajaNalogId).ToListAsync();
    }

    public async Task<PrimopredajaNalog> SavePrimopredajuAsync(PrimopredajaNalog nalog)
    {
        if (nalog.PrimopredajaNalogId == 0)
        {
            _db.PrimopredajaNalozi.Add(nalog);
        }
        else
        {
            var existing = await _db.PrimopredajaNalozi
                .Include(p => p.Stavke)
                .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == nalog.PrimopredajaNalogId);

            if (existing != null)
            {
                if (existing.IsKnjizen) throw new InvalidOperationException("Proknjižena primopredaja se ne može menjati.");

                existing.BrojNaloga = nalog.BrojNaloga;
                existing.Datum = nalog.Datum;
                existing.SifraMagacinaDaje = nalog.SifraMagacinaDaje;
                existing.SifraMagacinaPrima = nalog.SifraMagacinaPrima;
                existing.StopaPdv = nalog.StopaPdv;

                _db.PrimopredajaStavke.RemoveRange(existing.Stavke);
                existing.Stavke = nalog.Stavke;
            }
        }

        await _db.SaveChangesAsync();
        return nalog;
    }

    public async Task KnjiziPrimopredajuAsync(int primopredajaNalogId)
    {
        var nalog = await _db.PrimopredajaNalozi
            .Include(p => p.Stavke)
            .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == primopredajaNalogId);

        if (nalog == null) throw new InvalidOperationException("Primopredaja nije pronađena.");
        if (nalog.IsKnjizen) throw new InvalidOperationException("Primopredaja je već proknjižena.");

        var magDaje = await _db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == nalog.SifraMagacinaDaje);
        var magPrima = await _db.Magacini.FirstOrDefaultAsync(m => m.SifraMagacina == nalog.SifraMagacinaPrima);
        bool prelaziVpMp = (magDaje?.VrstaMagacina ?? "Veleprodaja") != (magPrima?.VrstaMagacina ?? "Veleprodaja");

        var kartice = new MaterijalnaKarticaService(_db);
        decimal ukupnoVrednostDaje = 0m;
        decimal ukupnoVrednostPrima = 0m;

        foreach (var s in nalog.Stavke)
        {
            // 1. Izlaz iz magacina koji daje (automatski računa prosečnu cenu)
            decimal vrednost = await kartice.DodajIzlazRedAsync(
                nalog.SifraMagacinaDaje,
                s.SifraArtikla,
                nalog.Datum,
                $"Primopredaja br. {nalog.BrojNaloga} u magacin {nalog.SifraMagacinaPrima}",
                s.Kolicina);

            // 2. Ulaz u magacin koji prima. Kod prelaska veleprodaja↔maloprodaja se vrednost
            // preračunava po StopaPdv (v. KreirajNalogPrelazaVpMp) jer maloprodajni magacin
            // vodi robu SA PDV a veleprodajni BEZ — u suprotnom bi kartica magacina koji prima
            // mešala vrednosti sa i bez poreza.
            decimal vrednostPrima = prelaziVpMp ? PreracunajVrednost(vrednost, magDaje, magPrima, nalog.StopaPdv) : vrednost;
            decimal jedinicaCena = s.Kolicina != 0 ? vrednostPrima / s.Kolicina : 0m;
            await kartice.DodajUlazRedAsync(
                nalog.SifraMagacinaPrima,
                s.SifraArtikla,
                nalog.Datum,
                $"Primopredaja br. {nalog.BrojNaloga} iz magacina {nalog.SifraMagacinaDaje}",
                s.Kolicina,
                jedinicaCena);

            ukupnoVrednostDaje += vrednost;
            ukupnoVrednostPrima += vrednostPrima;
        }

        if (prelaziVpMp && ukupnoVrednostDaje != 0)
        {
            nalog.NalogId = await KreirajNalogPrelazaVpMpAsync(nalog, magDaje, magPrima, ukupnoVrednostDaje, ukupnoVrednostPrima);
        }

        nalog.IsKnjizen = true;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Preračunava vrednost robe pri prelasku između veleprodajnog magacina (vodi robu BEZ PDV,
    /// konto 1320) i maloprodajnog (vodi robu SA PDV, konto 1340) — dodaje PDV kad roba ulazi u
    /// prodavnicu, oduzima ga kad se vraća u stovarište. Bez promene ako obe strane iste vrste.
    /// </summary>
    private static decimal PreracunajVrednost(decimal vrednost, Magacin? magDaje, Magacin? magPrima, decimal stopaPdv)
    {
        bool primaJeMaloprodaja = magPrima?.VrstaMagacina == "Maloprodaja";
        bool dajeJeMaloprodaja = magDaje?.VrstaMagacina == "Maloprodaja";

        if (primaJeMaloprodaja && !dajeJeMaloprodaja)
            return Math.Round(vrednost * (1 + stopaPdv / 100m), 2);

        if (!primaJeMaloprodaja && dajeJeMaloprodaja)
            return Math.Round(vrednost / (1 + stopaPdv / 100m), 2);

        return vrednost;
    }

    /// <summary>
    /// Nalog u Glavnoj knjizi za prelazak robe između veleprodajnog i maloprodajnog magacina
    /// (Zaduženje/Razduženje prodavnice) — prenosi vrednost sa konta jednog magacina na konto
    /// drugog (<see cref="RobnaKonta.RobaZaVrstuMagacina"/>) i knjiži razliku na ukalkulisani PDV.
    /// Analogno obrascu iz <see cref="MaloprodajnaKalkulacijaService"/>, samo bez konta dobavljača
    /// jer je ovo interni prenos, ne nabavka. Napomena: dira samo osnovnu vrednost i PDV — ne
    /// prekvalifikuje ukalkulisanu razliku u ceni (1329/1348), koja ostaje kako je uneta pri
    /// prvobitnoj kalkulaciji (v. ANALIZA_I_PLAN.md §9.1, "Obračun razlike u ceni").
    /// </summary>
    private async Task<int> KreirajNalogPrelazaVpMpAsync(PrimopredajaNalog nalog, Magacin? magDaje, Magacin? magPrima, decimal vrednostDaje, decimal vrednostPrima)
    {
        string kontoDaje = RobnaKonta.RobaZaVrstuMagacina(magDaje?.VrstaMagacina);
        string kontoPrima = RobnaKonta.RobaZaVrstuMagacina(magPrima?.VrstaMagacina);
        string kontoPdv = RobnaKonta.UkalkulisaniPdvZaStopu(nalog.StopaPdv);
        decimal pdvIznos = Math.Abs(vrednostPrima - vrednostDaje);
        bool prelazUMaloprodaju = magPrima?.VrstaMagacina == "Maloprodaja";

        string opis = $"{nalog.VrstaDokumenta} br. {nalog.BrojNaloga} ({nalog.SifraMagacinaDaje} → {nalog.SifraMagacinaPrima})";
        int sledeciBroj = (await _db.Nalozi.Select(n => (int?)n.BrojNaloga).MaxAsync() ?? 0) + 1;

        var glavniNalog = new Nalog
        {
            BrojNaloga = sledeciBroj,
            DatumNaloga = nalog.Datum,
            Opis = opis,
            IsKnjizen = true,
            DatumKnjiženja = DateTime.Now,
            VrstaNaloga = "PRIMOPREDAJA"
        };

        int rb = 1;
        glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, BrojKonta = kontoPrima, Opis = opis, Duguje = vrednostPrima, Potrazuje = 0m });
        glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb++, BrojKonta = kontoDaje, Opis = opis, Duguje = 0m, Potrazuje = vrednostDaje });

        if (pdvIznos != 0)
        {
            if (prelazUMaloprodaju)
                glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb, BrojKonta = kontoPdv, Opis = opis, Duguje = 0m, Potrazuje = pdvIznos });
            else
                glavniNalog.Stavke.Add(new StavkaNaloga { RedniBroj = rb, BrojKonta = kontoPdv, Opis = opis, Duguje = pdvIznos, Potrazuje = 0m });
        }

        glavniNalog.UkupnoDuguje = glavniNalog.Stavke.Sum(s => s.Duguje);
        glavniNalog.UkupnoPotrazuje = glavniNalog.Stavke.Sum(s => s.Potrazuje);

        _db.Nalozi.Add(glavniNalog);
        await _db.SaveChangesAsync();
        return glavniNalog.NalogId;
    }

    /// <summary>
    /// Rasknjiži primopredaju (ili zaduženje/razduženje — isti dokument) — uklanja redove
    /// materijalne kartice koje je ova primopredaja upisala (obrnutim redosledom od
    /// knjiženja, magacin prima pa magacin daje po stavci) i vraća nalog u status nacrta
    /// radi izmene. Baca grešku ako je za neki artikal/magacin u međuvremenu knjiženo
    /// nešto kasnije.
    /// </summary>
    public async Task RasknjiziPrimopredajuAsync(int primopredajaNalogId)
    {
        var nalog = await _db.PrimopredajaNalozi
            .Include(p => p.Stavke)
            .FirstOrDefaultAsync(p => p.PrimopredajaNalogId == primopredajaNalogId);

        if (nalog == null) throw new InvalidOperationException("Primopredaja nije pronađena.");
        if (!nalog.IsKnjizen) throw new InvalidOperationException("Primopredaja nije proknjižena.");

        var kartice = new MaterijalnaKarticaService(_db);

        foreach (var s in nalog.Stavke.AsEnumerable().Reverse())
        {
            await kartice.UkloniPoslednjiRedAsync(
                nalog.SifraMagacinaPrima,
                s.SifraArtikla,
                $"Primopredaja br. {nalog.BrojNaloga} iz magacina {nalog.SifraMagacinaDaje}");

            await kartice.UkloniPoslednjiRedAsync(
                nalog.SifraMagacinaDaje,
                s.SifraArtikla,
                $"Primopredaja br. {nalog.BrojNaloga} u magacin {nalog.SifraMagacinaPrima}");
        }

        if (nalog.NalogId.HasValue)
        {
            var glavniNalog = await _db.Nalozi.Include(n => n.Stavke).FirstOrDefaultAsync(n => n.NalogId == nalog.NalogId.Value);
            if (glavniNalog != null)
            {
                _db.StavkeNaloga.RemoveRange(glavniNalog.Stavke);
                _db.Nalozi.Remove(glavniNalog);
            }
            nalog.NalogId = null;
        }

        nalog.IsKnjizen = false;
        await _db.SaveChangesAsync();
    }
}

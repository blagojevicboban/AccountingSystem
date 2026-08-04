using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeApp.Services;

/// <summary>
/// Puni i pretražuje padajuću listu konta partnera (dobavljač / kupac) u dokumentima.
///
/// Legacy sistem ovo radi u <c>daj_konto(us)</c> (FIN2.PRG:1223-1228) — otvara kontni plan
/// filtriran na <c>left(konto,3)</c> jednako grupi kupaca (us=1) ili dobavljača (us=2), pa
/// pusti korisnika da bira. Grupe nisu izvedene iz kontnog okvira nego su podatak firme:
/// FIN1.PRG:643-649 ih vezuje za fleg <c>novi_zakon</c> — po novom zakonu kupci "204" i
/// dobavljači "435", po starom "120" i "220".
/// </summary>
public static class KontoPicker
{
    /// <summary>Grupe konta partnera po novom i starom zakonu (FIN1.PRG:643-649).</summary>
    public static class Grupe
    {
        public const string DobavljaciNoviZakon = "435";
        public const string DobavljaciStariZakon = "220";
        public const string KupciNoviZakon = "204";
        public const string KupciStariZakon = "120";
    }

    /// <summary>
    /// Bira grupu koju kontni plan firme zaista koristi. Firme prenete sa starog zakona
    /// nemaju 435/204 nego 220/120, pa bi fiksni prefiks kod njih dao praznu listu.
    /// </summary>
    public static string OdrediPrefiks(IEnumerable<Konto> konta, string noviZakon, string stariZakon)
    {
        var lista = konta as IList<Konto> ?? konta.ToList();
        if (lista.Any(k => k.BrojKonta.StartsWith(noviZakon, System.StringComparison.Ordinal))) return noviZakon;
        if (lista.Any(k => k.BrojKonta.StartsWith(stariZakon, System.StringComparison.Ordinal))) return stariZakon;
        return noviZakon;
    }

    /// <summary>Vezuje kombo za konta dobavljača (435 / 220).</summary>
    public static void PoveziDobavljace(ComboBox combo, IEnumerable<Konto> konta)
        => Poveži(combo, konta, OdrediPrefiks(konta, Grupe.DobavljaciNoviZakon, Grupe.DobavljaciStariZakon));

    /// <summary>Vezuje kombo za konta kupaca (204 / 120).</summary>
    public static void PoveziKupce(ComboBox combo, IEnumerable<Konto> konta)
        => Poveži(combo, konta, OdrediPrefiks(konta, Grupe.KupciNoviZakon, Grupe.KupciStariZakon));

    /// <summary>
    /// Vezuje kombo za konta zadate grupe i uključuje pretragu po otkucanom tekstu.
    /// Traži se i po broju i po nazivu (a ne samo po početku prikaza, kako radi ugrađeni
    /// <c>TextSearch</c>), jer se partner češće zna po imenu nego po šifri.
    /// </summary>
    public static void Poveži(ComboBox combo, IEnumerable<Konto> konta, string prefiks)
    {
        var svi = konta
            .Where(k => k.BrojKonta.StartsWith(prefiks, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.BrojKonta)
            .ToList();

        combo.IsEditable = true;
        combo.IsTextSearchEnabled = false;
        combo.StaysOpenOnEdit = true;
        combo.DisplayMemberPath = nameof(Konto.Prikaz);
        combo.SelectedValuePath = nameof(Konto.BrojKonta);
        combo.ItemsSource = svi;
        _izvori[combo] = svi;

        if (combo.Template?.FindName("PART_EditableTextBox", combo) is TextBox)
        {
            ZakačiPretragu(combo);
        }
        else
        {
            // Šablon se primenjuje tek pri prvom iscrtavanju — Loaded je siguran trenutak
            // da se dohvati unutrašnji TextBox i zakači pretraga.
            combo.Loaded += (_, _) => ZakačiPretragu(combo);
        }
    }

    private static readonly Dictionary<ComboBox, List<Konto>> _izvori = new();

    private static void ZakačiPretragu(ComboBox combo)
    {
        if (combo.Template?.FindName("PART_EditableTextBox", combo) is not TextBox tb) return;

        tb.TextChanged -= NaPromenuTeksta;
        tb.TextChanged += NaPromenuTeksta;
    }

    private static void NaPromenuTeksta(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (tb.TemplatedParent is not ComboBox combo) return;
        if (!_izvori.TryGetValue(combo, out var svi)) return;

        string upit = tb.Text?.Trim() ?? "";
        int caret = tb.CaretIndex;

        combo.ItemsSource = string.IsNullOrEmpty(upit)
            ? svi
            : svi.Where(k =>
                    k.BrojKonta.Contains(upit, System.StringComparison.OrdinalIgnoreCase) ||
                    k.NazivKonta.Contains(upit, System.StringComparison.OrdinalIgnoreCase))
                 .ToList();

        // Postavljanje ItemsSource prepisuje tekst iz izabrane stavke — vraćamo ono što je
        // korisnik otkucao, inače bi mu se unos brisao na svako slovo.
        if (tb.Text != upit)
        {
            tb.Text = upit;
            tb.CaretIndex = caret;
        }

        combo.IsDropDownOpen = combo.Items.Count > 0;
    }

    /// <summary>Konto koji je korisnik izabrao ili otkucao (dozvoljen je i konto van grupe).</summary>
    public static string? IzabraniKonto(ComboBox combo)
    {
        if (combo.SelectedItem is Konto k) return k.BrojKonta;

        string tekst = combo.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(tekst)) return null;

        // Kad je izabrano iz liste bez SelectedItem-a, tekst je u obliku "broj - naziv".
        int crta = tekst.IndexOf(" - ", System.StringComparison.Ordinal);
        return crta > 0 ? tekst[..crta] : tekst;
    }

    /// <summary>Postavlja zatečeni konto pri otvaranju postojećeg dokumenta.</summary>
    public static void PostaviKonto(ComboBox combo, string? brojKonta)
    {
        if (string.IsNullOrWhiteSpace(brojKonta)) return;

        if (_izvori.TryGetValue(combo, out var svi) &&
            svi.FirstOrDefault(k => k.BrojKonta == brojKonta) is { } pogodak)
        {
            combo.SelectedItem = pogodak;
        }
        else
        {
            // Konto iz starijeg dokumenta ne mora biti u grupi — prikazuje se kakav jeste.
            combo.Text = brojKonta;
        }
    }
}

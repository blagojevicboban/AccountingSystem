using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeApp.Services;

/// <summary>
/// Puni i pretražuje padajuću listu partnera (kupac/dobavljač) u dokumentima — isti obrazac
/// pretrage kao <see cref="KontoPicker"/> (koji radi nad kontnim planom), ovde nad šifarnikom
/// partnera. Traži se po šifri, nazivu i PIB-u, jer se partner često zna po jednom od ta tri.
/// </summary>
public static class PartnerPicker
{
    /// <summary>Vezuje kombo za ceo šifarnik partnera i uključuje pretragu po otkucanom tekstu.</summary>
    public static void Poveži(ComboBox combo, IEnumerable<Partner> partneri)
    {
        var svi = partneri.OrderBy(p => p.Naziv).ToList();

        combo.IsEditable = true;
        combo.IsTextSearchEnabled = false;
        combo.StaysOpenOnEdit = true;
        combo.DisplayMemberPath = nameof(Partner.Prikaz);
        combo.SelectedValuePath = nameof(Partner.PartnerId);
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

    private static readonly Dictionary<ComboBox, List<Partner>> _izvori = new();

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
            : svi.Where(p =>
                    p.Naziv.Contains(upit, System.StringComparison.OrdinalIgnoreCase) ||
                    p.SifraPartnera.Contains(upit, System.StringComparison.OrdinalIgnoreCase) ||
                    (p.Pib != null && p.Pib.Contains(upit, System.StringComparison.OrdinalIgnoreCase)))
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

    /// <summary>Partner koji je korisnik izabrao iz liste (null ako je samo otkucao tekst bez izbora).</summary>
    public static Partner? IzabraniPartner(ComboBox combo) => combo.SelectedItem as Partner;

    /// <summary>Postavlja zatečenog partnera pri otvaranju postojećeg dokumenta.</summary>
    public static void PostaviPartnera(ComboBox combo, int? partnerId)
    {
        if (partnerId == null) return;

        if (_izvori.TryGetValue(combo, out var svi) &&
            svi.FirstOrDefault(p => p.PartnerId == partnerId.Value) is { } pogodak)
        {
            combo.SelectedItem = pogodak;
        }
    }
}

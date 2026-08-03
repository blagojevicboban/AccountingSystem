using System.Globalization;
using System.Windows.Data;

namespace ERPiFinansijeApp.Views.Nalozi;

/// <summary>
/// Prikazuje broj konta zajedno sa nazivom ("2413/1 - POSEBAN TEKUCI RACUN") u ćeliji
/// koja nije u režimu izmene. Naziv se čita iz keša koji puni NalogEditWindow pri
/// učitavanju kontnog plana, jer stavka naloga u sebi nosi samo broj konta.
/// </summary>
public sealed class KontoPrikazConverter : IValueConverter
{
    public static Dictionary<string, string> Nazivi { get; } = new(StringComparer.OrdinalIgnoreCase);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string broj = (value as string)?.Trim() ?? string.Empty;
        if (broj.Length == 0) return string.Empty;

        return Nazivi.TryGetValue(broj, out var naziv) && !string.IsNullOrWhiteSpace(naziv)
            ? $"{broj} - {naziv}"
            : broj;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

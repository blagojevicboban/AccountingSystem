using System;
using System.Windows;
using System.Windows.Input;
using ERPiFinansijeApp.Views.Pomoc;
using ERPiFinansijeData;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Nalozi;

public partial class PreknjizavanjeWindow : Window
{
    public PreknjizavanjeWindow()
    {
        InitializeComponent();
        ContextHelpFix.UkloniDugmeZaPomoc(this);
    }

    private async void BtnIzvrsi_Click(object sender, RoutedEventArgs e)
    {
        string staro = TxtStaroKonto.Text.Trim();
        string novo = TxtNovoKonto.Text.Trim();

        if (string.IsNullOrWhiteSpace(staro))
        {
            MessageBox.Show("Unesite staro konto.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(novo))
        {
            MessageBox.Show("Unesite novo konto.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (staro == novo)
        {
            MessageBox.Show("Staro i novo konto ne mogu biti jednaki.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var potvrda = MessageBox.Show(
            $"PAŽNJA!\n\nDa li zaista želite da preknjižite sve stavke sa konta '{staro}' na novo konto '{novo}'?",
            "Potvrda preknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            int zamenjeno = await service.PreknjiziKontoAsync(staro, novo);
            MessageBox.Show($"Preknjižavanje uspešno završeno!\n\nZamenjeno je ukupno {zamenjeno} stavki sa konta {staro} na konto {novo}.",
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri preknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            BtnOdustani_Click(sender, e);
        }
        else if (e.Key == Key.F1)
        {
            OtvoriPomoc();
        }
    }

    private void OtvoriPomoc()
    {
        new EditHelpWindow(
            "🔄 Pomoć — Preknjižavanje konta",
            "Masovna zamena broja konta na svim stavkama naloga glavne knjige (analogno DOS proceduri 'prekm').",
            new (string, string)[]
            {
                ("Esc", "Odustaje bez izvršavanja."),
            },
            "Akcija menja SVE stavke koje trenutno koriste 'Staro konto' na 'Novo konto', bez obzira na nalog kom pripadaju. Preporučuje se rezervna kopija baze pre pokretanja."
        ) { Owner = this }.ShowDialog();
    }
}

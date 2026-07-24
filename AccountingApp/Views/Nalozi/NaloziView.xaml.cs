using System.Windows;
using System.Windows.Controls;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Nalozi;

public partial class NaloziView : UserControl
{
    private List<Nalog> _allNalozi = new();

    public NaloziView()
    {
        InitializeComponent();
        // Set here (not as a XAML literal) so the Checked event this triggers runs
        // after the whole tree — including DgNalozi, declared later in the XAML —
        // is fully constructed. As a XAML attribute it fired mid-InitializeComponent(),
        // before DgNalozi existed, crashing ApplyFilter() with a NullReferenceException.
        ChkSamoProknjizeni.IsChecked = true;
        LoadNalozi();
    }

    private async void LoadNalozi()
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            _allNalozi = await service.GetNaloziAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        string search = TxtPretraga.Text.Trim().ToLower();
        bool samoKnjizeni = ChkSamoProknjizeni.IsChecked ?? false;

        var filtered = _allNalozi.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToLower().Contains(search) || (n.Opis != null && n.Opis.ToLower().Contains(search))) &&
            (!samoKnjizeni || n.IsKnjizen)
        ).ToList();

        DgNalozi.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgNalozi.SelectedIndex = 0;
        }
        else
        {
            DgStavke.ItemsSource = null;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void DgNalozi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selectedNalog)
        {
            TxtDetailHeader.Text = $"📋 Stavke naloga #{selectedNalog.BrojNaloga} ({selectedNalog.Opis})";
            DgStavke.ItemsSource = selectedNalog.Stavke;
        }
    }

    private void BtnNoviNalog_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new NalogEditWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }

    private void BtnIzmeniNalog_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog selectedNalog)
        {
            MessageBox.Show("Izaberite nalog za izmenu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selectedNalog.IsKnjizen)
        {
            MessageBox.Show("Proknjižen nalog se ne može menjati.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dijalog = new NalogEditWindow(selectedNalog) { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }

    private async void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is Nalog selectedNalog)
        {
            if (selectedNalog.IsKnjizen)
            {
                MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} je već proknjižen!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new NaloziService(db);

                await service.KnjiziNalogAsync(selectedNalog.NalogId);
                MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadNalozi();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void BtnRasknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (DgNalozi.SelectedItem is not Nalog selectedNalog)
        {
            MessageBox.Show("Izaberite nalog za rasknjižavanje.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!selectedNalog.IsKnjizen)
        {
            MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} nije proknjižen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li ste sigurni da želite da rasknjižite nalog #{selectedNalog.BrojNaloga}?\n\nNalog će se vratiti u status nacrta i moći će ponovo da se izmeni.",
            "Potvrda rasknjižavanja", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            await service.RasknjiziNalogAsync(selectedNalog.NalogId);
            MessageBox.Show($"Nalog #{selectedNalog.BrojNaloga} je rasknjižen.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadNalozi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnNovaGodina_Click(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAdministrator)
        {
            MessageBox.Show("Prenos početnog stanja u novu godinu dozvoljen je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var proknjizeniNalozi = _allNalozi.Where(n => n.IsKnjizen).ToList();
        if (proknjizeniNalozi.Count == 0)
        {
            MessageBox.Show("Nema proknjiženih naloga — nema šta da se prenese u novu godinu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        int izvornaGodina = proknjizeniNalozi.Max(n => n.DatumNaloga.Year);
        int novaGodina = izvornaGodina + 1;

        var potvrda = MessageBox.Show(
            $"Da li želite da prenesete zaključni saldo konta iz {izvornaGodina}. u {novaGodina}. godinu?\n\n" +
            $"Biće kreiran nalog za početno stanje datiran 01.01.{novaGodina}. sa saldom svakog konta koji ima promet.",
            "Potvrda prenosa u novu godinu", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (potvrda != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NovaGodinaService(db);

            var nalog = await service.PrenesiUNovuGoduAsync(izvornaGodina);
            MessageBox.Show(
                $"Preneseno početno stanje u {novaGodina}. godinu — nalog #{nalog.BrojNaloga} sa {nalog.Stavke.Count} stavki (Duguje={nalog.UkupnoDuguje:N2}, Potražuje={nalog.UkupnoPotrazuje:N2}).",
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadNalozi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri prenosu u novu godinu: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}

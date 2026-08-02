using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Konta;

public partial class KontoPickerWindow : Window
{
    private List<Konto> _allKonta = new();
    public Konto? IzabraniKonto { get; private set; }

    public KontoPickerWindow(string initialSearch = "")
    {
        InitializeComponent();
        TxtPretraga.Text = initialSearch;
        Loaded += KontoPickerWindow_Loaded;
    }

    private async void KontoPickerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TxtPretraga.Focus();
        TxtPretraga.SelectAll();

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new KontaService(db);
            _allKonta = await service.GetKontaAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju kontnog plana: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        if (DgKonta == null) return;

        string query = TxtPretraga.Text.Trim().ToLower();
        var filtered = _allKonta.Where(k =>
            string.IsNullOrEmpty(query) ||
            k.BrojKonta.ToLower().Contains(query) ||
            k.NazivKonta.ToLower().Contains(query) ||
            (k.StariKonto != null && k.StariKonto.ToLower().Contains(query))
        ).ToList();

        DgKonta.ItemsSource = filtered;
        if (filtered.Any())
        {
            DgKonta.SelectedIndex = 0;
        }
    }

    private void TxtPretraga_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void TxtPretraga_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && DgKonta.Items.Count > 0)
        {
            DgKonta.Focus();
            if (DgKonta.SelectedIndex < 0) DgKonta.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            PotvrdiIzbor();
            e.Handled = true;
        }
    }

    private void DgKonta_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PotvrdiIzbor();
            e.Handled = true;
        }
    }

    private void DgKonta_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PotvrdiIzbor();
    }

    private void PotvrdiIzbor()
    {
        if (DgKonta.SelectedItem is Konto izabran)
        {
            IzabraniKonto = izabran;
            DialogResult = true;
            Close();
        }
    }

    private void BtnIzaberi_Click(object sender, RoutedEventArgs e)
    {
        PotvrdiIzbor();
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}

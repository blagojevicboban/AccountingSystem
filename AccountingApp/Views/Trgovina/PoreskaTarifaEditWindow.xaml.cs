using System;
using System.Globalization;
using System.Windows;
using AccountingData;
using AccountingData.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Trgovina;

public partial class PoreskaTarifaEditWindow : Window
{
    private readonly PoreskaTarifa? _existingTarifa;

    public PoreskaTarifaEditWindow(PoreskaTarifa? existingTarifa = null)
    {
        InitializeComponent();
        _existingTarifa = existingTarifa;

        if (_existingTarifa != null)
        {
            TxtNaslov.Text = "✏️ Izmena poreske tarife";
            TxtTarifniBroj.Text = _existingTarifa.TarifniBroj;
            TxtTarifniBroj.IsReadOnly = true; // Tarifni broj se ne menja
            TxtPorezProcenat.Text = _existingTarifa.PorezProcenat.ToString("N2");
            TxtPosebanPorezProcenat.Text = _existingTarifa.PosebanPorezProcenat.ToString("N2");
            ChkPorezUCeni.IsChecked = _existingTarifa.PorezUCeni;
        }
        else
        {
            TxtNaslov.Text = "➕ Nova poreska tarifa";
            TxtPorezProcenat.Text = "0,00";
            TxtPosebanPorezProcenat.Text = "0,00";
        }
    }

    private async void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        string tarBrojText = TxtTarifniBroj.Text.Trim();

        if (!int.TryParse(tarBrojText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tarBroj) || tarBroj < 1 || tarBroj > 99)
        {
            MessageBox.Show("Tarifni broj mora biti ceo broj od 1 do 99.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtTarifniBroj.Focus();
            return;
        }

        if (!decimal.TryParse(TxtPorezProcenat.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal porezProcenat))
        {
            MessageBox.Show("Unesite ispravnu vrednost poreza.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPorezProcenat.Focus();
            return;
        }

        decimal.TryParse(TxtPosebanPorezProcenat.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal posebanPorezProcenat);

        string tarBrojNormalizovan = tarBroj.ToString(CultureInfo.InvariantCulture);
        bool porezUCeni = ChkPorezUCeni.IsChecked == true;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);

            if (_existingTarifa == null)
            {
                bool vecPostoji = await db.PoreskeTarife.AnyAsync(t => t.TarifniBroj == tarBrojNormalizovan);
                if (vecPostoji)
                {
                    MessageBox.Show($"Poreska tarifa sa brojem '{tarBrojNormalizovan}' već postoji.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtTarifniBroj.Focus();
                    return;
                }

                db.PoreskeTarife.Add(new PoreskaTarifa
                {
                    TarifniBroj = tarBrojNormalizovan,
                    PorezProcenat = porezProcenat,
                    PosebanPorezProcenat = posebanPorezProcenat,
                    PorezUCeni = porezUCeni
                });
            }
            else
            {
                var t = await db.PoreskeTarife.FirstOrDefaultAsync(x => x.PoreskaTarifaId == _existingTarifa.PoreskaTarifaId);
                if (t != null)
                {
                    t.PorezProcenat = porezProcenat;
                    t.PosebanPorezProcenat = posebanPorezProcenat;
                    t.PorezUCeni = porezUCeni;
                }
            }

            await db.SaveChangesAsync();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju poreske tarife:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

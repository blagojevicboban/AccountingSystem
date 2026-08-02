using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Shared;

public class NapredniFilterCriteria
{
    public DateTime? DatumOd { get; set; }
    public DateTime? DatumDo { get; set; }
    public decimal? IznosMin { get; set; }
    public decimal? IznosMax { get; set; }
    public string BrojDokumenta { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public int? SelectedPartnerId { get; set; }
    public bool? SamoProknjizeni { get; set; } // null = svi, true = samo proknjiženi, false = samo neproknjiženi

    public bool HasActiveFilter =>
        DatumOd.HasValue || DatumDo.HasValue || IznosMin.HasValue || IznosMax.HasValue ||
        !string.IsNullOrWhiteSpace(BrojDokumenta) || !string.IsNullOrWhiteSpace(Konto) ||
        SelectedPartnerId.HasValue || SamoProknjizeni.HasValue;
}

public partial class NaprednaPretragaWindow : Window
{
    private readonly AccountingDbContext _db;
    public NapredniFilterCriteria FilterCriteria { get; private set; }

    public NaprednaPretragaWindow(AccountingDbContext db, NapredniFilterCriteria? postojeciFilter = null)
    {
        InitializeComponent();
        _db = db;
        FilterCriteria = postojeciFilter ?? new NapredniFilterCriteria();

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += NaprednaPretragaWindow_Loaded;
    }

    private async void NaprednaPretragaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var partneri = await _db.Partneri.OrderBy(p => p.Naziv).ToListAsync();
            partneri.Insert(0, new Partner { PartnerId = 0, Naziv = "— Svi partneri —" });
            CmbPartneri.ItemsSource = partneri;

            if (FilterCriteria.DatumOd.HasValue) DpDatumOd.SelectedDate = FilterCriteria.DatumOd.Value;
            if (FilterCriteria.DatumDo.HasValue) DpDatumDo.SelectedDate = FilterCriteria.DatumDo.Value;

            if (FilterCriteria.IznosMin.HasValue) TxtIznosMin.Text = $"{FilterCriteria.IznosMin.Value:N2}";
            if (FilterCriteria.IznosMax.HasValue) TxtIznosMax.Text = $"{FilterCriteria.IznosMax.Value:N2}";

            TxtBrojDokumenta.Text = FilterCriteria.BrojDokumenta;
            TxtKonto.Text = FilterCriteria.Konto;

            if (FilterCriteria.SelectedPartnerId.HasValue)
            {
                CmbPartneri.SelectedValue = FilterCriteria.SelectedPartnerId.Value;
            }

            if (FilterCriteria.SamoProknjizeni == true) RbStatusProknjizeni.IsChecked = true;
            else if (FilterCriteria.SamoProknjizeni == false) RbStatusNeproknjizeni.IsChecked = true;
            else RbStatusSvi.IsChecked = true;
        }
        catch
        {
            // fallback
        }
    }

    private void BtnPrimeni_Click(object sender, RoutedEventArgs e)
    {
        FilterCriteria.DatumOd = DpDatumOd.SelectedDate;
        FilterCriteria.DatumDo = DpDatumDo.SelectedDate;

        FilterCriteria.IznosMin = ParseDecimal(TxtIznosMin.Text);
        FilterCriteria.IznosMax = ParseDecimal(TxtIznosMax.Text);

        FilterCriteria.BrojDokumenta = TxtBrojDokumenta.Text.Trim();
        FilterCriteria.Konto = TxtKonto.Text.Trim();

        if (CmbPartneri.SelectedValue is int pId && pId > 0)
        {
            FilterCriteria.SelectedPartnerId = pId;
        }
        else
        {
            FilterCriteria.SelectedPartnerId = null;
        }

        if (RbStatusProknjizeni.IsChecked == true) FilterCriteria.SamoProknjizeni = true;
        else if (RbStatusNeproknjizeni.IsChecked == true) FilterCriteria.SamoProknjizeni = false;
        else FilterCriteria.SamoProknjizeni = null;

        DialogResult = true;
        Close();
    }

    private void BtnPonisti_Click(object sender, RoutedEventArgs e)
    {
        FilterCriteria = new NapredniFilterCriteria();
        DpDatumOd.SelectedDate = null;
        DpDatumDo.SelectedDate = null;
        TxtIznosMin.Text = "";
        TxtIznosMax.Text = "";
        TxtBrojDokumenta.Text = "";
        TxtKonto.Text = "";
        CmbPartneri.SelectedIndex = 0;
        RbStatusSvi.IsChecked = true;
    }

    private static decimal? ParseDecimal(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return null;
        val = val.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

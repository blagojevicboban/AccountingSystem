using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingApp.Views.Nalozi;
using AccountingData;
using AccountingData.Models;
using AccountingData.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Dms;

public partial class DmsOcrPreviewWindow : Window
{
    private readonly string _putanjaFajla;
    private readonly AccountingDbContext _db;
    private readonly DmsOcrInvoiceParser _parser;
    private readonly DmsOcrMatchingService _matchingService;
    private OcrRacunResult _ocrResult = new();
    private List<Partner> _partneri = new();

    public bool JeKnjizeno { get; private set; }

    public DmsOcrPreviewWindow(string putanjaFajla, AccountingDbContext db)
    {
        InitializeComponent();
        _putanjaFajla = putanjaFajla;
        _db = db;
        _parser = new DmsOcrInvoiceParser();
        _matchingService = new DmsOcrMatchingService(_db);

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };

        Loaded += DmsOcrPreviewWindow_Loaded;
    }

    private async void DmsOcrPreviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            _partneri = await _db.Partneri.OrderBy(p => p.Naziv).ToListAsync();
            CmbPartneri.ItemsSource = _partneri;

            _ocrResult = await _parser.ProcessDocumentAsync(_putanjaFajla);
            await _matchingService.ProcessOcrMatchingAsync(_ocrResult);

            Mouse.OverrideCursor = null;

            PopuniFormuIzOcr();
        }
        catch (Exception ex)
        {
            Mouse.OverrideCursor = null;
            MessageBox.Show($"Greška pri OCR obradi fajla: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PopuniFormuIzOcr()
    {
        TxtStatus.Text = _ocrResult.StatusPoruka;
        TxtPib.Text = _ocrResult.PibDobavljaca;
        TxtBrojRacuna.Text = _ocrResult.BrojRacuna;

        if (_ocrResult.DatumRacuna.HasValue) DpDatumRacuna.SelectedDate = _ocrResult.DatumRacuna.Value;
        if (_ocrResult.ValutaDospela.HasValue) DpValuta.SelectedDate = _ocrResult.ValutaDospela.Value;

        TxtOsnovica.Text = $"{_ocrResult.OsnovicaNeto:N2}";
        TxtPdv.Text = $"{_ocrResult.PdvIznos:N2}";
        TxtUkupno.Text = $"{_ocrResult.UkupanIznosBruto:N2}";

        TxtRawText.Text = _ocrResult.RawText;

        if (_ocrResult.UpareniPartnerId.HasValue)
        {
            CmbPartneri.SelectedValue = _ocrResult.UpareniPartnerId.Value;
        }
    }

    private void TxtPib_TextChanged(object sender, TextChangedEventArgs e)
    {
        string pib = TxtPib.Text.Trim();
        if (!string.IsNullOrEmpty(pib))
        {
            var match = _partneri.FirstOrDefault(p => p.Pib != null && p.Pib.Trim() == pib);
            if (match != null)
            {
                CmbPartneri.SelectedValue = match.PartnerId;
            }
        }
    }

    private void BtnPripremiNalog_Click(object sender, RoutedEventArgs e)
    {
        // Prikupljanje eventualno korigovanih vrednosti iz UI forme
        _ocrResult.PibDobavljaca = TxtPib.Text.Trim();
        _ocrResult.BrojRacuna = TxtBrojRacuna.Text.Trim();
        _ocrResult.DatumRacuna = DpDatumRacuna.SelectedDate ?? DateTime.Today;
        _ocrResult.ValutaDospela = DpValuta.SelectedDate ?? DateTime.Today.AddDays(15);

        if (CmbPartneri.SelectedValue is int pId)
        {
            _ocrResult.UpareniPartnerId = pId;
        }

        _ocrResult.OsnovicaNeto = ParseDecimal(TxtOsnovica.Text);
        _ocrResult.PdvIznos = ParseDecimal(TxtPdv.Text);
        _ocrResult.UkupanIznosBruto = ParseDecimal(TxtUkupno.Text);

        if (_ocrResult.UkupanIznosBruto <= 0 && _ocrResult.OsnovicaNeto > 0)
        {
            _ocrResult.UkupanIznosBruto = _ocrResult.OsnovicaNeto + _ocrResult.PdvIznos;
        }

        var generisaneStavke = DmsOcrMatchingService.GenerisiStavkeNalogaZaUlazniRacun(_ocrResult);

        var nalogNacrt = new Nalog
        {
            VrstaNaloga = "Ulazni račun",
            DatumNaloga = _ocrResult.DatumRacuna ?? DateTime.Today,
            Opis = $"Ulazni račun br. {_ocrResult.BrojRacuna} od {_ocrResult.DatumRacuna:dd.MM.yyyy}",
            IsKnjizen = false,
            Stavke = generisaneStavke
        };

        var editDijalog = new NalogEditWindow(nalogNacrt) { Owner = this };
        if (editDijalog.ShowDialog() == true)
        {
            JeKnjizeno = true;
            DialogResult = true;
            Close();
        }
    }

    private static decimal ParseDecimal(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0m;
        val = val.Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

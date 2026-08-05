using System;
using System.Linq;
using System.Windows;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.PutniNalozi;

/// <summary>
/// Izvozi deo dnevnice iznad zakonskog neoporezivog iznosa u JSON koji ERPiZarade uvozi u
/// obračun zarade (Faza 3.2). Prvo pokazuje šta je pronađeno i šta bi izvoz izostavio — isti
/// obrazac kao uvoz naloga za knjiženje u <c>NaloziView</c>, samo u suprotnom smeru.
/// </summary>
public partial class IzvozZaZaradeWindow : Window
{
    private string? _spremanJson;
    private int _brojStavki;

    public IzvozZaZaradeWindow()
    {
        InitializeComponent();

        for (int m = 1; m <= 12; m++)
            CmbMesec.Items.Add(new System.Windows.Controls.ComboBoxItem
            {
                Content = $"{m:D2} — {System.Globalization.CultureInfo.GetCultureInfo("sr-Latn-RS").DateTimeFormat.GetMonthName(m)}",
                Tag = m
            });
        CmbMesec.SelectedIndex = DateTime.Today.Month - 1;

        int godinaSad = DateTime.Today.Year;
        for (int g = godinaSad - 1; g <= godinaSad + 1; g++)
            CmbGodina.Items.Add(g);
        CmbGodina.SelectedItem = godinaSad;
    }

    private async void BtnPripremi_Click(object sender, RoutedEventArgs e)
    {
        if (CmbMesec.SelectedItem is not System.Windows.Controls.ComboBoxItem mesecItem
            || CmbGodina.SelectedItem is not int godina)
        {
            return;
        }

        int mesec = (int)mesecItem.Tag;

        BtnSacuvaj.IsEnabled = false;
        _spremanJson = null;
        TxtStatus.Text = "Pripremam...";

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? AppSession.TrenutnaFirma;

            var (json, nalazi, brojStavki) = await PutniNaloziZaZaradeWriter.GenerisiAsync(db, firma, godina, mesec);

            var stavke = await db.PutniNalozi
                .Where(p => p.IsKnjizeno && p.Vrsta == VrstaSlužbenogPutovanja.Zemlja
                         && p.DatumPovratka.Year == godina && p.DatumPovratka.Month == mesec)
                .ToListAsync();

            // Prikaz u tabeli računa prekoračenje po istom pravilu kao writer, da korisnik
            // vidi tačno ono što će ući u fajl — bez ponovnog izračunavanja u writer-u.
            var servis = new PutniNalogService(db);
            var prikaz = new System.Collections.Generic.List<object>();
            foreach (var pn in stavke)
            {
                decimal limit = await servis.VaziciNeoporeziviIznosAsync(pn.DatumPovratka);
                decimal prekoracenje = limit > 0
                    ? PutniNalogService.PrekoracenjeDnevnice(pn.UkupnoDnevnice, pn.BrojDnevnica, limit)
                    : 0m;

                if (prekoracenje <= 0m || string.IsNullOrWhiteSpace(pn.Jmbg)) continue;

                prikaz.Add(new
                {
                    pn.Jmbg,
                    pn.ZaposleniIme,
                    pn.BrojNaloga,
                    DatumPovratka = pn.DatumPovratka.ToString("dd.MM.yyyy"),
                    pn.UkupnoDnevnice,
                    PrekoracenjeDnevnice = prekoracenje
                });
            }

            DgStavke.ItemsSource = prikaz;

            if (nalazi.Count > 0)
            {
                ListaNalaza.ItemsSource = nalazi
                    .Select(n => $"[{n.TezinaTekst}] {n.Provera}: {n.Opis}")
                    .ToList();
                PanelNalazi.Visibility = Visibility.Visible;
            }
            else
            {
                PanelNalazi.Visibility = Visibility.Collapsed;
            }

            _spremanJson = json;
            _brojStavki = brojStavki;

            TxtStatus.Text = json != null
                ? $"Spremno za izvoz: {brojStavki} stavki za {mesec:D2}/{godina}."
                : $"Nema ničega za izvoz za {mesec:D2}/{godina}.";

            BtnSacuvaj.IsEnabled = json != null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri pripremi izvoza: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "";
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (_spremanJson == null) return;

        var mesecItem = (System.Windows.Controls.ComboBoxItem)CmbMesec.SelectedItem;
        int mesec = (int)mesecItem.Tag;
        int godina = (int)CmbGodina.SelectedItem;

        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Putni nalozi za ERPiZarade (*.json)|*.json",
            FileName = $"PutniNaloziZaZarade_{godina}_{mesec:D2}.json",
            Title = "Sačuvaj izvoz za ERPiZarade"
        };

        if (sfd.ShowDialog() != true) return;

        try
        {
            System.IO.File.WriteAllText(sfd.FileName, _spremanJson);
            MessageBox.Show(
                $"Izvezeno {_brojStavki} stavki u:\n{sfd.FileName}\n\n" +
                "Uvezite fajl u ERPiZarade (💸 Isplate u mesecu → Primanja → „Uvoz iz ERPiFinansije\").",
                "Izvoz završen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fajl nije sačuvan: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e) => Close();
}

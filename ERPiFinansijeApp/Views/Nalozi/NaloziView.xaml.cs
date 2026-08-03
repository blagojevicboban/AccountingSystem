using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERPiFinansijeApp.Services;
using ERPiFinansijeData;
using ERPiFinansijeData.Models;
using ERPiFinansijeData.Services;
using Microsoft.EntityFrameworkCore;

namespace ERPiFinansijeApp.Views.Nalozi;

public partial class NaloziView : UserControl
{
    private List<Nalog> _allNalozi = new();
    private Dictionary<int, string> _promeneMap = new();
    private Shared.NapredniFilterCriteria _napredniFilter = new();

    public NaloziView()
    {
        InitializeComponent();
        RbProknjizeni.IsChecked = true;
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
            _promeneMap = await new PromenaService(db).GetMapAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyFilter()
    {
        if (DgNalozi == null) return;

        string search = TxtPretraga.Text.Trim().ToLower();

        bool samoProknjizeni = RbProknjizeni?.IsChecked == true;
        bool samoNeproknjizeni = RbNeproknjizeni?.IsChecked == true;

        var filtered = _allNalozi.Where(n =>
            (string.IsNullOrEmpty(search) || n.BrojNaloga.ToString().Contains(search) || (n.Opis != null && n.Opis.ToLower().Contains(search))) &&
            (!samoProknjizeni || n.IsKnjizen) &&
            (!samoNeproknjizeni || !n.IsKnjizen) &&
            (!_napredniFilter.DatumOd.HasValue || n.DatumNaloga >= _napredniFilter.DatumOd.Value.Date) &&
            (!_napredniFilter.DatumDo.HasValue || n.DatumNaloga <= _napredniFilter.DatumDo.Value.Date.AddDays(1).AddTicks(-1)) &&
            (!_napredniFilter.IznosMin.HasValue || n.UkupnoDuguje >= _napredniFilter.IznosMin.Value) &&
            (!_napredniFilter.IznosMax.HasValue || n.UkupnoDuguje <= _napredniFilter.IznosMax.Value) &&
            (string.IsNullOrEmpty(_napredniFilter.BrojDokumenta) || n.BrojNaloga.ToString().Contains(_napredniFilter.BrojDokumenta) || (n.Opis != null && n.Opis.Contains(_napredniFilter.BrojDokumenta, StringComparison.OrdinalIgnoreCase))) &&
            (string.IsNullOrEmpty(_napredniFilter.Konto) || (n.Stavke != null && n.Stavke.Any(s => s.BrojKonta.Contains(_napredniFilter.Konto, StringComparison.OrdinalIgnoreCase)))) &&
            (!_napredniFilter.SelectedPartnerId.HasValue || (n.Stavke != null && n.Stavke.Any(s => s.PartnerId == _napredniFilter.SelectedPartnerId.Value)))
        ).OrderByDescending(n => n.BrojNaloga).ToList();

        DgNalozi.ItemsSource = filtered;
        ColBrojNaloga.SortDirection = ListSortDirection.Descending;
        if (filtered.Any())
        {
            DgNalozi.SelectedIndex = 0;
        }
        else
        {
            DgStavke.ItemsSource = null;
        }

        AzurirajDugmad();
    }

    /// <summary>
    /// Izmeni/Proknjiži/Rasknjiži/Štampa zahtevaju izabran nalog u gridu.
    /// </summary>
    private void AzurirajDugmad()
    {
        var selectedNalog = DgNalozi.SelectedItem as Nalog;
        bool imaSelekciju = selectedNalog != null;
        bool isKnjizen = selectedNalog?.IsKnjizen == true;

        BtnIzmeniNalog.IsEnabled = imaSelekciju;
        BtnStampa.IsEnabled = imaSelekciju;

        bool mozeKnjiziti = imaSelekciju && !isKnjizen;
        BtnProknjizi.IsEnabled = mozeKnjiziti;
        if (CmiProknjizi != null) CmiProknjizi.IsEnabled = mozeKnjiziti;

        bool mozeRasknjiziti = imaSelekciju && isKnjizen;
        BtnRasknjizi.IsEnabled = mozeRasknjiziti;
        if (CmiRasknjizi != null) CmiRasknjizi.IsEnabled = mozeRasknjiziti;

        bool samoProknjizeni = RbProknjizeni?.IsChecked == true;
        bool imaNeproknjizenih = _allNalozi != null && _allNalozi.Any(n => !n.IsKnjizen);
        BtnProknjiziSve.IsEnabled = !samoProknjizeni && imaNeproknjizenih;

        if (CmiIzmeni != null) CmiIzmeni.IsEnabled = imaSelekciju;
    }

    private void DataGridRow_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
        {
            row.Focus();
            if (!row.IsSelected)
            {
                DgNalozi.SelectedItem = row.DataContext;
            }
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
            DgStavke.ItemsSource = selectedNalog.Stavke.Select(s => new
            {
                s.RedniBroj,
                s.BrojKonta,
                s.Opis,
                OpisPromene = s.PromenaKod.HasValue
                    ? (_promeneMap.TryGetValue(s.PromenaKod.Value, out var opis) ? opis : s.PromenaKod.Value.ToString())
                    : "",
                s.Duguje,
                s.Potrazuje
            }).ToList();
        }

        AzurirajDugmad();
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

        OtvoriIzmenuNaloga(selectedNalog);
    }

    private void DgNalozi_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject) == null) return;
        if (DgNalozi.SelectedItem is not Nalog selectedNalog) return;

        OtvoriIzmenuNaloga(selectedNalog);
    }

    private async void OtvoriIzmenuNaloga(Nalog nalog)
    {
        if (nalog.IsKnjizen)
        {
            var odgovor = MessageBox.Show(
                $"Nalog #{nalog.BrojNaloga} je proknjižen i ne može se menjati u ovom statusu.\n\nDa li želite da ga rasknjižite radi izmene?",
                "Proknjižen nalog", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (odgovor != MessageBoxResult.Yes) return;

            if (!AppSession.IsAdministrator)
            {
                MessageBox.Show("Rasknjižavanje naloga dozvoljeno je samo administratoru.", "Nedozvoljena akcija", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var options = new DbContextOptionsBuilder<AccountingDbContext>()
                    .UseSqlite($"Data Source={AppConfig.DbPath}")
                    .Options;

                using var db = new AccountingDbContext(options);
                var service = new NaloziService(db);

                await service.RasknjiziNalogAsync(nalog.NalogId, AppSession.TrenutniKorisnik?.KorisnikId, AppSession.TrenutniKorisnik?.KorisnickoIme);
                int nalogId = nalog.NalogId;
                
                _allNalozi = await service.GetNaloziAsync();
                _promeneMap = await new PromenaService(db).GetMapAsync();
                ApplyFilter();

                var osvezeniNalog = _allNalozi.FirstOrDefault(n => n.NalogId == nalogId);
                if (osvezeniNalog != null)
                {
                    var dijalog = new NalogEditWindow(osvezeniNalog) { Owner = Window.GetWindow(this) };
                    if (dijalog.ShowDialog() == true)
                    {
                        LoadNalozi();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri rasknjižavanju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        var editDijalog = new NalogEditWindow(nalog) { Owner = Window.GetWindow(this) };
        if (editDijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }

    private void BtnNapredniFilter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var win = new Shared.NaprednaPretragaWindow(db, _napredniFilter) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true)
            {
                _napredniFilter = win.FilterCriteria;
                if (_napredniFilter.HasActiveFilter)
                {
                    BtnNapredniFilter.Background = System.Windows.Media.Brushes.DarkOrange;
                }
                else
                {
                    BtnNapredniFilter.Background = (System.Windows.Media.Brush)FindResource("PrimaryLightBrush");
                }
                ApplyFilter();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri filtriranju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnUvozIzvoda_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var win = new Izvodi.UvozIzvodaWindow(db) { Owner = Window.GetWindow(this) };
            if (win.ShowDialog() == true || win.JeProknjizeno)
            {
                LoadNalozi();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri otvaranju uvoza izvoda: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Uvoz naloga za knjiženje iz ERPiZarade.
    ///
    /// Nalog se prvo pročita i pokaže, pa tek po potvrdi snimi — i to kao
    /// <b>neproknjižen</b>. Knjiženje ostaje odluka korisnika, kao i kod svakog drugog naloga.
    /// </summary>
    private async void BtnUvozZarada_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Nalog iz ERPiZarade (*.json)|*.json|Svi fajlovi (*.*)|*.*",
            Title = "Izaberite nalog za knjiženje izvezen iz ERPiZarade"
        };

        if (ofd.ShowDialog() != true) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new ZaradeImportService(db);

            var rezultat = await service.ProcitajAsync(ofd.FileName);

            // Konta koja nedostaju su jedina greška koja se rešava na licu mesta: kad ih
            // korisnik potvrdi, zavedu se i fajl se čita ponovo. Provera time nije zaobiđena
            // nego rešena — posle zavođenja konto postoji, pa iznos ima svoju karticu.
            if (!rezultat.SmeSeUvesti && rezultat.KontaKojaNedostaju.Count > 0
                && await PonudiZavodjenjeKontaAsync(service, rezultat))
            {
                rezultat = await service.ProcitajAsync(ofd.FileName);
            }

            if (!rezultat.SmeSeUvesti)
            {
                MessageBox.Show(
                    "Nalog nije uvezen:\n\n" +
                    string.Join(Environment.NewLine,
                        rezultat.Nalazi
                            .Where(n => n.Tezina == TezinaNalazaUvoza.Greska)
                            .Take(10)
                            .Select(n => $"• {n.Provera}: {n.Opis}")),
                    "Uvoz zaustavljen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var nalog = rezultat.Nalog!;

            string poruka =
                $"Iz fajla je pročitano:\n\n" +
                $"• Firma: {rezultat.FirmaNaziv}\n" +
                $"• Period: {rezultat.Mesec:D2}/{rezultat.Godina}" +
                (rezultat.RedniBrojIsplate > 1 ? $", isplata {rezultat.RedniBrojIsplate}" : "") + "\n" +
                $"• Opis: {nalog.Opis}\n" +
                $"• Stavki: {nalog.Stavke.Count}\n" +
                $"• Duguje: {nalog.UkupnoDuguje:N2}   Potražuje: {nalog.UkupnoPotrazuje:N2}\n" +
                $"• Broj naloga koji će dobiti: {nalog.BrojNaloga}\n";

            var upozorenja = rezultat.Nalazi.Where(n => n.Tezina == TezinaNalazaUvoza.Upozorenje).ToList();
            if (upozorenja.Count > 0)
            {
                poruka += "\nProveriti:\n" +
                          string.Join(Environment.NewLine, upozorenja.Select(n => $"• {n.Provera}: {n.Opis}")) + "\n";
            }

            if (rezultat.MogucDuplikat != null)
            {
                poruka += $"\nPAŽNJA: nalog #{rezultat.MogucDuplikat.BrojNaloga} istog opisa i datuma " +
                          "već postoji. Verovatno je ovaj fajl već uvezen.\n";
            }

            poruka += "\nUvesti nalog? Ostaje neproknjižen dok ga sami ne proknjižite.";

            if (MessageBox.Show(poruka, "Potvrda uvoza", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            await service.UveziAsync(nalog);

            LoadNalozi();

            MessageBox.Show($"Uvezen nalog #{nalog.BrojNaloga} sa {nalog.Stavke.Count} stavki.",
                "Uvoz završen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri uvozu naloga: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Nudi da se konta koja nalogu nedostaju zavedu u kontni plan, sa nazivima iz Kontnog
    /// okvira. Vraća <c>true</c> ako je bar jedan konto zaveden, pa se fajl čita ponovo.
    ///
    /// Postoji zato što ERPiFinansije nema podrazumevani kontni plan: nova faza u ERPiZarade
    /// donese konto koji firma nikad nije otvorila, i prvi uvoz stane. Nazivi se pokazuju
    /// pre potvrde — konto se zavodi u knjige, a ne usput.
    /// </summary>
    private static async Task<bool> PonudiZavodjenjeKontaAsync(
        ZaradeImportService service, RezultatCitanjaZarada rezultat)
    {
        var konta = rezultat.KontaKojaNedostaju;

        // Druge greške (nalog van ravnoteže, pogrešan format) se zavođenjem konta ne rešavaju,
        // pa nema smisla ni nuditi ga — korisnik bi zaveo konta i opet ostao bez uvoza.
        bool samoKonta = rezultat.Nalazi
            .Where(n => n.Tezina == TezinaNalazaUvoza.Greska)
            .All(n => n.Provera == "Konto ne postoji u kontnom planu");

        if (!samoKonta) return false;

        bool sviIzOkvira = konta.All(k => k.IzKontnogOkvira);

        string poruka =
            $"Kontni plan nema {konta.Count} konta koja ovaj nalog traži:\n\n" +
            string.Join(Environment.NewLine, konta.Select(k => $"• {k.Prikaz}")) + "\n\n" +
            (sviIzOkvira
                ? "Nazivi su iz Pravilnika o Kontnom okviru."
                : "Za konta koja Kontni okvir ne prepoznaje predložen je opis stavke iz naloga.") +
            "\n\nZavesti ih u kontni plan? Naziv i ostalo možete izmeniti kasnije u „Kontnom planu“.";

        if (MessageBox.Show(poruka, "Nedostaju konta", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes)
            return false;

        int dodato = await service.ZavediKontaAsync(konta);

        return dodato > 0;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match) return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
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

            await service.RasknjiziNalogAsync(selectedNalog.NalogId, AppSession.TrenutniKorisnik?.KorisnikId, AppSession.TrenutniKorisnik?.KorisnickoIme);

            var otvoriIzmenu = MessageBox.Show(
                $"Nalog #{selectedNalog.BrojNaloga} je rasknjižen.\n\nDa li želite odmah da ga izmenite?",
                "Uspeh", MessageBoxButton.YesNo, MessageBoxImage.Information);

            int nalogId = selectedNalog.NalogId;
            LoadNalozi();

            if (otvoriIzmenu == MessageBoxResult.Yes)
            {
                var nalog = _allNalozi.FirstOrDefault(n => n.NalogId == nalogId);
                if (nalog != null)
                {
                    var dijalog = new NalogEditWindow(nalog) { Owner = Window.GetWindow(this) };
                    if (dijalog.ShowDialog() == true)
                    {
                        LoadNalozi();
                    }
                }
            }
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

    private async void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        var selektovaniNalozi = DgNalozi.SelectedItems.OfType<Nalog>().ToList();

        if (!selektovaniNalozi.Any() && DgNalozi.SelectedItem is Nalog singleNalog)
        {
            selektovaniNalozi.Add(singleNalog);
        }

        if (!selektovaniNalozi.Any())
        {
            MessageBox.Show("Molimo izaberite jedan ili više naloga za štampu.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var firma = await db.Firme.FirstOrDefaultAsync() ?? AppSession.TrenutnaFirma ?? new Firma { Naziv = "Firma" };

            var nalogIds = selektovaniNalozi.Select(n => n.NalogId).ToList();
            var naloziSaStavkama = await db.Nalozi
                .Include(n => n.Stavke)
                .Where(n => nalogIds.Contains(n.NalogId))
                .ToListAsync();

            var nalogeForPdf = selektovaniNalozi
                .Select(s => naloziSaStavkama.FirstOrDefault(n => n.NalogId == s.NalogId) ?? s)
                .ToList();

            byte[] pdfBytes = PdfReportService.GenerisiNalogePdf(firma, nalogeForPdf);

            string fileName = nalogeForPdf.Count == 1 
                ? $"Nalog_{nalogeForPdf[0].BrojNaloga}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                : $"Nalozi_Vise_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            string pdfPath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            Process.Start(new ProcessStartInfo { FileName = pdfPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF štampe: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnProknjiziSve_Click(object sender, RoutedEventArgs e)
    {
        var unpostedCount = _allNalozi.Count(n => !n.IsKnjizen);
        if (unpostedCount == 0)
        {
            MessageBox.Show("Nema neproknjiženih naloga u bazi.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var potvrda = MessageBox.Show(
            $"Da li želite da proknjižite sve neproknjižene naloge (ukupno {unpostedCount} naloga)?",
            "Potvrda masovnog knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (potvrda != MessageBoxResult.Yes) return;

        try
        {
            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={AppConfig.DbPath}")
                .Options;

            using var db = new AccountingDbContext(options);
            var service = new NaloziService(db);

            var (proknjizenoCount, neuravnotezeni) = await service.KnjiziSveNalogeAsync();

            string poruka = $"Uspešno je proknjiženo {proknjizenoCount} naloga!";
            if (neuravnotezeni.Count > 0)
            {
                poruka += $"\n\nSledeći nalozi nisu u ravnoteži i ostali su neproknjiženi: {string.Join(", ", neuravnotezeni)}";
            }

            MessageBox.Show(poruka, "Masovno knjiženje", MessageBoxButton.OK, 
                neuravnotezeni.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            LoadNalozi();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri masovnom knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnPreknjizavanje_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new PreknjizavanjeWindow { Owner = Window.GetWindow(this) };
        if (dijalog.ShowDialog() == true)
        {
            LoadNalozi();
        }
    }

    private void BtnExportExcelNalozi_Click(object sender, RoutedEventArgs e)
        => ExcelExportService.ExportDataGridToExcel(DgNalozi, "Nalozi za knjiženje", "Nalozi_Glavna_Knjiga");
}

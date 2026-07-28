using System.Windows;
using System.Windows.Controls;
using AccountingApp.Views.Backup;
using AccountingApp.Views.Dashboard;
using AccountingApp.Views.Izvestaji;
using AccountingApp.Views.Kartice;
using AccountingApp.Views.Magacin;
using AccountingApp.Views.Nalozi;
using AccountingApp.Views.Partneri;
using AccountingApp.Views.Pomoc;
using AccountingApp.Views.Trgovina;
using AccountingData;

namespace AccountingApp;

public partial class MainWindow : Window
{
    private readonly AccountingDbContext _db;

    public MainWindow(AccountingDbContext db)
    {
        InitializeComponent();
        _db = db;

        WindowState = UserSettings.Instance.StartMaximized ? WindowState.Maximized : WindowState.Normal;

        AppSession.TrenutnaFirmaChanged += () => Dispatcher.Invoke(UpdateFirmaInfo);
        UpdateFirmaInfo();
        UpdateKorisnikInfo();

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        VersionText.Text = $"v{versionStr}  •  {System.DateTime.Now.Year}";

        NavigateToDashboard();

        // Provera ažuriranja u pozadini
        _ = CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            var source = new Velopack.Sources.GithubSource(
                "https://github.com/blagojevicboban/AccountingSystem",
                null,
                false);
            var mgr = new Velopack.UpdateManager(source);
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                var dialog = new UpdateDialog(newVersion, mgr);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri proveri ažuriranja: {ex.Message}");
        }
    }

    private void UpdateFirmaInfo()
    {
        TxtFirmaNaziv.Text = AppSession.TrenutnaFirma?.Naziv ?? "—";
        TxtFirmaSifra.Text = AppSession.TrenutnaFirma?.Sifra ?? "—";
    }

    private void UpdateKorisnikInfo()
    {
        var uloga = AppSession.TrenutniKorisnik?.Uloga ?? "Knjigovođa";
        TxtKorisnik.Text = $"👤 {AppSession.TrenutniKorisnik?.ImeIPrezime ?? "—"} ({uloga})";
        TxtImeKorisnika.Text = AppSession.TrenutniKorisnik?.ImeIPrezime ?? "—";
        TxtUlogaKorisnika.Text = uloga;
        ApplyRolePermissions();
    }

    private void ApplyRolePermissions()
    {
        if (AppSession.TrenutniKorisnik?.Uloga == "Gledalac")
        {
            BtnPodesavanja.IsEnabled = false;
            BtnBackup.IsEnabled = false;
        }
    }

    private void NavigateToDashboard()
    {
        TxtHeaderTitle.Text = "📊 Radna tabla";
        MainContentHost.Content = new DashboardView();
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        NavigateToDashboard();
    }

    private void NavKonta_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 Kontni plan (Šifarnik konta)";
        MainContentHost.Content = new Views.Konta.KontaView();
    }

    private void NavNalozi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📖 Glavna knjiga (Nalozi za knjiženje)";
        MainContentHost.Content = new NaloziView();
    }

    private void NavKartice_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 Kartice konta";
        MainContentHost.Content = new KarticeView();
    }

    private void NavPartneri_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "👥 Partneri (Analitika i otvorene stavke)";
        MainContentHost.Content = new PartneriView();
    }

    private void NavMagacin_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📦 Magacini i zalihe";
        MainContentHost.Content = new MagacinView();
    }

    private void NavKalkulacije_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📦 Robno knjigovodstvo (Kalkulacije, Otpremnice, Nivelacije, Robne kartice, Računopolagači)";
        MainContentHost.Content = new TrgovinaView();
    }

    private void NavIzvestaji_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📄 Izveštaji i PDF";
        MainContentHost.Content = new IzvestajiView();
    }

    private void NavBilansi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏛️ Zvanični Finansijski Izveštaji za APR";
        MainContentHost.Content = new Views.Bilansi.BilansiView();
    }

    private void NavPdv_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🧾 PDV Evidencija (KIR i KPR)";
        MainContentHost.Content = new Views.Pdv.PdvEvidencijaView();
    }

    private void NavFirme_Click(object sender, RoutedEventArgs e)
    {
        NavigateToFirme();
    }

    private void FirmaBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BtnFirme.IsChecked = true;
        NavigateToFirme();
    }

    private void NavigateToFirme()
    {
        TxtHeaderTitle.Text = "🏢 Upravljanje firmama";
        MainContentHost.Content = new Views.Firme.FirmeView();
    }

    private void NavPodesavanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚙️ Podešavanja aplikacije";
        MainContentHost.Content = new Views.Podesavanja.PodesavanjaView();
    }

    private void NavBackup_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "💾 Rezervne kopije i restauracija (Backup & Restore)";
        MainContentHost.Content = new BackupView();
    }

    private void NavKorisnici_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "👤 Administracija korisnika i uloga (RBAC)";
        MainContentHost.Content = new Views.Korisnici.KorisniciView(_db);
    }

    private void NavPomoc_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "❓ Pomoć";
        MainContentHost.Content = new PomocView();
    }

    private void BtnOdjava_Click(object sender, RoutedEventArgs e)
    {
        AppSession.TrenutniKorisnik = null;
        var loginWindow = new Views.Korisnici.LoginWindow(_db);
        loginWindow.Show();
        Close();
    }

    private void BtnChangelog_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new ChangelogWindow { Owner = this };
        dijalog.ShowDialog();
    }

    private void VersionText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var dijalog = new ChangelogWindow { Owner = this };
        dijalog.ShowDialog();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((e.Key == System.Windows.Input.Key.F || e.Key == System.Windows.Input.Key.K) && 
            (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            TxtSearchNav.Focus();
            TxtSearchNav.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.M && 
                 (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
        {
            BtnToggleSidebar_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F1)
        {
            NavPomoc_Click(sender, e);
            e.Handled = true;
        }
    }

    private void BtnToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        if (SidebarColumn.Width.Value > 100)
        {
            SidebarColumn.Width = new GridLength(64);
            TxtBrandTitle.Visibility = Visibility.Collapsed;
            TxtBrandSubtitle.Visibility = Visibility.Collapsed;
            PnlSearchNav.Visibility = Visibility.Collapsed;
            PnlFirmaDetails.Visibility = Visibility.Collapsed;
            PnlPromeniBadge.Visibility = Visibility.Collapsed;
            HeaderFinansije.Visibility = Visibility.Collapsed;
            HeaderRobno.Visibility = Visibility.Collapsed;
            HeaderPorezi.Visibility = Visibility.Collapsed;
            HeaderPodesavanja.Visibility = Visibility.Collapsed;
            HeaderDokumentacija.Visibility = Visibility.Collapsed;
        }
        else
        {
            SidebarColumn.Width = new GridLength(240);
            TxtBrandTitle.Visibility = Visibility.Visible;
            TxtBrandSubtitle.Visibility = Visibility.Visible;
            PnlSearchNav.Visibility = Visibility.Visible;
            PnlFirmaDetails.Visibility = Visibility.Visible;
            PnlPromeniBadge.Visibility = Visibility.Visible;
            HeaderFinansije.Visibility = Visibility.Visible;
            HeaderRobno.Visibility = Visibility.Visible;
            HeaderPorezi.Visibility = Visibility.Visible;
            HeaderPodesavanja.Visibility = Visibility.Visible;
            HeaderDokumentacija.Visibility = Visibility.Visible;
        }
    }

    private void TxtSearchNav_GotFocus(object sender, RoutedEventArgs e)
    {
        TxtSearchPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void TxtSearchNav_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSearchNav.Text))
        {
            TxtSearchPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private void TxtSearchNav_TextChanged(object sender, TextChangedEventArgs e)
    {
        TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearchNav.Text) ? Visibility.Visible : Visibility.Collapsed;
        var query = TxtSearchNav.Text.Trim().ToLowerInvariant();

        foreach (var child in PnlNavigation.Children)
        {
            if (child is RadioButton rb)
            {
                var text = rb.Content?.ToString()?.ToLowerInvariant() ?? "";
                var toolTip = rb.ToolTip?.ToString()?.ToLowerInvariant() ?? "";
                rb.Visibility = (string.IsNullOrEmpty(query) || text.Contains(query) || toolTip.Contains(query)) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            else if (child is Button b)
            {
                var text = b.Content?.ToString()?.ToLowerInvariant() ?? "";
                b.Visibility = (string.IsNullOrEmpty(query) || text.Contains(query)) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            else if (child is Separator s)
            {
                s.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (child is TextBlock tb)
            {
                tb.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
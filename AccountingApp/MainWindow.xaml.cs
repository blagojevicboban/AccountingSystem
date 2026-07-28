using System.Windows;
using System.Windows.Controls;
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

    private void FirmaBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        foreach (var child in PnlNavigation.Children)
        {
            if (child is RadioButton rb) rb.IsChecked = false;
        }

        TxtHeaderTitle.Text = "🏢 Upravljanje firmama";
        MainContentHost.Content = new Views.Firme.FirmeView();
    }

    private void NavPodesavanja_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "⚙️ Podešavanja aplikacije";
        MainContentHost.Content = new Views.Podesavanja.PodesavanjaView();
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
}
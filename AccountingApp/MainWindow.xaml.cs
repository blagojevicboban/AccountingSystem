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

        AppSession.TrenutnaFirmaChanged += () => Dispatcher.Invoke(UpdateFirmaInfo);
        UpdateFirmaInfo();
        UpdateKorisnikInfo();

        NavigateToDashboard();
    }

    private void UpdateFirmaInfo()
    {
        TxtFirmaNaziv.Text = AppSession.TrenutnaFirma?.Naziv ?? "—";
        TxtFirmaSifra.Text = AppSession.TrenutnaFirma?.Sifra ?? "—";
    }

    private void UpdateKorisnikInfo()
    {
        TxtKorisnik.Text = $"👤 {AppSession.TrenutniKorisnik?.ImeIPrezime ?? "—"}";
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
        TxtHeaderTitle.Text = "🛒 Trgovina i fakture";
        MainContentHost.Content = new TrgovinaView();
    }

    private void NavIzvestaji_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📄 Finansijski izveštaji i PDF";
        MainContentHost.Content = new IzvestajiView();
    }

    private void NavFirme_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏢 Upravljanje firmama";
    }

    private void NavPomoc_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "❓ Pomoć";
        MainContentHost.Content = new PomocView();
    }

    private void BtnChangelog_Click(object sender, RoutedEventArgs e)
    {
        var dijalog = new ChangelogWindow { Owner = this };
        dijalog.ShowDialog();
    }

    private void BtnUvozDOS_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Migracija DOS podataka izvršena za ARHIBEL - 2026 (KOR01)!\n\nUvezeno:\n• 339 naloga za knjiženje\n• 5.606 stavki knjiženja\n• 2.466 artikala na zalihama\n• 42 konta\n• 105 magacina", "DOS migracija", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
using System.Windows;
using System.Windows.Controls;
using AccountingApp.Views.Dashboard;
using AccountingApp.Views.Izvestaji;
using AccountingApp.Views.Nalozi;

namespace AccountingApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NavigateToDashboard();
    }

    private void NavigateToDashboard()
    {
        TxtHeaderTitle.Text = "📊 Dashboard";
        MainContentHost.Content = new DashboardView();
    }

    private void NavDashboard_Click(object sender, RoutedEventArgs e)
    {
        NavigateToDashboard();
    }

    private void NavNalozi_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📖 General Ledger (Journal Entries)";
        MainContentHost.Content = new NaloziView();
    }

    private void NavKartice_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📋 Account Cards";
    }

    private void NavPartneri_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "👥 Subledger (Partners & Open Items)";
    }

    private void NavMagacin_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📦 Inventory & Warehouses";
    }

    private void NavKalkulacije_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🛒 Trade & Invoices";
    }

    private void NavIzvestaji_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "📄 Financial Reports & PDF Export";
        MainContentHost.Content = new IzvestajiView();
    }

    private void NavFirme_Click(object sender, RoutedEventArgs e)
    {
        TxtHeaderTitle.Text = "🏢 Company Management";
    }

    private void BtnUvozDOS_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("DOS Legacy migration executed for ARHIBEL - 2026 (KOR01)!\n\nImported:\n• 339 Journal Entries\n• 5,606 Line Items\n• 2,466 Stock Materials\n• 42 Accounts\n• 105 Warehouses", "DOS Migration", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
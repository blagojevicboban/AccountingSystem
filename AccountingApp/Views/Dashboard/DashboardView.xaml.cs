using System.Windows.Controls;
using AccountingData;
using Microsoft.EntityFrameworkCore;

namespace AccountingApp.Views.Dashboard;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            string dbPath = AppConfig.DbPath;
            if (!System.IO.File.Exists(dbPath)) return;

            var options = new DbContextOptionsBuilder<AccountingDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using var db = new AccountingDbContext(options);

            int nalogeCount = await db.Nalozi.CountAsync();
            int kontaCount = await db.Konta.CountAsync();
            int matCount = await db.Artikli.CountAsync();
            int partneriCount = await db.Partneri.CountAsync();

            TxtUkupnoNaloga.Text = nalogeCount.ToString("N0");
            TxtUkupnoKonta.Text = kontaCount.ToString("N0");
            TxtUkupnoMaterijala.Text = matCount.ToString("N0");
            TxtUkupnoPartnera.Text = partneriCount.ToString("N0");

            var recentNalozi = await db.Nalozi
                .OrderByDescending(n => n.NalogId)
                .Take(15)
                .ToListAsync();

            DgRecentNalozi.ItemsSource = recentNalozi;
        }
        catch
        {
        }
    }
}

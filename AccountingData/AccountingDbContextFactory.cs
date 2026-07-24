using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccountingData;

/// <summary>
/// Fabrika za design-time kreiranje DbContext-a (potrebna za EF migracije).
/// Ne utiče na runtime ponašanje aplikacije.
/// </summary>
public class AccountingDbContextFactory : IDesignTimeDbContextFactory<AccountingDbContext>
{
    public AccountingDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "accounting_migration_temp.db");

        var optionsBuilder = new DbContextOptionsBuilder<AccountingDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new AccountingDbContext(optionsBuilder.Options);
    }
}

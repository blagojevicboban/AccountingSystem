using Microsoft.EntityFrameworkCore;
using AccountingData.Models;

namespace AccountingData;

public class AccountingDbContext : DbContext
{
    public DbSet<Firma> Firme => Set<Firma>();
    public DbSet<Korisnik> Korisnici => Set<Korisnik>();
    public DbSet<Konto> Konta => Set<Konto>();
    public DbSet<Nalog> Nalozi => Set<Nalog>();
    public DbSet<StavkaNaloga> StavkeNaloga => Set<StavkaNaloga>();
    public DbSet<Partner> Partneri => Set<Partner>();
    public DbSet<Magacin> Magacini => Set<Magacin>();
    public DbSet<Artikal> Artikli => Set<Artikal>();

    public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Firma>()
            .HasIndex(f => f.Sifra)
            .IsUnique();

        modelBuilder.Entity<Korisnik>()
            .HasIndex(k => k.KorisnickoIme)
            .IsUnique();

        modelBuilder.Entity<Konto>()
            .HasIndex(k => k.BrojKonta)
            .IsUnique();

        modelBuilder.Entity<Partner>()
            .HasIndex(p => p.SifraPartnera);

        modelBuilder.Entity<Artikal>()
            .HasIndex(a => a.SifraArtikla);

        modelBuilder.Entity<Nalog>()
            .HasIndex(n => n.BrojNaloga);
    }
}

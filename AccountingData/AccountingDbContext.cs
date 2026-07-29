using System;
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
    public DbSet<MaterijalnaKartica> MaterijalneKartice => Set<MaterijalnaKartica>();
    public DbSet<UlazNalog> UlazNalozi => Set<UlazNalog>();
    public DbSet<UlazStavka> UlazStavke => Set<UlazStavka>();
    public DbSet<TrebovanjeNalog> TrebovanjeNalozi => Set<TrebovanjeNalog>();
    public DbSet<TrebovanjeStavka> TrebovanjeStavke => Set<TrebovanjeStavka>();
    public DbSet<PrimopredajaNalog> PrimopredajaNalozi => Set<PrimopredajaNalog>();
    public DbSet<PrimopredajaStavka> PrimopredajaStavke => Set<PrimopredajaStavka>();
    public DbSet<Kalkulacija> Kalkulacije => Set<Kalkulacija>();
    public DbSet<KalkulacijaStavka> KalkulacijaStavke => Set<KalkulacijaStavka>();
    public DbSet<MaloprodajnaKalkulacija> MaloprodajneKalkulacije => Set<MaloprodajnaKalkulacija>();
    public DbSet<KarticaKonta> KarticeKonta => Set<KarticaKonta>();
    public DbSet<KamatnaStopa> KamatneStope => Set<KamatnaStopa>();
    public DbSet<Promena> Promene => Set<Promena>();
    public DbSet<RacunOtpremnica> RacuniOtpremnice => Set<RacunOtpremnica>();
    public DbSet<RacunOtpremnicaStavka> RacunOtpremnicaStavke => Set<RacunOtpremnicaStavka>();
    public DbSet<NivelacijaCena> NivelacijeCena => Set<NivelacijaCena>();
    public DbSet<NivelacijaStavka> NivelacijaStavke => Set<NivelacijaStavka>();
    public DbSet<PoreskaTarifa> PoreskeTarife => Set<PoreskaTarifa>();

    public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Kreira DbContext nad zadatom SQLite bazom i primenjuje EF Core migracije
    /// (kreira bazu od nule ako ne postoji).
    /// </summary>
    public static AccountingDbContext Create(string dbPath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AccountingDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        var ctx = new AccountingDbContext(optionsBuilder.Options);
        ctx.Database.Migrate();

        try
        {
            ctx.Database.ExecuteSqlRaw("ALTER TABLE PrimopredajaNalozi ADD COLUMN VrstaDokumenta TEXT DEFAULT 'Primopredaja';");
        }
        catch { }

        try
        {
            ctx.Database.ExecuteSqlRaw(@"
                UPDATE Magacini 
                SET NazivMagacina = OdgovornoLice, OdgovornoLice = NULL 
                WHERE (NazivMagacina LIKE 'Magacin %' OR NazivMagacina IS NULL OR NazivMagacina = '') 
                  AND OdgovornoLice IS NOT NULL 
                  AND TRIM(OdgovornoLice) != '';
            ");
        }
        catch { }

        return ctx;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Podrazumevani administratorski nalog (lozinka: admin).
        modelBuilder.Entity<Korisnik>().HasData(new Korisnik
        {
            KorisnikId = 1,
            KorisnickoIme = "admin",
            // Fiksni, osoljeni PBKDF2 heš za "admin" — mora biti konstanta jer
            // EF HasData zahteva determinističku vrednost (ulazi u model snapshot).
            LozinkaHash = "PBKDF2$100000$IxpGjzsTHvV0x7fZq6RdJQ==$6ERduoiJeJ9Iwc5bF56gYD0r3MqcFCWBYyw8XTHQ3u4=",
            ImeIPrezime = "Administrator",
            Uloga = "Administrator",
            IsActive = true
        });

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

        modelBuilder.Entity<MaterijalnaKartica>()
            .HasIndex(k => new { k.SifraMagacina, k.SifraArtikla });

        modelBuilder.Entity<UlazNalog>()
            .HasIndex(u => u.BrojNaloga);

        modelBuilder.Entity<TrebovanjeNalog>()
            .HasIndex(t => t.BrojNaloga);

        modelBuilder.Entity<PrimopredajaNalog>()
            .HasIndex(p => p.BrojNaloga);

        modelBuilder.Entity<Kalkulacija>()
            .HasIndex(k => k.BrojKalkulacije);

        modelBuilder.Entity<MaloprodajnaKalkulacija>()
            .HasIndex(k => k.BrojKalkulacije);

        modelBuilder.Entity<KarticaKonta>()
            .HasIndex(k => k.BrojKonta);

        modelBuilder.Entity<KamatnaStopa>()
            .HasIndex(k => k.DatumOd);

        modelBuilder.Entity<Promena>()
            .HasIndex(p => p.Sifra);

        modelBuilder.Entity<PoreskaTarifa>()
            .HasIndex(t => t.TarifniBroj)
            .IsUnique();
    }

    private const int PasswordSaltSize = 16;
    private const int PasswordHashSize = 32;
    private const int PasswordIterations = 100_000;

    public static string HashPassword(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(PasswordSaltSize);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password, salt, PasswordIterations, System.Security.Cryptography.HashAlgorithmName.SHA256, PasswordHashSize);
        return $"PBKDF2${PasswordIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash) || !storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
            return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, expected.Length);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

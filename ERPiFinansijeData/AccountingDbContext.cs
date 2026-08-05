using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using ERPiFinansijeData.Models;

namespace ERPiFinansijeData;

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
    public DbSet<Materijal> Materijali => Set<Materijal>();
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
    public DbSet<MaloprodajnaKalkulacijaStavka> MaloprodajnaKalkulacijaStavke => Set<MaloprodajnaKalkulacijaStavka>();
    public DbSet<KarticaKonta> KarticeKonta => Set<KarticaKonta>();
    public DbSet<KamatnaStopa> KamatneStope => Set<KamatnaStopa>();
    public DbSet<Promena> Promene => Set<Promena>();
    public DbSet<RacunOtpremnica> RacuniOtpremnice => Set<RacunOtpremnica>();
    public DbSet<RacunOtpremnicaStavka> RacunOtpremnicaStavke => Set<RacunOtpremnicaStavka>();
    public DbSet<NivelacijaCena> NivelacijeCena => Set<NivelacijaCena>();
    public DbSet<NivelacijaStavka> NivelacijaStavke => Set<NivelacijaStavka>();
    public DbSet<PoreskaTarifa> PoreskeTarife => Set<PoreskaTarifa>();
    public DbSet<NalogAudit> NalogAuditi => Set<NalogAudit>();
    public DbSet<KursnaListaStavka> KursneListeStavke => Set<KursnaListaStavka>();
    public DbSet<FiskalniRacunLog> FiskalniRacuniLog => Set<FiskalniRacunLog>();
    public DbSet<DokumentPrilog> DokumentiPrilozi => Set<DokumentPrilog>();
    public DbSet<UvoznaKalkulacija> UvozneKalkulacije => Set<UvoznaKalkulacija>();
    public DbSet<UvoznaStavka> UvozneStavke => Set<UvoznaStavka>();
    public DbSet<ZatvaranjeStavke> ZatvaranjaStavki => Set<ZatvaranjeStavke>();

    public DbSet<PonudaPredracun> PonudePredracuni => Set<PonudaPredracun>();
    public DbSet<PonudaStavka> PonudeStavke => Set<PonudaStavka>();
    public DbSet<NarudzbenicaDobavljacu> NarudzbeniceDobavljacima => Set<NarudzbenicaDobavljacu>();
    public DbSet<NarudzbenicaStavka> NarudzbeniceStavke => Set<NarudzbenicaStavka>();

    public DbSet<Kompenzacija> Kompenzacije => Set<Kompenzacija>();
    public DbSet<KompenzacijaStavka> KompenzacijeStavke => Set<KompenzacijaStavka>();

    public DbSet<PutniNalog> PutniNalozi => Set<PutniNalog>();
    public DbSet<PutniNalogTrosakStavka> PutniNaloziTroskoviStavke => Set<PutniNalogTrosakStavka>();
    public DbSet<NeoporeziviIznosDnevnice> NeoporeziviIznosiDnevnice => Set<NeoporeziviIznosDnevnice>();

    public DbSet<BlagajnickiNalog> BlagajnickiNalozi => Set<BlagajnickiNalog>();

    public DbSet<MestoTroska> MestaTroska => Set<MestoTroska>();

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

        InitializeDatabase(ctx);

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

    /// <summary>
    /// Inicijalizuje bazu: detektuje zatečenu šemu, usklađuje istoriju migracija i primenjuje
    /// nove migracije. Pokriva tri scenarija:
    /// <list type="number">
    ///   <item>Nova baza — <c>Migrate()</c> kreira sve od nule.</item>
    ///   <item>Baza napravljena prethodnom verzijom — tabele postoje, <c>__EFMigrationsHistory</c>
    ///     ne postoji ili je prazna. Sve poznate migracije se žigošu kao primenjene pre
    ///     <c>Migrate()</c>, da ne bi pokušao da kreira tabele koje već postoje.</item>
    ///   <item>Baza sa potpunom istorijom — <c>Migrate()</c> primenjuje samo nove migracije.</item>
    /// </list>
    /// </summary>
    private static void InitializeDatabase(AccountingDbContext ctx)
    {
        if (PostojiZatecenaSemaBezMigracija(ctx))
        {
            OznaciSveMigracijeKaoPrimenjene(ctx);
        }

        ctx.Database.Migrate();
    }

    /// <summary>
    /// Da li baza postoji, sadrži tabele, ali nema nijednu primenjenu migraciju u istoriji.
    /// To se dešava kad je baza napravljena prethodnom verzijom koja je koristila
    /// <c>EnsureCreated()</c>, ili kad je <c>Migrate()</c> kreirao tabele ali se
    /// <c>__EFMigrationsHistory</c> ispraznila ili izbrisala, ili kad je baza
    /// napravljena drugim modulom (ERPi Zarade) čije tabele uključuju <c>Firme</c>.
    /// </summary>
    private static bool PostojiZatecenaSemaBezMigracija(AccountingDbContext ctx)
    {
        var creator = ctx.Database.GetService<IRelationalDatabaseCreator>();
        if (!creator.Exists() || !creator.HasTables())
            return false;

        // Baza postoji i ima tabele. Proverimo da li ima primenjene migracije.
        try
        {
            var primenjene = ctx.Database.GetAppliedMigrations().ToList();
            return primenjene.Count == 0;
        }
        catch
        {
            // __EFMigrationsHistory ne postoji — baza je definitivno zatečena.
            return true;
        }
    }

    /// <summary>
    /// Upisuje SVE poznate migracije u <c>__EFMigrationsHistory</c> BEZ izvršavanja njihovog
    /// sadržaja, čime se zatečena baza usvaja u sistem migracija a da se nijedan podatak ne
    /// dira. Od tog trenutka svaka naredna migracija ide kroz uobičajenu EF proceduru.
    /// </summary>
    private static void OznaciSveMigracijeKaoPrimenjene(AccountingDbContext ctx)
    {
        ctx.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL
            );");

        var verzija = typeof(DbContext).Assembly.GetName().Version?.ToString() ?? "8.0.16";

        foreach (var migracija in ctx.Database.GetMigrations())
        {
            ctx.Database.ExecuteSqlRaw(
                "INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, {1});",
                migracija, verzija);
        }
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

        modelBuilder.Entity<Materijal>()
            .HasIndex(m => m.SifraArtikla);

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

        modelBuilder.Entity<NeoporeziviIznosDnevnice>()
            .HasIndex(n => n.DatumOd);

        modelBuilder.Entity<Promena>()
            .HasIndex(p => p.Sifra);

        modelBuilder.Entity<PoreskaTarifa>()
            .HasIndex(t => t.TarifniBroj)
            .IsUnique();

        modelBuilder.Entity<NalogAudit>()
            .HasIndex(a => a.NalogId);

        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasOne(z => z.StavkaDuguje)
            .WithMany()
            .HasForeignKey(z => z.StavkaDugujeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasOne(z => z.StavkaPotrazuje)
            .WithMany()
            .HasForeignKey(z => z.StavkaPotrazujeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasIndex(z => z.StavkaDugujeId);

        modelBuilder.Entity<ZatvaranjeStavke>()
            .HasIndex(z => z.StavkaPotrazujeId);
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

---
name: sqlite-efcore-schema-migration
description: Workflow rules for EF Core SQLite database migrations, process locking troubleshooting, and unit testing in ERPiFinansije.
---

# SQLite & EF Core Schema Migration Workflow (ERPiFinansije)

This skill documents the database schema management patterns, SQLite database management, and build troubleshooting for `ERPiFinansije`.

---

## 1. Safe SQLite Schema & Model Definition (`AccountingDbContext.cs`)

`ERPiFinansije` uses SQLite with EF Core 8 **migrations** (not `EnsureCreated`).

### Rule: Model Entity Integrity
When adding new properties to model entities (e.g. `Nalog.cs`, `Konto.cs`, `Partner.cs`, `Artikal.cs`):
1. **Model**: Add the property with appropriate C# type in `ERPiFinansijeData/Models`.
2. **DbContext**: Update `AccountingDbContext.cs` sets and indexing.
3. **Migration**: Generate a new migration —
   ```powershell
   dotnet ef migrations add <DescriptiveName> --project ERPiFinansijeData/ERPiFinansijeData.csproj --startup-project ERPiFinansijeData/ERPiFinansijeData.csproj -o Migrations
   ```
4. **Apply**: Any code path that opens a database goes through `AccountingDbContext.Create(dbPath)`,
   which calls `Database.Migrate()` — never call `EnsureCreatedAsync()`, it does not
   understand migrations and will leave the `__EFMigrationsHistory` table missing/out of sync.

### Rule: Seeded Data (`HasData`)
The default `admin` user (password `admin123`) is seeded via `modelBuilder.Entity<Korisnik>().HasData(...)`
in `OnModelCreating`. `HasData` requires a **compile-time deterministic** value — the `LozinkaHash`
is a hardcoded PBKDF2 string (fixed salt), not a call to `HashPassword()`. If you need to change the
seeded password, generate a new hash offline (fixed salt + `Rfc2898DeriveBytes.Pbkdf2`) and hardcode it,
then add a migration for the data change.

### Password Hashing
`AccountingDbContext.HashPassword(password)` / `VerifyPassword(password, hash)` implement salted
PBKDF2-SHA256 (100k iterations), format `PBKDF2$<iterations>$<saltB64>$<hashB64>`. Never store
plaintext passwords.

---

## 2. Process File Locking & Build Failures

During `dotnet build` or `dotnet test`, the `ERPiFinansijeApp.exe` or `ERPiFinansijeData.dll` binaries may be locked by a running instance of `ERPiFinansijeApp` or `netcoredbg`.

### Remediation Command:
```powershell
powershell -Command "Stop-Process -Name ERPiFinansijeApp, netcoredbg -Force -ErrorAction SilentlyContinue"
```
Run this command whenever `dotnet build` fails with `MSB3021` or `MSB3026` file access error.

---

## 3. Unit Testing & Verification Workflow

- **Test Project**: `ERPiFinansijeData.Tests`
- **Execution Command**:
  ```powershell
  dotnet test C:\KNJIGE\ERPiFinansije\ERPiFinansijeData.Tests\ERPiFinansijeData.Tests.csproj
  ```
- **Rule**: All accounting logic (journal totals, balance validation, posting rules) MUST be unit tested using `Microsoft.EntityFrameworkCore.InMemory`.

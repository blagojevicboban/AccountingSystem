---
name: sqlite-efcore-schema-migration
description: Workflow rules for EF Core SQLite database migrations, process locking troubleshooting, and unit testing in AccountingSystem.
---

# SQLite & EF Core Schema Migration Workflow (AccountingSystem)

This skill documents the database schema management patterns, SQLite database management, and build troubleshooting for `AccountingSystem`.

---

## 1. Safe SQLite Schema & Model Definition (`AccountingDbContext.cs`)

`AccountingSystem` uses SQLite with EF Core 8.

### Rule: Model Entity Integrity
When adding new properties to model entities (e.g. `Nalog.cs`, `Konto.cs`, `Partner.cs`, `Artikal.cs`):
1. **Model**: Add the property with appropriate C# type in `AccountingData/Models`.
2. **DbContext**: Update `AccountingDbContext.cs` sets and indexing.
3. **Database Auto-Creation**: `await db.Database.EnsureCreatedAsync()` is used for SQLite instances.

---

## 2. Process File Locking & Build Failures

During `dotnet build` or `dotnet test`, the `AccountingApp.exe` or `AccountingData.dll` binaries may be locked by a running instance of `AccountingApp` or `netcoredbg`.

### Remediation Command:
```powershell
powershell -Command "Stop-Process -Name AccountingApp, netcoredbg -Force -ErrorAction SilentlyContinue"
```
Run this command whenever `dotnet build` fails with `MSB3021` or `MSB3026` file access error.

---

## 3. Unit Testing & Verification Workflow

- **Test Project**: `AccountingData.Tests`
- **Execution Command**:
  ```powershell
  dotnet test C:\KNJIGE\AccountingSystem\AccountingData.Tests\AccountingData.Tests.csproj
  ```
- **Rule**: All accounting logic (journal totals, balance validation, posting rules) MUST be unit tested using `Microsoft.EntityFrameworkCore.InMemory`.

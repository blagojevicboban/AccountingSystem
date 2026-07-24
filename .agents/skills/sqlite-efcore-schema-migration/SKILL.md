---
name: sqlite-efcore-schema-migration
description: Workflow rules for EF Core SQLite database migrations, legacy database column patching, process locking troubleshooting, and unit testing in SredstvaSystem.
---

# SQLite & EF Core Schema Migration Workflow (SredstvaSystem)

This skill documents the database schema management patterns, legacy SQLite database patching, and build troubleshooting for `SredstvaSystem`.

---

## 1. Safe SQLite Schema Patching (`SredstvaDbContext.cs`)

`SredstvaSystem` supports legacy SQLite database files created before EF Core migrations were introduced as well as standard databases with `__EFMigrationsHistory`.

### Rule: Always Update `EnsureExtraColumnsExist`
When adding new properties to model entities (e.g. `Sredstvo.cs`):
1. **Model**: Add the property with appropriate C# type.
2. **Database Context**: In [SredstvaDbContext.cs](file:///c:/SREDSTVA/SredstvaSystem/SredstvaData/SredstvaDbContext.cs), update `EnsureExtraColumnsExist(string dbPath)`.
3. **Execution**: `EnsureExtraColumnsExist` MUST run unconditionally in `SredstvaDbContext.Create(dbPath)` **before** `Database.Migrate()`.
4. **Column Check Pattern**:
   ```csharp
   if (!ColumnExists("Sredstva", "ColumnName"))
   {
       Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"ColumnName\" TEXT NOT NULL DEFAULT '';");
   }
   ```

---

## 2. Process File Locking & Build Failures

During `dotnet build` or `dotnet test`, the `SredstvaApp.exe` or `SredstvaData.dll` binaries may be locked by a running instance of `SredstvaApp` or `netcoredbg`.

### Remediation Command:
```powershell
powershell -Command "Stop-Process -Name SredstvaApp, netcoredbg -Force -ErrorAction SilentlyContinue"
```
Run this command whenever `dotnet build` fails with `MSB3021` or `MSB3026` file access error.

---

## 3. Unit Testing & Verification Workflow

- **Test Project**: `SredstvaData.Tests`
- **Execution Command**:
  ```powershell
  dotnet test C:\SREDSTVA\SredstvaSystem\SredstvaData.Tests\SredstvaData.Tests.csproj
  ```
- **Rule**: All calculations (accounting depreciation, start rules, pre-disposal rules, tax differences) MUST be unit tested without DB/UI dependencies.

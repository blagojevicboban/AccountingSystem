---
name: release-and-versioning
description: How to cut a new release of AccountingApp — bumping version.txt, triggering the GitHub Actions Velopack release workflow, and how the Velopack auto-update client works. Use whenever asked to release, publish, bump the version, tag a build, or troubleshoot auto-update.
---

# Release & Versioning (AccountingApp / AccountingSystem)

Releases are built and published using **Velopack** or CI (`.github/workflows/release.yml`) or via `publish.ps1`. Bumping `version.txt` and pushing to `main` cuts a new release.

---

## 1. Single Source of Truth: `version.txt`

- [version.txt](file:///C:/KNJIGE/AccountingSystem/version.txt) at the repo root holds the plain version string, e.g. `1.0.0` (no `v` prefix, no quotes, no trailing newline content beyond the number).
- `AccountingApp/AccountingApp.csproj` reads it at build time via an MSBuild property (`<Version>$([System.IO.File]::ReadAllText('$(MSBuildProjectDirectory)\..\version.txt').Trim())</Version>`), so the app's displayed/assembly version always matches this file — **never** hardcode a version elsewhere (csproj, XAML, About dialog).
- Git tags mirror this value exactly (`1.0.0`, `1.0.1`, ...).

## 2. Cutting a Release

1. Bump `version.txt` to a new version **strictly greater** than the current one (Velopack delta-updates depend on monotonically increasing versions — never reuse or decrement a version).
2. Run local packaging script `.\publish.ps1` or push to `main` branch to trigger `.github/workflows/release.yml`.
3. The build process executes: `dotnet publish` (self-contained `win-x64`, single-file) → `vpk pack` (packId `AccountingSystem`, mainExe `AccountingApp.exe`, packTitle `AccountingSystem`) → `vpk upload github`.

## 3. Client-Side Auto-Update

- `App.xaml.cs` initializes `VelopackApp.Build().Run();` on app startup.
- The app is installed per-user (no admin rights required); this is why CI must not change the packId (`AccountingSystem`) between releases — Velopack uses it to identify the update channel.

## 4. Gotchas

- If build fails at `vpk pack`/`vpk upload`, check that `version.txt`'s new value is strictly greater than the latest published tag.
- Check that `publish_output` directory is clean before building new releases.

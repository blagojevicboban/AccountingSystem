---
name: release-and-versioning
description: How to cut a new release of ERPiFinansijeApp — bumping version.txt, triggering the GitHub Actions Velopack release workflow, verifying the run and release with the `gh` CLI, and how the Velopack auto-update client works. Use whenever asked to release, publish, bump the version, tag a build, check whether a build passed, or troubleshoot auto-update.
---

# Release & Versioning (ERPiFinansijeApp / ERPiFinansije)

Releases are built and published using **Velopack** or CI (`.github/workflows/release.yml`) or via `publish.ps1`. Bumping `version.txt` and pushing cuts a new release. The workflow triggers on both `main` and `master`; **this repo's default branch is `master`** — pushing there is what actually releases.

---

## 1. Single Source of Truth: `version.txt`

- `version.txt` at the repo root holds the plain version string, e.g. `1.0.0` (no `v` prefix, no quotes, no trailing newline content beyond the number). Write it without a trailing newline — `printf '1.4.0' > version.txt`, not `echo`.
- `ERPiFinansijeApp/ERPiFinansijeApp.csproj` reads it at build time via an MSBuild property (`<Version>$([System.IO.File]::ReadAllText('$(MSBuildProjectDirectory)\..\version.txt').Trim())</Version>`), so the app's displayed/assembly version always matches this file — **never** hardcode a version elsewhere (csproj, XAML, About dialog).
- Git tags mirror this value exactly (`1.0.0`, `1.0.1`, ...).

## 2. Cutting a Release

1. Bump `version.txt` to a new version **strictly greater** than the current one (Velopack delta-updates depend on monotonically increasing versions — never reuse or decrement a version).
2. Run local packaging script `.\publish.ps1` or push to `main` branch to trigger `.github/workflows/release.yml`.
3. The build process executes: `dotnet publish` (self-contained `win-x64`, single-file) → `vpk pack` (packId `ERPiFinansije`, mainExe `ERPiFinansijeApp.exe`, packTitle `ERPiFinansije`) → `vpk upload github`.

## 3. Verifying the Release with `gh` (GitHub CLI)

A push only *starts* the release. Do not report a release as done on the strength of a
successful `git push` — check the run and the published assets.

### PATH quirk in this environment

`gh` is installed at `C:\Program Files\GitHub CLI\gh.exe` and that directory **is** in the
machine PATH, but a shell started before the install still carries the old PATH. Prefix
`gh` calls with a PATH refresh so they work in any session:

```powershell
$env:PATH += ";C:\Program Files\GitHub CLI"; gh run list --limit 3
```

If `gh` is missing entirely: `winget install --id GitHub.cli --source winget`. Add
`--source winget` explicitly — the `msstore` source fails here with a certificate error
(`0x8a15005e`) and aborts the whole command.

### Auth is a one-time human step

`gh auth login` is interactive (device code + browser) and **cannot be driven from an
agent shell** — the tool runs non-interactive with stdin on the null device, so the prompt
reads EOF and hangs or fails. Ask the user to run it in their own terminal; do not try to
automate it, and never ask them to paste a token into the conversation (it would be
recorded in the transcript). Confirm with `gh auth status` before relying on `gh`.

Required scopes: `repo` and `workflow`.

### Commands worth using

```powershell
gh run list --limit 5                  # did the release workflow pass?
gh run view <run-id> --log-failed      # only the failing steps' logs
gh run watch                           # follow the in-progress run
gh release view 1.4.0                  # tag, publish time, and asset list
gh release view 1.4.0 --json assets    # asset list in machine-readable form
```

A healthy release carries five assets: `ERPiFinansije-win-Setup.exe`,
`ERPiFinansije-win-Portable.zip`, `ERPiFinansije-<version>-full.nupkg`, plus Velopack's
own `RELEASES` and `releases.win.json` feed files. **The `.nupkg` and the two feed files
are what matter for existing installs** — Velopack clients read the feed and pull the
package on next start. A release whose run went green but that is missing any of those
three reaches only new installs, not anyone who already has the app.

A full run takes roughly 6 minutes, so a check fired immediately after the push will show
`in_progress`, not a verdict.

### Do not poll `api.github.com`

Unauthenticated GitHub API calls share a 60/hour quota per IP with other tooling on this
machine. Use `gh` (which spends the authenticated quota) rather than raw API fetches, and
check once — or use `gh run watch`, which is a single long-lived call — instead of a
polling loop.

## 4. Client-Side Auto-Update

- `App.xaml.cs` initializes `VelopackApp.Build().Run();` on app startup.
- The app is installed per-user (no admin rights required); this is why CI must not change the packId (`ERPiFinansije`) between releases — Velopack uses it to identify the update channel.

## 5. Gotchas

- If build fails at `vpk pack`/`vpk upload`, check that `version.txt`'s new value is strictly greater than the latest published tag (`gh release list --limit 5` shows what is already out there).
- Check that `publish_output` directory is clean before building new releases.
- **Migrations are not a release concern.** `AccountingDbContext.Create` calls `Database.Migrate()`, which applies every pending migration in order, so a user upgrading across many versions at once is fine — verified by seeding databases at old migrations and opening them with the current build (skipping 24 versions applied all 25 cleanly). This holds only as long as nothing uses `EnsureCreated()`, which would leave a database with no `__EFMigrationsHistory` for `Migrate()` to build on.

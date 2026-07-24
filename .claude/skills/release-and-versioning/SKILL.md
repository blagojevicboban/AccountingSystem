---
name: release-and-versioning
description: How to cut a new release of SredstvaApp — bumping version.txt, triggering the GitHub Actions Velopack release workflow, and how the Velopack auto-update client works. Use whenever asked to release, publish, bump the version, tag a build, or troubleshoot auto-update.
---

# Release & Versioning (SredstvaApp / AssetManager)

Releases are built and published entirely by CI (`.github/workflows/release.yml`) using **Velopack**. There is no local packaging step in normal flow — the only manual action is bumping `version.txt` and pushing to `main`.

---

## 1. Single Source of Truth: `version.txt`

- [version.txt](file:///c:/SREDSTVA/SredstvaSystem/version.txt) at the repo root holds the plain version string, e.g. `1.0.42` (no `v` prefix, no quotes, no trailing newline content beyond the number).
- `SredstvaApp/SredstvaApp.csproj` reads it at build time via an MSBuild property (`<Version>$([System.IO.File]::ReadAllText('...\version.txt').Trim())</Version>`), so the app's displayed/assembly version always matches this file — **never** hardcode a version elsewhere (csproj, XAML, About dialog).
- Git tags mirror this value exactly (`1.0.41`, `1.0.42`, ...) and are created by the release workflow (`vpk upload github --publish --releaseName "$VERSION"`) — don't hand-create release tags.

## 2. Cutting a Release

1. Bump `version.txt` to a new version **strictly greater** than the current one (Velopack delta-updates depend on monotonically increasing versions — never reuse or decrement a version).
2. Commit the bump (repo convention: a small standalone commit, e.g. `ver` or `fix: vX.Y.Z - ...`) and push to `main`.
3. Pushing to `main`/`master` auto-triggers `.github/workflows/release.yml` (`on: push: branches: [main, master]`). It can also be run manually from the Actions tab (`workflow_dispatch`) if you need to re-run a release without a new commit.
4. CI does, in order: `dotnet publish` (self-contained `win-x64`, single-file, ReadyToRun) → copies `SredstvaApp/Resources/Help` into the publish output → installs `vpk` (Velopack CLI) → `vpk pack` (packId `SredstvaSystem`, mainExe `SredstvaApp.exe`, icon `SredstvaApp/app.ico`) → `vpk upload github --publish` to `blagojevicboban/AssetManager` GitHub Releases, tagging the release with the version string.
5. Confirm the release by checking the repo's GitHub Releases page for the new tag and the `SredstvaAppSetup.exe` asset.

## 3. Client-Side Auto-Update

- `MainWindow.xaml.cs` (`CheckForUpdatesAsync`) runs on every app start, using `Velopack.Sources.GithubSource` pointed at `https://github.com/blagojevicboban/AssetManager` (public repo, no token). If the repo is ever renamed/moved, this URL **must** be updated here or auto-update silently breaks (it only logs to `Debug.WriteLine` on failure, no user-facing error).
- If a newer version exists, `UpdateDialog.xaml.cs` prompts the user and applies the Velopack delta update.
- The app is installed per-user (no admin rights required); this is why CI must not change the packId (`SredstvaSystem`) between releases — Velopack uses it to identify the update channel.

## 4. Gotchas

- If CI fails at `vpk pack`/`vpk upload`, check that `version.txt`'s new value is actually greater than the latest published tag — Velopack will refuse/produce a broken delta chain otherwise.
- `SredstvaApp/Resources/Help` is copied manually in the workflow because it may not be picked up by the csproj's own content items — if you add new non-code assets that must ship, verify they either flow through `dotnet publish` output or add an equivalent copy step.
- This workflow has no separate "beta"/"pre-release" channel — every push to `main` with a bumped version is a public production release. Don't bump `version.txt` on a feature branch/PR unless you intend to release immediately on merge.

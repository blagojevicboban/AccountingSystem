---
name: release-and-versioning
description: Automated step-by-step process for bumping app version, updating CHANGELOG.md and README.md with detailed release notes, creating git commit with release summary, tagging, and pushing to cut a new Velopack release. Use whenever asked to release, bump version, publish, tag, or create a new version/release.
---

# Automated Release & Versioning Workflow (ERPiFinansije)

Use this workflow whenever the user requests a new release, version bump, publish, tag update, or "pređi na novu verziju".

---

## 📋 Release Execution Checklist

When triggered (e.g. "pređi na novu verziju", "napravi novu verziju", "bump version", "release v1.0.X"):

### Step 1: Pre-Flight Check & Git Status
1. Check current git status (`git status`). Ensure no unintended `.db` SQLite database files or temporary test binaries are staged.
2. Detect current active git branch dynamically via `git branch --show-current` (e.g. `master` or `main`).
3. Check for EF Core schema migrations in `ERPiFinansijeData/Migrations/` added since the previous version tag (`git diff --name-only <last_tag> HEAD ERPiFinansijeData/Migrations/`). If any exist, flag a **`⚠️ OBAVEŠTENJE O MIGRACIJI BAZE`** section in the release notes.

### Step 2: Determine Next Version Number
1. Read current version from `version.txt` (e.g., `1.0.14`).
2. Increment the patch version (e.g., `1.0.15`).

### Step 3: Gather All Changes Since Last Release
1. Inspect git status, recent commits (`git log <last_tag>..HEAD --oneline`), and chat history for all changes made since the last release tag.
2. Group changes into clear categories:
   - 🚀 **Nove funkcionalnosti** (New Features & Shortcuts)
   - 🎨 **UI / UX i Odzivnost** (UI / UX Enhancements & Aesthetics)
   - ⚡ **Optimizacija i Performanse** (Optimizations & Workflows)
   - 🐛 **Ispravke i Validacije** (Bug Fixes & Validations)
   - ⚠️ **Migracije i Baza Podataka** (Database Schema Updates, if applicable)

### Step 4: Update `CHANGELOG.md` & `README.md`
1. Insert the new release entry at the top of `CHANGELOG.md` right below the main header using today's ISO date (`YYYY-MM-DD`):
   ```markdown
   ## [1.0.15] - YYYY-MM-DD

   ### 🚀 ...
   - ...
   ```
2. Keep all historical release notes intact below.
3. Update version badge or version reference in `README.md` to match the new version, ensuring new features are reflected.

### Step 5: Update `version.txt`
1. Overwrite `version.txt` with the exact version string (e.g. `1.0.15`).

### Step 6: Build & Test Verification
1. Run `dotnet build ERPiFinansijeApp/ERPiFinansijeApp.csproj`.
2. Run `dotnet test ERPiFinansijeData.Tests/ERPiFinansijeData.Tests.csproj`.
3. Confirm 0 build errors and all unit tests pass.

### Step 7: Git Commit, Tag & Remote Push
1. Stage modified files: `git add version.txt CHANGELOG.md README.md ERPiFinansijeApp/ ERPiFinansijeData/ .agents/`.
2. Create commit with title `vX.Y.Z - <Summary>` and detailed multi-line message body listing all features.
3. Create git tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`.
4. Push commit and tag to current active branch: `git push origin <active_branch>` and `git push origin vX.Y.Z`.

### Step 8: (Optional) Local Packaging
1. If the user explicitly asks for a local installer executable or `.nupkg`, execute `.\publish.ps1` to build the local Velopack package in `publish_output`.

---

## 1. Single Source of Truth: `version.txt`
- [version.txt](file:///C:/KNJIGE/ERPiFinansije/version.txt) at repo root holds the plain version string `X.Y.Z`.
- `ERPiFinansijeApp/ERPiFinansijeApp.csproj` reads it automatically via MSBuild property (`<Version>$([System.IO.File]::ReadAllText(...))</Version>`).

## 2. Client-Side Auto-Update & Velopack
- `App.xaml.cs` initializes `VelopackApp.Build().Run();` on app startup.
- Pushing to remote triggers `.github/workflows/release.yml` for automated GitHub Releases and delta-packages.

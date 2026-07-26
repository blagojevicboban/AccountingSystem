---
name: release-and-versioning
description: Automated step-by-step process for bumping app version, updating CHANGELOG.md and README.md with detailed release notes, creating git commit with release summary, tagging, and pushing to cut a new Velopack release. Use whenever asked to release, bump version, publish, tag, or create a new version/release.
---

# Automated Release & Versioning Workflow (AccountingSystem)

Use this workflow whenever the user requests a new release, version bump, publish, tag update, or "pređi na novu verziju".

---

## 📋 Release Execution Checklist

When triggered (e.g. "pređi na novu verziju", "napravi novu verziju", "bump version", "release v1.0.X"):

### Step 1: Determine Next Version Number
1. Read current version from `version.txt` (e.g., `1.0.13`).
2. Increment the patch version (e.g., `1.0.14`).

### Step 2: Gather All Changes Since Last Release
1. Inspect git status, recent commits, and chat history for all changes made since the last release tag.
2. Group changes into clear categories:
   - 🚀 **Nove funkcionalnosti** (New Features & Shortcuts)
   - 🎨 **UI / UX i Odzivnost** (UI / UX Enhancements & Aesthetics)
   - ⚡ **Optimizacija i Performanse** (Optimizations & Workflows)
   - 🐛 **Ispravke i Validacije** (Bug Fixes & Validations)

### Step 3: Update `CHANGELOG.md`
1. Insert the new release entry at the top of `CHANGELOG.md` right below the main header:
   ```markdown
   ## [1.0.14] - YYYY-MM-DD

   ### 🚀 ...
   - ...
   ```
2. Keep all historical release notes intact below.

### Step 4: Update `README.md`
1. Update version badge or version reference in `README.md` to match the new version.
2. Ensure newly introduced features (e.g. F2 account picker, keyboard shortcuts, context menus) are reflected in feature lists.

### Step 5: Update `version.txt`
1. Overwrite `version.txt` with the exact version string (e.g. `1.0.14`).

### Step 6: Build & Test Verification
1. Run `dotnet build AccountingApp/AccountingApp.csproj`.
2. Run `dotnet test AccountingData.Tests/AccountingData.Tests.csproj`.
3. Confirm 0 build errors and all unit tests pass.

### Step 7: Git Commit, Tag & Push
1. Stage modified files: `git add version.txt CHANGELOG.md README.md AccountingApp/ AccountingData/`.
2. Create commit with title `vX.Y.Z - <Summary>` and detailed multi-line message body listing all features.
3. Push commit and optional tag to remote branch: `git push origin main`.

---

## 1. Single Source of Truth: `version.txt`
- [version.txt](file:///C:/KNJIGE/AccountingSystem/version.txt) at repo root holds the plain version string `X.Y.Z`.
- `AccountingApp/AccountingApp.csproj` reads it automatically via MSBuild property (`<Version>$([System.IO.File]::ReadAllText(...))</Version>`).

## 2. Client-Side Auto-Update & Velopack
- `App.xaml.cs` initializes `VelopackApp.Build().Run();` on app startup.
- Pushing to `main` triggers `.github/workflows/release.yml` for automated GitHub Releases and delta-packages.

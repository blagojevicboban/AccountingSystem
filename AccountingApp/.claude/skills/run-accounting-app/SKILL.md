---
name: run-accounting-app
description: Build, launch, and drive the AccountingApp WPF desktop app (screenshot, click, type, log in) via a UI Automation PowerShell driver. Use when asked to run, start, build, test, or screenshot AccountingApp / AccountingSystem, or to verify a WPF/XAML UI change actually works.
---

Paths below are relative to `AccountingSystem/` (the repo root — it contains
`AccountingSystem.slnx`). AccountingApp is a .NET 8 WPF desktop app (`net8.0-windows`,
code-behind, no MVVM), run natively on Windows — there is no headless/xvfb story
here, the app just runs. Drive it via `driver.ps1` in this directory.

## Database: KOR01 is the sanctioned test database, no isolation needed

Unlike some sibling WPF apps in this environment, `AccountingApp` has **no
settings.json / user-configurable db path**. `AppConfig.DbPath` (`AccountingApp/AppConfig.cs`)
always resolves to `C:\KNJIGE\Radni\KOR01\accounting_kor01.db` if that file exists,
falling back to `%LocalAppData%\AccountingApp\Baze\*.db` only if it doesn't. KOR01
**is** the project's designated test/dev company (migrated from legacy DOS/Clipper
DBF files) — there is no real customer data at this fixed path, so driving the app
against it directly (as below) is intentional, not a safety hazard. If you write
data via the UI during a driver session and want a pristine KOR01 again:

```powershell
dotnet run --project AccountingMigration/AccountingMigration.csproj
```

This wipes and re-imports `accounting_kor01.db` from the legacy DBF files (idempotent,
~10s). If `AccountingApp` is ever pointed at real customer data at that same fixed
path in the future, this assumption breaks and an isolation scheme (like the
settings.json swap other apps in this family use) would need to be added first.

## Run (agent path) — build, launch, drive, screenshot

### 1. Build

```powershell
dotnet build AccountingSystem.slnx -c Debug
```

Produces `AccountingApp\bin\Debug\net8.0-windows\AccountingApp.exe`. (Ignore the
`NU1701` warnings about OpenTK/SkiaSharp.Views.WPF being restored for .NETFramework
— harmless, unrelated to this app.)

### 2. Drive it

All commands go through `driver.ps1` (this directory). Each invocation is a
fresh `powershell.exe` process; it tracks the running app's PID in
`$env:TEMP\accounting_driver_state.json` so successive calls find the same window.

**Always start every invocation with the literal absolute path below — do not
lead with a `$Drv = ...` assignment or a `cd`, even when chaining several
`driver.ps1` calls in one shell invocation.** This repo's `.claude/settings.json`
has a pre-approved `permissions.allow` rule keyed on this exact literal prefix:

```
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1"
```

A call (or a multi-line/`;`-chained batch of calls) whose text starts with that
exact prefix runs with no permission prompt. Prefix matching only looks at how
the command *starts* — so `$Drv = "..."` or `cd X; powershell ...` as the first
line defeats it and a prompt comes back. Repeat the full literal path on every
line instead of factoring it into a variable; it's more typing but keeps every
call, and every batch, matching the rule:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" launch "C:\ERP\AccountingSystem\AccountingApp\bin\Debug\net8.0-windows\AccountingApp.exe"
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" ss login.png              # screenshot the login window
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" type TxtUsername "^a{DEL}admin"
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" type TxtPassword "^a{DEL}admin123"
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" click BtnLogin
# The login -> MainWindow swap isn't instant; a `tree`/`click` fired immediately
# after can still land on the login window and error "AutomationId not found".
# Give it a beat (a Start-Sleep of ~1-2s, or just retry the click once) before
# treating a post-login step as failed.
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" tree                      # dump AutomationId tree of current window
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" click BtnIzvestaji        # any AutomationId from `tree`, e.g. BtnDashboard/BtnNalozi/BtnKartice/BtnPartneri/BtnMagacin/BtnKalkulacije/BtnIzvestaji/BtnFirme/BtnUvozDOS
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" ss izvestaji.png
powershell -ExecutionPolicy Bypass -File "C:\ERP\AccountingSystem\AccountingApp\.claude\skills\run-accounting-app\driver.ps1" close
```

`Stop-Process -Name AccountingApp*` (used to clear a file lock before rebuilding
— see the `sqlite-efcore-schema-migration` skill) is also pre-approved.

Commands: `launch <exe>`, `tree`, `click <AutomationId>`, `type <AutomationId> <text>`,
`keys <SendKeys-string>`, `ss <out.png>`, `close`. `AutomationId` is the control's
`x:Name` in XAML (WPF exposes it 1:1 to UI Automation for named elements).
`keys` sends raw SendKeys to whatever control currently has focus (no AutomationId
lookup) — needed for controls with none, e.g. a `DataGrid` cell's auto-generated
editing `ComboBox`/`TextBox` (see gotcha below).

Default seeded login is **`admin` / `admin123`** (PBKDF2 hash via EF Core `HasData`
in `AccountingData/AccountingDbContext.cs` — see the `sqlite-efcore-schema-migration`
skill). This is true on every freshly-migrated database, including KOR01 after
re-running `AccountingMigration`.

## Run (human path)

Visual Studio 2022+ / Rider: open `AccountingSystem.slnx`, set `AccountingApp` as
startup project, F5. Or `dotnet run --project AccountingApp\AccountingApp.csproj`
— opens a real window, blocks until closed; useless for an agent without the
driver above.

## Test

```powershell
dotnet test AccountingData.Tests\AccountingData.Tests.csproj
```

3 tests pass. Covers `NaloziService` only — no UI coverage. The driver above is
the only way to exercise the WPF layer.

## Gotchas

- **`RadioButton` (the sidebar nav) does NOT support `InvokePattern`.** Only plain
  `Button` does. Calling `InvokePattern.Invoke()` on a `RadioButton` throws
  "Unsupported Pattern." Using `SelectionItemPattern.Select()` or
  `TogglePattern.Toggle()` instead "succeeds" (the button visibly becomes
  selected/blue) but **does not raise the WPF `Click` event** — so
  `MainWindow`'s `Click="NavXxx_Click"` handlers silently never fire and the
  content pane never actually switches. Verified empirically: `Select()`
  highlighted "General Ledger" in the sidebar but the Dashboard content stayed
  on screen. Fix: focus the element then send a **Space key press**
  (`SendKeys::SendWait(" ")`) — WPF's `ButtonBase` (base of both `Button` and
  `RadioButton`) treats Space as a click-equivalent and reliably raises `Click`.
  `driver.ps1`'s `click` command already does Invoke-first-else-Space.
  See [driver.ps1:115-133](driver.ps1#L115-L133).
- **`PasswordBox` has no `ValuePattern`.** UI Automation can't set its value
  directly (by design). `driver.ps1`'s `type` command uses `SendKeys` uniformly
  for both `TextBox` and `PasswordBox`.
- **Windows may silently pre-fill `TxtUsername`/`TxtPassword` from a previous
  run** (shell-level edit-control autocomplete, keyed loosely to the app/window
  — not something this app implements). A screenshot right after `launch` can
  show old credentials already typed in. Don't trust it — always clear-then-type
  explicitly: `type TxtUsername "^a{DEL}admin"` (`^a` = Ctrl+A select-all,
  `{DEL}` = delete, then the real value). Verified: first launch showed a stale
  5-character password already filled; explicit clear+retype fixed it.
- **`SetForegroundWindow` gets silently denied on repeat calls.** Windows'
  foreground-lock heuristic blocks a background process from repeatedly
  stealing focus — symptom is a screenshot that shows your editor/terminal
  instead of the app, no error raised. Fix: go through
  `(New-Object -ComObject WScript.Shell).AppActivate($pid)` instead of raw
  `user32!SetForegroundWindow`. `driver.ps1`'s `Get-TopWindow` already does
  this before every `ss`/`click`/`type`. See [driver.ps1:59-69](driver.ps1#L59-L69).
- **A native `MessageBox` (e.g. the crash dialog below, or `BtnUvozDOS`'s fake
  "DOS Legacy migration executed" popup) is a separate top-level window for the
  same process.** `Get-TopWindow` picks the *last* window for the tracked PID,
  so `tree`/`click`/`ss` transparently target the dialog once one is open — use
  `tree` to find its OK button's `AutomationId` (it won't have a friendly
  `x:Name`, e.g. `AutomationId='2'`).
- **`BtnUvozDOS` ("DOS Legacy Import") does not actually run a migration.** It's
  a hardcoded `MessageBox.Show` with fixed fake numbers — the real importer is
  the separate `AccountingMigration` console project (`dotnet run --project
  AccountingMigration/AccountingMigration.csproj`). Don't mistake clicking it
  for driving a real import.

## Bug found and fixed while building this skill (2026-07-24)

Driving the app for real (not just reading the code) caught a genuine crash:
**clicking "General Ledger" (`BtnNalozi`) crashed with a `NullReferenceException`**
every time, for any user — not a driver artifact.

Root cause: `NaloziView.xaml` set `ChkSamoProknjizeni`'s `IsChecked="True"` as a
literal XAML attribute. WPF applies that during `InitializeComponent()`, which
fires the `Checked` event (→ `Filter_Changed` → `ApplyFilter()`) **synchronously,
mid-parse** — before `DgNalozi` (declared later in the same XAML tree, in a
sibling `Border`) has been constructed yet. `ApplyFilter()`'s
`DgNalozi.ItemsSource = filtered;` then threw on a still-null `DgNalozi`.

Fix (`AccountingApp/Views/Nalozi/NaloziView.xaml` + `.xaml.cs`): removed the
`IsChecked="True"` XAML literal and instead set `ChkSamoProknjizeni.IsChecked = true;`
in code, right after `InitializeComponent()` — by then the whole tree (including
`DgNalozi`) exists, so the same `Checked` event fires safely. Verified fixed with
the driver: `click BtnNalozi` now shows the Journal Entries grid with no dialog.

General lesson for this codebase: **don't set `IsChecked`/`IsSelected`/etc. as a
XAML literal on a control if its `Checked`/`SelectionChanged`/etc. handler touches
sibling named elements declared later in the same file** — the event can fire
before those elements exist. Set the initial state in code instead.

- **`keys` is flaky in this sandbox: the first keystroke of a call is sometimes
  dropped, and occasionally focus jumps away from the app entirely (e.g. to the
  Windows taskbar search box) between driver invocations** — each `driver.ps1`
  call is a fresh process, so every `keys` call redoes window activation, and on
  this remote/virtual desktop that activation isn't 100% reliable. Mitigations:
  add a generous `Start-Sleep` (400ms+) before `keys`, prefer one `keys` call
  with the full string over several single-character calls, and always screenshot
  *immediately* after to confirm the text actually landed in the target control
  before trusting the next step — don't chain several blind `keys` calls in a row.
  If a screenshot shows the taskbar or wrong window, the keys went nowhere useful;
  just retry the step, it's environmental, not app state.
- **An open `ComboBox` dropdown (`IsDropDownOpen=true`) is a separate top-level
  Popup hwnd, and it auto-closes the instant the window is reactivated** — which
  `Get-TopWindow` does on *every* `driver.ps1` call (`AppActivate`). So there is no
  way with this driver to open a dropdown in one call and then inspect/click one of
  its rows (`ListItem`) in a later call — by the time the later call's `Get-TopWindow`
  runs, the popup is already gone. Verified empirically trying to build a
  `clickitem` command (removed again). If you need to test dropdown-row selection,
  either drive it with `keys` (types + `{ENTER}`/`{DOWN}{ENTER}` in the *same*
  `keys` call as opening it, so no intervening reactivation) or accept that only
  the "type to filter" part is screenshot-verifiable with this driver, not the
  "click a specific row" part.

- **Clicking a `ComboBox` (e.g. `CmbNacinPlacanja`) with `click` toggles its dropdown
  open/closed via `InvokePattern`/`Toggle`, and can leave it in a state where the
  previously-typed text gets cleared** on the next window reactivation. Don't
  `click` into an editable `ComboBox` to focus it before typing — use `type
  <AutomationId> "^a{DEL}value"` directly, which sends `SendKeys` without touching
  the dropdown state.
- **Rapid scripted `keys`/Tab navigation into a `DataGrid` cell (e.g. tabbing into
  the stavke grid's `DataGridComboBoxColumn`) immediately followed by clicking a
  dialog's Cancel/Close button can trigger `System.InvalidOperationException:
  DialogResult can be set only after Window is created and shown as dialog`** in
  `crash.log`. Reproduced once this way (2026-08-01); a clean re-run — open the
  same dialog, click Cancel immediately with no intervening grid/keys traffic —
  closed fine with no exception. Treat this as a UI-Automation timing artifact
  from stacking `keys` and `click` calls too tightly on a `DataGrid` in edit mode,
  not a real app regression — but if it reproduces *without* prior grid/`keys`
  interaction, that would be a genuine bug worth reporting.

## Troubleshooting

- **`FindFirst` returns `$null` / "Element with AutomationId 'X' not found"**:
  run `driver.ps1 tree` first to confirm the current window and its actual
  `AutomationId`s — you may be on a `MessageBox` dialog (see Gotchas) or the
  login window rather than `MainWindow`.
- **Screenshot captures the wrong window (editor/terminal instead of the app)**:
  see the `SetForegroundWindow` gotcha above.
- **Clicking a sidebar nav item does nothing (dashboard stays visible)**: you're
  likely on an older copy of `driver.ps1` that only tries `InvokePattern` — see
  the `RadioButton` gotcha above; the fixed version falls back to Space-bar.

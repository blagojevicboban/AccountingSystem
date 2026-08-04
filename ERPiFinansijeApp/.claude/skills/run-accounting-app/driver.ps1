<#
.SYNOPSIS
  Drives the ERPiFinansijeApp WPF desktop app via UI Automation: launch, screenshot,
  click, type, close. Built for agents that cannot see a GUI directly.

.DESCRIPTION
  Commands (positional $Command):
    launch <exe> [nologin]  Start the exe, wait for the first window, and log in as admin/admin123.
                            Pass "nologin" as the second argument to stop at the login screen.
    tree                Dump the UI Automation tree of the current top window (name/AutomationId/ControlType).
    click <AutomationId> Click a button/control by its AutomationId (x:Name in XAML).
    type <AutomationId> <text>  Focus a TextBox/PasswordBox by AutomationId and type text.
    ss   <path.png>     Screenshot the current top window to a PNG file.
    close               Close the tracked process (graceful Close(), then kill if needed).

  State (the tracked process id) lives in $env:TEMP\accounting_driver_state.json so each
  invocation of this script (a fresh PowerShell process) can find the same app instance.

.EXAMPLE
  powershell -File driver.ps1 launch "C:\path\ERPiFinansijeApp.exe"   # already logged in
  powershell -File driver.ps1 tree
  powershell -File driver.ps1 click BtnNalozi
  powershell -File driver.ps1 ss shot1.png
  powershell -File driver.ps1 close

  powershell -File driver.ps1 launch "C:\path\ERPiFinansijeApp.exe" nologin   # stay on login screen
#>
param(
    [Parameter(Position=0, Mandatory=$true)][string]$Command,
    [Parameter(Position=1)][string]$Arg1,
    [Parameter(Position=2)][string]$Arg2
)

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Native -Name Win32 -MemberDefinition @"
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
"@

$StateFile = Join-Path $env:TEMP "accounting_driver_state.json"

function Get-TrackedProcessId {
    if (-not (Test-Path $StateFile)) { throw "No tracked process. Run 'launch' first." }
    (Get-Content $StateFile | ConvertFrom-Json).ProcessId
}

function Get-TopWindow {
    $procId = Get-TrackedProcessId
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $procId)
    # A process may have more than one top-level window (dialogs) — take the last (most recent/topmost).
    $wins = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)
    if ($wins.Count -eq 0) { throw "No window found for tracked process $procId (has it exited?)." }
    $win = $wins[$wins.Count - 1]
    # Bring to foreground — CopyFromScreen captures whatever is actually on top,
    # and the IDE/terminal that launched this script otherwise obscures the app.
    # Raw SetForegroundWindow gets silently denied by Windows' foreground-lock
    # heuristic on repeated calls from a background process, so go through the
    # WScript.Shell AppActivate COM object instead — it is exempt from that lock.
    $hwnd = [IntPtr]$win.Current.NativeWindowHandle
    [Native.Win32]::ShowWindow($hwnd, 9) | Out-Null   # SW_RESTORE
    $shell = New-Object -ComObject WScript.Shell
    $shell.AppActivate((Get-TrackedProcessId)) | Out-Null
    Start-Sleep -Milliseconds 200
    return $win
}

function Find-ById([string]$id) {
    $win = Get-TopWindow
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $el = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($null -eq $el) { throw "Element with AutomationId '$id' not found in current window." }
    return $el
}

switch ($Command) {

    "launch" {
        if (-not $Arg1) { throw "Usage: driver.ps1 launch <path-to-exe> [nologin]" }
        # --autologin (Debug-only switch in App.xaml.cs) opens MainWindow directly as the
        # first active administrator. Nothing is typed: sending credentials with SendKeys
        # was unreliable here (dropped keystrokes, focus wandering off), and it could not
        # work anyway -- LoginWindow refuses to let the seeded "admin123" password through
        # without changing it first, so the main window would never appear.
        $argumenti = if ($Arg2 -eq "nologin") { @() } else { @("--autologin") }
        $proc = if ($argumenti.Count -gt 0) {
                    Start-Process -FilePath $Arg1 -ArgumentList $argumenti -PassThru
                } else {
                    Start-Process -FilePath $Arg1 -PassThru
                }
        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)

        # Without --autologin any window will do (that is the login screen). With it, wait
        # for the window that actually carries the sidebar (BtnDashboard), so a following
        # 'click' cannot land on a half-built window. WPF startup takes a couple of seconds.
        $dashCond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "BtnDashboard")
        $deadline = (Get-Date).AddSeconds(30)
        $win = $null
        while ((Get-Date) -lt $deadline) {
            foreach ($w in $root.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
                if ($argumenti.Count -eq 0 -or
                    $null -ne $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $dashCond)) {
                    $win = $w
                    break
                }
            }
            if ($null -ne $win) { break }
            Start-Sleep -Milliseconds 300
        }
        if ($null -eq $win) { throw "Timed out waiting for a window from pid $($proc.Id)." }
        @{ ProcessId = $proc.Id } | ConvertTo-Json | Set-Content -Path $StateFile
        "Launched pid=$($proc.Id), window='$($win.Current.Name)'"
    }

    "tree" {
        $win = Get-TopWindow
        function Dump-Tree($el, $depth) {
            $c = $el.Current
            Write-Output ("  " * $depth + "[$($c.ControlType.ProgrammaticName)] Name='$($c.Name)' AutomationId='$($c.AutomationId)'")
            if ($depth -ge 8) { return }
            foreach ($child in $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)) {
                Dump-Tree $child ($depth + 1)
            }
        }
        Dump-Tree $win 0
    }

    "click" {
        if (-not $Arg1) { throw "Usage: driver.ps1 click <AutomationId>" }
        $el = Find-ById $Arg1
        $el.SetFocus()
        Start-Sleep -Milliseconds 100
        # Plain Button supports InvokePattern and Invoke() fires its Click handler.
        # WPF RadioButton (the sidebar nav) does NOT support InvokePattern, and its
        # UIA SelectionItemPattern.Select()/TogglePattern.Toggle() only flip IsChecked
        # WITHOUT raising the Click event — so MainWindow's Click="NavXxx_Click"
        # handlers silently never fire and the content pane never switches (verified:
        # the RadioButton visibly became selected/blue but Dashboard stayed on screen).
        # Space-bar is WPF's built-in keyboard-activation key for ButtonBase (Button,
        # RadioButton, CheckBox all derive from it) and DOES raise Click, so it's used
        # uniformly as the fallback for anything without InvokePattern.
        $patternObj = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$patternObj)) {
            $patternObj.Invoke()
        } else {
            [System.Windows.Forms.SendKeys]::SendWait(" ")
        }
        "Clicked '$Arg1'"
    }

    "type" {
        if (-not $Arg1) { throw "Usage: driver.ps1 type <AutomationId> <text>" }
        $el = Find-ById $Arg1
        $el.SetFocus()
        Start-Sleep -Milliseconds 150
        # PasswordBox does not expose ValuePattern (by design) so SendKeys is used for
        # both TextBox and PasswordBox — it works uniformly on whatever has focus.
        [System.Windows.Forms.SendKeys]::SendWait($Arg2)
        "Typed into '$Arg1'"
    }

    "keys" {
        if (-not $Arg1) { throw "Usage: driver.ps1 keys <SendKeys-string>" }
        # Unlike 'type', does not look up an AutomationId first — sends raw SendKeys
        # to whatever control currently has focus. Needed for controls with no
        # AutomationId, e.g. a DataGrid cell's auto-generated editing ComboBox.
        Get-TopWindow | Out-Null
        # Each driver.ps1 call is a fresh process; the terminal regains OS
        # foreground activation between calls, which can leave WPF's *logical*
        # focus target un-reasserted at the OS level even though it never
        # changed inside the app. Re-assert focus on whatever UIA reports as
        # focused before sending keys, so keystrokes land on the actual control.
        $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
        if ($null -ne $focused) { $focused.SetFocus() }
        Start-Sleep -Milliseconds 400
        [System.Windows.Forms.SendKeys]::SendWait($Arg1)
        "Sent keys '$Arg1'"
    }

    "clicktype" {
        if (-not $Arg1 -or -not $Arg2) { throw "Usage: driver.ps1 clicktype <AutomationId> <SendKeys-string>" }
        # Combines 'click' + 'keys' in a single process/activation cycle. Needed when
        # the click triggers an async UI update (e.g. a DataGrid row opening a cell
        # into edit mode via Dispatcher.BeginInvoke) that must be typed into before
        # anything reactivates the window again — a second driver.ps1 call's own
        # Get-TopWindow/AppActivate would otherwise close an open ComboBox dropdown
        # (see SKILL.md gotcha) before the keys ever got sent.
        $el = Find-ById $Arg1
        $el.SetFocus()
        Start-Sleep -Milliseconds 100
        $patternObj = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$patternObj)) {
            $patternObj.Invoke()
        } else {
            [System.Windows.Forms.SendKeys]::SendWait(" ")
        }
        Start-Sleep -Milliseconds 900
        $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
        if ($null -ne $focused) { $focused.SetFocus() }
        Start-Sleep -Milliseconds 200
        [System.Windows.Forms.SendKeys]::SendWait($Arg2)
        "Clicked '$Arg1' then sent keys '$Arg2'"
    }

    "ss" {
        if (-not $Arg1) { throw "Usage: driver.ps1 ss <output.png>" }
        $win = Get-TopWindow
        $r = $win.Current.BoundingRectangle
        $bmp = New-Object System.Drawing.Bitmap([int]$r.Width, [int]$r.Height)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
        $bmp.Save($Arg1, [System.Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $bmp.Dispose()
        "Saved screenshot to $Arg1"
    }

    "close" {
        $procId = Get-TrackedProcessId
        try {
            $p = Get-Process -Id $procId -ErrorAction Stop
            $p.CloseMainWindow() | Out-Null
            if (-not $p.WaitForExit(5000)) { Stop-Process -Id $procId -Force }
        } catch {
            # Already exited.
        }
        Remove-Item $StateFile -ErrorAction SilentlyContinue
        "Closed."
    }

    default { throw "Unknown command '$Command'. Use launch|tree|click|type|ss|close." }
}

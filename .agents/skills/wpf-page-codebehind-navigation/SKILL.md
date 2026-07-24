---
name: wpf-page-codebehind-navigation
description: Conventions for adding a new WPF Page/Window in SredstvaApp — constructor-injected SredstvaDbContext, MainWindow navigation wiring, Loaded-based data binding, search/filter, and dialog refresh patterns. Use whenever adding or modifying a Views/*Page.xaml(.cs) or *Window.xaml(.cs).
---

# WPF Page/Window Code-Behind Pattern (SredstvaApp)

`SredstvaApp` does **not** use a ViewModel/binding-command MVVM layer (only `MainWindowViewModel.cs` exists). Pages and Windows talk to the database directly from code-behind. Follow this pattern for consistency — do not introduce a parallel MVVM pattern for a single new page.

---

## 1. Page Structure

- Each feature lives in `SredstvaApp/Views/<Feature>/<Feature>Page.xaml(.cs)` (list/grid pages) or `<Feature>Window.xaml(.cs)` (modal dialogs, e.g. `RashodWindow`, `PrijavaWindow`, `LoginWindow`).
- Constructor takes `SredstvaDbContext db` (and optional extra params like a pre-selected id, e.g. `KarticePage(SredstvaDbContext db, int sredstvoId)`) and stores it in a `private readonly SredstvaDbContext _db` field.
- Call `InitializeComponent()` first, then assign fields, then wire `Loaded += Page_Loaded`.
- Load data in the `Loaded` handler (not the constructor) — query via `_db`, materialize with `.ToList()`, assign to a backing `List<T> _all` field, and set `SomeGrid.ItemsSource = _all`.

## 2. Search / Filter

- A `TextBox` named `SearchBox` with `TextChanged` handler that lowercases the query, filters the in-memory `_all` list with `Contains(..., StringComparison.OrdinalIgnoreCase)`, and re-assigns `ItemsSource` to the filtered list (never re-query the DB on every keystroke).
- Recompute any footer totals (`UpdateTotals(IEnumerable<T> items)`) after every filter/reload so summary rows stay in sync with what's visible.

## 3. Navigation (MainWindow)

- New top-level pages are registered in `MainWindow.xaml.cs` as `NavigateTo(BtnX, () => new Views.X.XPage(_db))` — add the nav button in `MainWindow.xaml` and a one-line handler following the existing `BtnX_Click` pattern.
- Modal windows/dialogs are opened from within a page via `new Views.Feature.FeatureWindow(_db).ShowDialog()`, then the calling page **re-invokes its own `_Loaded` handler** to refresh (`SredstvaPage_Loaded(this, new RoutedEventArgs())`) rather than manually patching the in-memory list.
- To open a different page from within another page/window, resolve the shell via `Window.GetWindow(this) is MainWindow mainWindow` and call a public `mainWindow.OpenX(...)` method — don't `new` up `MainWindow` directly.

## 4. Selection & Bulk Actions

- Row selection checkboxes bind to a model's `IsSelected` bool; "select all" toggles every item in the current `ItemsSource` and calls `Grid.Items.Refresh()`.
- Bulk action buttons (e.g. `BtnNalepnice_Click`) validate `selected.Count == 0` first and show an informational `MessageBox` if nothing is selected, before proceeding.

## 5. Errors & User Feedback

- Wrap risky operations (PDF generation, file I/O, DB writes triggered from UI) in `try/catch` and show `MessageBox.Show($"Greška ...: {ex.Message}", "...", MessageBoxButton.OK, MessageBoxImage.Error)`. Informational/validation messages use `MessageBoxImage.Information`.
- Do not let exceptions from a button click propagate — always catch at the handler boundary.

## 6. Related

- For generating a print/export document from a page (the common companion to a list page), see [[questpdf-report-documents]].
- For adding a new persisted column referenced by a page/grid, see the `sqlite-efcore-schema-migration` skill.

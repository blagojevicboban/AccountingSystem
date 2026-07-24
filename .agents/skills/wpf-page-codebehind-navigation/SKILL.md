---
name: wpf-page-codebehind-navigation
description: Conventions for adding a new WPF View/Control/Window in AccountingApp — MainWindow navigation host, Loaded-based data binding, search/filter, and dialog patterns. Use whenever adding or modifying Views/*View.xaml(.cs).
---

# WPF Navigation & Control Pattern (AccountingApp)

`AccountingApp` uses clean WPF views with code-behind and EF Core DbContext services. Follow this pattern for consistency across views.

---

## 1. View Structure

- Each feature lives in `AccountingApp/Views/<Feature>/<Feature>View.xaml(.cs)` (e.g., `DashboardView`, `NaloziView`, `IzvestajiView`).
- Constructor calls `InitializeComponent()` first and then initiates async data loading.
- Queries are executed using `AccountingDbContext` and `AppConfig.DbPath`.

## 2. Search / Filter

- `TextBox` with `TextChanged` handler that lowercases the search term, filters the in-memory list with `Contains(..., StringComparison.OrdinalIgnoreCase)`, and re-assigns `ItemsSource` to the filtered list.
- Recompute summary totals after filtering.

## 3. Navigation (MainWindow)

- Views are displayed inside `MainContentHost.Content` in `MainWindow.xaml.cs`:
  ```csharp
  MainContentHost.Content = new NaloziView();
  ```

## 4. Errors & User Feedback

- Wrap risky operations (PDF generation, file I/O, DB writes) in `try/catch` and show `MessageBox.Show($"Greška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error)`.

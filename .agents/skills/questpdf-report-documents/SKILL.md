---
name: questpdf-report-documents
description: Conventions for QuestPDF report/print documents in SredstvaApp (Nalepnice, Rashod, Prijava, Amortizacija/Obrazac OA, Popis, Revalorizacija, Analitička kartica) — IDocument structure, header/footer/table styling, and the generate-and-open flow. Use whenever adding or editing a *Document.cs file under SredstvaApp/Views/**/Stampe or generating a new PDF report.
---

# QuestPDF Document Conventions (SredstvaApp)

Every printable report in `SredstvaApp` is a small `IDocument` class colocated with its feature (e.g. `Views/Rashod/Stampe/RashodDocument.cs`, `Views/Amortizacija/ObrazacOADocument.cs`, `Views/Sredstva/NalepniceDocument.cs`, `Views/Popis/PopisIzvestajDocument.cs`). Follow the existing shape rather than inventing a new document scaffold.

---

## 1. Class Shape

- Name: `<Feature>Document.cs`, implements `QuestPDF.Infrastructure.IDocument`.
- Constructor takes plain data (a `List<...Info>` DTO built by the calling page, plus optional `SredstvaData.Models.Firma? firma`) — never an `SredstvaDbContext`. Query the DB in the page/window, hand the document only the data it needs to render.
- If the document needs per-row DTOs, define small `public class XInfo { ... }` records/classes in the same file above the document class (see `RashodStavkaInfo`/`RashodNalogInfo` in `RashodDocument.cs`).

## 2. `Compose(IDocumentContainer container)`

```csharp
container.Page(page =>
{
    page.Size(PageSizes.A4.Portrait()); // or .Landscape() for wide tables (e.g. Obrazac OA)
    page.Margin(1, Unit.Centimetre);
    page.PageColor(Colors.White);
    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

    page.Header().Element(ComposeHeader);
    page.Content().Element(ComposeContent);
    page.Footer().Element(ComposeFooter);
});
```

- Split into `ComposeHeader`/`ComposeContent`/`ComposeFooter` private methods — keep `Compose` itself just the page/skeleton wiring.

## 3. Header

- Left column: report title (`FontSize(16).SemiBold().FontColor(_primaryColor)`) + print-date subtitle (`Datum štampe: {DateTime.Now:dd.MM.yyyy}`, `FontSize(10)`, `Colors.Grey.Medium`).
- Right column (`row.ConstantItem(250).AlignRight()`): company block from `Firma` — Naziv (bold, black), Mesto, `PIB: {firma.PIB}` — each guarded with `if (!string.IsNullOrEmpty(...))`.
- Use a per-document `private readonly string _primaryColor = "#2B4B80";` (or feature-appropriate QuestPDF color family, e.g. `Colors.Indigo.*`) for the accent color instead of hardcoding hex in multiple places.

## 4. Tables

- `table.ColumnsDefinition` with `ConstantColumn(px)` for fixed fields (codes, dates, amounts) and exactly one `RelativeColumn()` for the free-text/name column.
- Header row: local `static IContainer HeaderStyle(IContainer c)` — dark background (`Colors.Indigo.Darken4` or family matching accent), white semibold text, `FontSize(8)`, `PaddingVertical(4).PaddingHorizontal(4)`.
- Data rows: local `static IContainer RowStyle(IContainer c)` — `BorderBottom(0.5f)` in `Colors.Grey.Lighten2`, `FontSize(7.5f)`, same padding. Right-align numeric cells with `.AlignRight()`.
- Money values format as `value.ToString("N2")`; dates as `date.ToString("dd.MM.yyyy.")` (Serbian trailing dot).
- After a table representing a group/document, add a right-aligned bold "Ukupno: {sum:N2}" summary line in the accent color.

## 5. Footer

```csharp
container.AlignCenter().Text(x =>
{
    x.Span("Strana ").FontSize(7).FontColor(Colors.Grey.Darken1);
    x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Darken1);
    x.Span(" od ").FontSize(7).FontColor(Colors.Grey.Darken1);
    x.TotalPages().FontSize(7).FontColor(Colors.Grey.Darken1);
});
```

## 6. Generating & Opening the PDF (caller side, in the Page/Window)

```csharp
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var doc = new XDocument(data, firma);
var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"X_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
QuestPDF.Fluent.GenerateExtensions.GeneratePdf(doc, filePath);

var p = new System.Diagnostics.Process();
p.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
p.Start();
```

- `QuestPDF.Settings.License` is also set once globally in `App.xaml.cs`; setting it again at the call site is the established (harmless, idempotent) pattern here — keep doing it rather than removing it from either place.
- Output always goes to `Path.GetTempPath()` with a `{Prefix}_{yyyyMMdd_HHmmss}.pdf` name, then opened via `UseShellExecute = true` (opens in the user's default PDF viewer) — never write report PDFs elsewhere or return bytes to the caller.
- Wrap the whole generate-and-open block in try/catch per the error-handling convention in [[wpf-page-codebehind-navigation]].

## 7. Landscape/tabular tax reports

- For wide multi-column statutory forms (e.g. `ObrazacOADocument.cs`), use `PageSizes.A4.Landscape()` and keep the same header/table/footer decomposition — only the column count and page orientation change.

---
name: questpdf-report-documents
description: Conventions for QuestPDF report/print documents in AccountingApp (General Ledger, Trial Balance, Open Items / IOS, Warehouse Stock Reports) — IDocument structure, header/footer/table styling, and the generate-and-open flow. Use whenever adding or editing a *Document.cs file under AccountingApp/Services or generating a new PDF report.
---

# QuestPDF Document Conventions (AccountingApp)

Every printable report in `AccountingApp` is a small `IDocument` class colocated with its feature (e.g. `Services/PdfReportService.cs`, `Views/Izvestaji/DnevnikDocument.cs`). Follow the existing shape rather than inventing a new document scaffold.

---

## 1. Class Shape

- Name: `<Feature>Document.cs`, implements `QuestPDF.Infrastructure.IDocument`.
- Constructor takes plain data (a `List<...>` DTO built by the calling page, plus optional `AccountingData.Models.Firma? firma`) — never an `AccountingDbContext`. Query the DB in the page/window, hand the document only the data it needs to render.

## 2. `Compose(IDocumentContainer container)`

```csharp
container.Page(page =>
{
    page.Size(PageSizes.A4.Portrait());
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
- Right column (`row.ConstantItem(250).AlignRight()`): company block from `Firma` — Naziv (bold, black), Mesto, `PIB: {firma.Pib}` — each guarded with `if (!string.IsNullOrEmpty(...))`.

## 4. Tables

- `table.ColumnsDefinition` with `ConstantColumn(px)` for fixed fields (codes, dates, amounts) and exactly one `RelativeColumn()` for the free-text/name column.
- Header row: local `static IContainer HeaderStyle(IContainer c)` — dark background (`Colors.Blue.Darken4`), white semibold text, `FontSize(8)`, `PaddingVertical(4).PaddingHorizontal(4)`.
- Data rows: local `static IContainer RowStyle(IContainer c)` — `BorderBottom(0.5f)` in `Colors.Grey.Lighten2`, `FontSize(7.5f)`, same padding. Right-align numeric cells with `.AlignRight()`.
- Money values format as `value.ToString("N2")`; dates as `date.ToString("dd.MM.yyyy.")`.
- Summary row: add right-aligned bold totals.

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

## 6. Generating & Opening the PDF

```csharp
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
byte[] pdfBytes = PdfReportService.GenerisiDnevnikPdf(firma, nalozi);
string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
System.IO.File.WriteAllBytes(filePath, pdfBytes);

var p = new System.Diagnostics.Process();
p.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
p.Start();
```

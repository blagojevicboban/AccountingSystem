# 💼 AccountingSystem — Financial & General Ledger ERP

> Modern desktop ERP application for Financial Accounting (General Ledger, Journal Entries, Chart of Accounts), Subledger Partners (Open Items & Statements), Warehouse Inventory Management, Trade & Invoicing — developed in **C# / .NET 8 / WPF**.

---

## ✨ Features

- 🏢 **Multi-Company Architecture** — Manage unlimited companies with independent SQLite database instances per company.
- 📊 **Dashboard & Metrics** — Overview of active accounts, posted entries, warehouse stock levels, and subledger balances.
- 📖 **General Ledger & Journal Entries** — Entry processing with strict balance verification (Debit == Credit), journal logs, and account cards with running balance.
- 📋 **Chart of Accounts** — Synthetic and analytical accounts (Classes 0–9) with real-time search.
- 👥 **Subledger (Customers & Vendors)** — Open items tracking, customer/vendor statements, and PDF **IOS forms**.
- 📦 **Inventory & Stock Management** — Material receipts, requisitions, internal transfers, and warehouse cards using weighted average cost.
- 🛒 **Trade & Invoices** — Wholesale & retail cost calculations, price adjustments, and Trade Book logging (KEO form).
- 🔄 **Legacy DBF Migration (`AccountingMigration`)** — Automated import tool for dBase III / Clipper files (`C:\KNJIGE\Radni\KORxx`).
- 📄 **PDF Reporting (`QuestPDF`)** — High-performance PDF generation for Journal Entries, Trial Balance, Open Items, and Stock Reports.

---

## 🛠️ Technology Stack

| Domain | Technology |
| --- | --- |
| **Language** | C# 12 / .NET 8.0 |
| **UI Framework** | WPF (Windows Presentation Foundation) |
| **Charts** | LiveCharts2 (SkiaSharp) |
| **Database** | SQLite (one instance per company) |
| **ORM** | Entity Framework Core 8 |
| **Reporting / PDF** | QuestPDF |
| **Legacy DBF Parser** | Custom binary dBase III parser (Latin1 / YUSCII / CP852) |

---

## 📁 Project Structure

```text
AccountingSystem/
├── AccountingApp/            # Primary WPF Desktop App (Views, ViewModels, PDF Reports)
│   ├── Views/                # WPF Controls (Dashboard, Journal Entries, Accounts, Subledger, Inventory)
│   ├── Services/             # PdfReportService, NaloziService
│   └── AppConfig.cs          # Database path manager & environment setup
├── AccountingData/           # Data Access Layer (EF Core Models & DbContext)
│   └── Models/               # Firma, Korisnik, Konto, Nalog, StavkaNaloga, Partner, Artikal, Magacin
├── AccountingData.Tests/     # xUnit Test Suite with In-Memory SQLite provider
├── AccountingMigration/      # Legacy DBF Clipper import console utility
└── .vscode/                  # VS Code launch.json & tasks.json for F5 debugging
```

---

## 🚀 Quickstart

```bash
# 1. Build project
dotnet build

# 2. Run application
dotnet run --project AccountingApp/AccountingApp.csproj

# 3. Execute unit tests
dotnet test AccountingData.Tests/AccountingData.Tests.csproj
```

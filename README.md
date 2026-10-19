# Expense Tracker ECU

A completed C# 14 / .NET 10 Avalonia 12.1.1 desktop implementation based on the supplied Assignment 1 starter template. The original project filename and namespace are retained; the user-facing application name is corrected to Expense Tracker.

## Run on macOS, Windows or Linux

Install a stable .NET 10 SDK and open a terminal in this project folder containing `Expence_Tracker_ECU.sln`.

```bash
dotnet --version
dotnet restore Expence_Tracker_ECU.sln
dotnet build Expence_Tracker_ECU.sln -c Release -m:1
dotnet run --project Expence_Tracker_ECU.csproj -c Release
```

The first restore needs internet access to NuGet. `global.json` accepts installed .NET 10 feature bands, including 10.0.400. No database, Avalonia template installation or Visual Studio licence is required. A working desktop display is required for the application.

## First use

1. Launch the app using the command above. The initial transaction list is empty.
2. Enter a description, a positive amount such as `2500.00`, and a date in `yyyy-MM-dd` format. Choose Income or Expense and a category, then click **Save transaction**.
3. Set the dashboard month to match the transaction date (`yyyy-MM`) and click **Show month** to see the totals.
4. Select a transaction row to edit or delete it. Set a category budget and click **Save budget** to enable spending warnings.
5. Changes save automatically; close the window to quit.

## Verify the domain and view model

```bash
dotnet run --project Verification/Verification.csproj -c Release
```

Expected result: `RESULT 33/33 checks passed`, exit code 0. This dependency-free runner tests the actual core and linked presentation source. It is not an xUnit project and is run with `dotnet run`, not `dotnet test`. It does not test native window appearance or OS integration. The supplied build and verification logs record a Linux build with SDK 10.0.100 and runtime 10.0.0; recreate desktop evidence on your own machine.

## Features and behaviour

- Add, select/edit and delete income or expense transactions.
- Enter positive LKR amounts with up to two decimal places; use a decimal point without commas. Maximum individual amount is 1,000,000,000,000.
- Dates use `yyyy-MM-dd`; dashboard months use `yyyy-MM`. Dates from 1900 to 2100 are accepted.
- Six initial categories: Food, Transport, Bills, Shopping, Other and Income. Category creation/renaming is outside this small demonstration's scope.
- Monthly income, expenses, balance and category spending. Income never consumes a spending budget.
- Category budgets apply each month. Zero disables the budget. Below 80% is green, 80% through 100% is amber, and above 100% is red.
- Budget warnings are event-driven and appear when spending crosses over the budget. Loading existing overspending does not generate a fresh crossing; the red dashboard still shows it. Recovery below the budget allows a later crossing to warn again.
- Category and inclusive date filters, plus date/amount sorting. The transaction list includes all dates until a filter is supplied; the dashboard month is independent of these filters.
- JSON persistence after each successful change. Failed input keeps the editor values. Failed initial reads keep editing disabled.

## Local data and recovery

The file path is displayed at the bottom of the application. It is `tracker.json` under the operating system's local application-data directory in `ExpenseTrackerECU`. The path is chosen by `Environment.SpecialFolder.LocalApplicationData` so it is not hard-coded for macOS. It is plain local JSON.

Use one application instance at a time. `SemaphoreSlim` protects concurrent operations through the same repository instance; it is not an inter-process lock. The whole file is read and rewritten, which suits a small personal demonstration rather than a large ledger.

Before experimenting with corrupt-file recovery, quit the application and copy the data file somewhere safe. A corrupt or unsupported file is reported and left intact. To start fresh, move it aside and relaunch. Do not replace your only copy.

## Capture the report screenshots

Use October 2026 as the dashboard month (or consistently update all demonstration dates).

1. **S1 dashboard and editor:** enter Income `Salary`, 50000.00, 2026-10-01, category Income; enter Expense `Groceries`, 2500.00, 2026-10-09, category Food. The October totals should be income 50000.00, expense 2500.00, balance 47500.00. Capture the whole window.
2. **S2 editing and filtering:** select Groceries, change it to 3000.00 and save. Set category Food, date range 2026-10-01 to 2026-10-31, sort Amount high, and Apply filters. Capture the row and totals. Test Delete selected with a disposable extra transaction.
3. **S3 budget event:** leave Food's monthly budget at 5000.00; add Expense `Dinner`, 2500.00, 2026-10-10, category Food. Food spending becomes 5500.00 and exceeds the limit by 500.00. Capture the red category status and the warning.
4. **S4 validation and restart:** enter a negative amount and Save transaction; capture the validation error with no new row. Close and reopen the app and capture the previously saved rows and budgets (optionally combine two genuine screenshots in the placeholder).
5. **S5 verification:** run the verification command and capture the terminal's final `RESULT 33/33 checks passed` line. Use your own run as submission evidence.

The report deliberately reserves these spaces. It does not claim that screenshots have already been captured or that the app was demonstrated on macOS here.

## Report diagrams and source attribution

The `docs/figure*.mmd` files contain the exact Mermaid sources included beneath the report's diagram placeholders. Render each in a Mermaid editor and insert it in the matching report location. The class diagram lists the main domain/service classes; it abbreviates generic return types and omits command helpers and presentation projections for legibility.

The report's selected listings are taken from this project. `Verification/LanguageExamples.cs` contains the illustrative parameter-passing and exception-effect examples. Framework starter files originate from the supplied Avalonia template. Primary documentation URLs and the supplied ECU modules are cited in the report. Review the code, run the application yourself, and record any assistance in the form required by your unit before submission; no declaration of sole authorship has been added.

## Refinements from the preliminary design

The original repository abstraction, transactions, categories, budget monitor and MVVM responsibilities are retained. A single `MainViewModel` combines the proposed navigation/dashboard/editor view models because this implementation uses one screen. Immutable records and category IDs reduce shared mutable state and repeated category data; `DateOnly` represents transaction dates without time zones. Explicit property notification replaces the starter's toolkit-generated greeting so the report can demonstrate the notification idiom. Four original Avalonia dependencies are pinned to 12.1.1; unused toolkit and diagnostic packages are removed.

## Project layout

| Folder | Responsibility |
| --- | --- |
| Views, ViewModels, Commands (project root) | Avalonia AXAML interface, commands and view model |
| ExpenseTracker.Core | Immutable domain records, validation, JSON repository and dashboard/budget services |
| Verification | Executable regression checks and report language examples |
| docs | Mermaid source, verified build log, verification log and report preparation notes |

Build artefacts, caches, runtime installers and personal data are excluded from the ZIP. This is a source project, not a platform-specific installer.

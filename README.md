# Expense Tracker ECU

A C# 14 / .NET 10 desktop application built with Avalonia 12.1.1.

## Requirements

- .NET 10 SDK
- A working desktop display

## Setup and Run

Open a terminal in this project folder and run:

```bash
dotnet restore Expence_Tracker_ECU.sln
dotnet build Expence_Tracker_ECU.sln -c Release -m:1
dotnet run --project Expence_Tracker_ECU.csproj -c Release
```

The first restore requires internet access to download NuGet packages.

## Run Verification Checks

```bash
dotnet run --project Verification/Verification.csproj -c Release
```

Expected output: `RESULT 33/33 checks passed`

## Features

- Add, edit and delete income or expense transactions
- Positive LKR amounts up to two decimal places; maximum 1,000,000,000,000
- Dates in `yyyy-MM-dd` format (1900–2100); dashboard months in `yyyy-MM`
- Six default categories: Food, Transport, Bills, Shopping, Other, Income
- Monthly income, expenses, balance and per-category spending
- Category budgets with colour indicators (green / amber / red)
- Event-driven budget warnings when spending crosses the limit
- Category and date filters with date/amount sorting
- JSON persistence after each successful change

## Data Storage

Transaction data is saved to `tracker.json` under the OS local application-data directory in `ExpenseTrackerECU`. The file path is shown at the bottom of the application window.

Use one application instance at a time. To start fresh, move the data file aside and relaunch.

## Project Layout

| Folder                      | Responsibility                                          |
| --------------------------- | ------------------------------------------------------- |
| Views, ViewModels, Commands | Avalonia AXAML interface, commands and view model       |
| ExpenseTracker.Core         | Domain models, validation, JSON repository and services |
| Verification                | Regression checks and language examples                 |
| docs                        | Mermaid diagrams, build log and verification log        |

using System.Text.Json;
using ExpenseTracker.Core.Models;
using ExpenseTracker.Core.Persistence;
using ExpenseTracker.Core.Services;
using Expence_Tracker_ECU.Commands;
using Expence_Tracker_ECU.ViewModels;
using Verification;

// Dependency-free regression runner: a failed check exits with code 1.
// Each persistence check uses an isolated temporary folder, never the user's data.
var checks = new List<(string Name, Func<Task> Run)>();
void Check(string name, Action action) => checks.Add((name, () => { action(); return Task.CompletedTask; }));
void CheckAsync(string name, Func<Task> action) => checks.Add((name, action));
void Assert(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
Category Food(decimal budget = 100m) => TrackerData.CreateDefault().Categories[0] with { MonthlyBudget = budget };
Transaction Item(decimal amount = 20m, int day = 9, TransactionType type = TransactionType.Expense)
    => new(Guid.NewGuid(), "Test item", amount, Food().Id, new DateOnly(2026, 10, day), type);
async Task WithRepository(Func<JsonTransactionRepository, string, Task> action)
{
    string folder = Path.Combine(Path.GetTempPath(), "ExpenseTrackerVerification", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(folder);
    try { await action(new(Path.Combine(folder, "tracker.json")), folder); }
    finally { Directory.Delete(folder, recursive: true); }
}

Check("decimal preserves the 0.1 plus 0.2 example", () => Assert(0.1m + 0.2m == 0.3m));
Check("value passing copies and ref changes the caller", () =>
{
    decimal amount = 10m; LanguageExamples.ChangeCopy(amount); Assert(amount == 10m);
    LanguageExamples.ChangeCaller(ref amount); Assert(amount == 99m);
});
Check("immutable category replacement leaves the original unchanged", () =>
{
    var original = Food(); var replacement = LanguageExamples.ChangeReferenceCopy(original);
    Assert(original.MonthlyBudget == 100m && replacement.MonthlyBudget == 99m);
});
Check("notification occurs once and uses the property name", () =>
{
    var model = new NotificationExample(); var names = new List<string?>();
    model.PropertyChanged += (_, e) => names.Add(e.PropertyName);
    model.Description = "Coffee"; model.Description = "Coffee";
    Assert(names.SequenceEqual(new[] { "Description" }));
});
Check("LINQ aggregates expenses and excludes income", () =>
    Assert(LanguageExamples.ExpenseTotal(new[] { Item(10m), Item(20m), Item(500m, type: TransactionType.Income) }) == 30m));
Check("monthly summary excludes other months and income", () =>
{
    var records = new[] { Item(20m), Item(100m, type: TransactionType.Income), Item(50m) with { Date = new(2026, 9, 9) } };
    Assert(DashboardService.Summarise(records, new[] { Food() }, new(2026, 10, 1))[0].Spent == 20m);
});
Check("budget status covers below 80 percent at 80 at 100 and above", () =>
{
    var expected = new[] { BudgetStatus.UnderBudget, BudgetStatus.NearLimit, BudgetStatus.NearLimit, BudgetStatus.OverBudget };
    var amounts = new[] { 79.99m, 80m, 100m, 100.01m };
    for (int i = 0; i < amounts.Length; i++)
        Assert(DashboardService.Summarise(new[] { Item(amounts[i]) }, new[] { Food() }, new(2026, 10, 1))[0].Status == expected[i]);
});
Check("zero budget disables threshold warnings", () =>
    Assert(DashboardService.Summarise(new[] { Item(1000m) }, new[] { Food(0m) }, new(2026, 10, 1))[0].Status == BudgetStatus.Disabled));
Check("budget event fires once then fires after recovery and recrossing", () =>
{
    var monitor = new BudgetMonitor(); int count = 0;
    monitor.BudgetExceeded += (_, e) => { Assert(e.Spent == 101m); count++; };
    monitor.CheckBudget(new[] { Item(101m) }, new[] { Food() }, new(2026, 10, 1));
    monitor.CheckBudget(new[] { Item(101m) }, new[] { Food() }, new(2026, 10, 1));
    monitor.CheckBudget(new[] { Item(80m) }, new[] { Food() }, new(2026, 10, 1));
    monitor.CheckBudget(new[] { Item(101m) }, new[] { Food() }, new(2026, 10, 1)); Assert(count == 2);
});
Check("silent startup baseline does not raise an event", () =>
{
    var monitor = new BudgetMonitor(); int count = 0; monitor.BudgetExceeded += (_, _) => count++;
    monitor.CheckBudget(new[] { Item(101m) }, new[] { Food() }, new(2026, 10, 1), notify: false);
    monitor.CheckBudget(new[] { Item(101m) }, new[] { Food() }, new(2026, 10, 1)); Assert(count == 0);
});
Check("category and date filters are inclusive", () =>
{
    var records = new[] { Item(10m, 8), Item(20m, 9), Item(30m, 10), Item(40m, 9) with { CategoryId = Guid.NewGuid() } };
    var result = DashboardService.FilterAndSort(records, Food().Id, new(2026, 10, 9), new(2026, 10, 10), TransactionSort.DateNewest);
    Assert(result.Count == 2 && result[0].Amount == 30m && result[1].Amount == 20m);
});
Check("all four sort modes order the rows", () =>
{
    var records = new[] { Item(30m, 8), Item(10m, 10), Item(20m, 9) };
    Assert(DashboardService.FilterAndSort(records, null, null, null, TransactionSort.DateNewest)[0].Date.Day == 10);
    Assert(DashboardService.FilterAndSort(records, null, null, null, TransactionSort.DateOldest)[0].Date.Day == 8);
    Assert(DashboardService.FilterAndSort(records, null, null, null, TransactionSort.AmountHigh)[0].Amount == 30m);
    Assert(DashboardService.FilterAndSort(records, null, null, null, TransactionSort.AmountLow)[0].Amount == 10m);
});
CheckAsync("invalid and excessive amounts are rejected", async () =>
{
    foreach (decimal amount in new[] { 0m, -1m, 1.001m, DataRules.MaximumAmount + 1m })
        await Throws<ArgumentException>(() => { DataRules.ValidateAmount(amount); return Task.CompletedTask; });
});
CheckAsync("invalid descriptions categories dates and types are rejected", async () =>
{
    var invalid = new[] { Item() with { Description = " " }, Item() with { Description = new string('x', 121) },
        Item() with { CategoryId = Guid.NewGuid() }, Item() with { Date = new(1800, 1, 1) },
        Item() with { Type = (TransactionType)99 } };
    foreach (var item in invalid)
        await Throws<ArgumentException>(() => { DataRules.Validate(item, new[] { Food() }); return Task.CompletedTask; });
});
CheckAsync("new storage has empty transactions and six defaults", () => WithRepository(async (r, _) =>
    Assert((await r.GetAllAsync()).Count == 0 && (await r.GetCategoriesAsync()).Count == 6)));
CheckAsync("add persists exact decimal values across a new instance", () => WithRepository(async (r, _) =>
{
    var item = Item(1234.56m); await r.AddAsync(item);
    Assert((await new JsonTransactionRepository(r.FilePath).GetAllAsync()).Single() == item);
}));
CheckAsync("edit preserves the identifier and replaces values", () => WithRepository(async (r, _) =>
{
    var item = Item(); await r.AddAsync(item); await r.UpdateAsync(item with { Amount = 99m, Description = "Edited" });
    var saved = (await r.GetAllAsync()).Single(); Assert(saved.Id == item.Id && saved.Amount == 99m && saved.Description == "Edited");
}));
CheckAsync("delete persists removal", () => WithRepository(async (r, _) =>
{
    var item = Item(); await r.AddAsync(item); await r.DeleteAsync(item.Id);
    Assert((await new JsonTransactionRepository(r.FilePath).GetAllAsync()).Count == 0);
}));
CheckAsync("changed category budget persists across restart", () => WithRepository(async (r, _) =>
{
    var category = (await r.GetCategoriesAsync())[0]; await r.UpdateCategoryAsync(category with { MonthlyBudget = 9876.54m });
    Assert((await new JsonTransactionRepository(r.FilePath).GetCategoriesAsync())[0].MonthlyBudget == 9876.54m);
}));
CheckAsync("duplicate ids fail without changing the committed file", () => WithRepository(async (r, _) =>
{
    var item = Item(); await r.AddAsync(item); string before = await File.ReadAllTextAsync(r.FilePath);
    await Throws<ArgumentException>(() => r.AddAsync(item)); Assert(await File.ReadAllTextAsync(r.FilePath) == before);
}));
CheckAsync("invalid update fails without losing the previous amount", () => WithRepository(async (r, _) =>
{
    var item = Item(); await r.AddAsync(item); await Throws<ArgumentException>(() => r.UpdateAsync(item with { Amount = -1m }));
    Assert((await r.GetAllAsync()).Single().Amount == 20m);
}));
CheckAsync("missing update and delete release the semaphore", () => WithRepository(async (r, _) =>
{
    await Throws<KeyNotFoundException>(() => r.UpdateAsync(Item()));
    await Throws<KeyNotFoundException>(() => r.DeleteAsync(Guid.NewGuid()));
    await r.AddAsync(Item()); Assert((await r.GetAllAsync()).Count == 1);
}));
CheckAsync("twenty concurrent additions have no lost records", () => WithRepository(async (r, _) =>
{
    await Task.WhenAll(Enumerable.Range(1, 20).Select(i => r.AddAsync(Item(i))));
    var records = await r.GetAllAsync(); Assert(records.Count == 20 && records.Sum(t => t.Amount) == 210m);
}));
CheckAsync("corrupt JSON is reported and is not overwritten", () => WithRepository(async (r, _) =>
{
    const string broken = "{ broken JSON"; await File.WriteAllTextAsync(r.FilePath, broken);
    await Throws<JsonException>(() => r.GetAllAsync()); await Throws<JsonException>(() => r.AddAsync(Item()));
    Assert(await File.ReadAllTextAsync(r.FilePath) == broken);
}));
CheckAsync("invalid schema version is rejected", () => WithRepository(async (r, _) =>
{
    await File.WriteAllTextAsync(r.FilePath, "{\"SchemaVersion\":2}");
    await Throws<InvalidDataException>(() => r.GetAllAsync());
}));
CheckAsync("cancellation does not poison the repository", () => WithRepository(async (r, _) =>
{
    using var source = new CancellationTokenSource(); source.Cancel();
    await Throws<OperationCanceledException>(() => r.AddAsync(Item(), source.Token));
    await r.AddAsync(Item()); Assert((await r.GetAllAsync()).Count == 1);
}));
CheckAsync("successful writes leave no temporary files", () => WithRepository(async (r, folder) =>
{
    await r.AddAsync(Item()); Assert(Directory.GetFiles(folder, "*.tmp").Length == 0);
}));
CheckAsync("read failure effects are absent from the method return type", async () =>
{
    await Throws<FileNotFoundException>(() => { _ = LanguageExamples.ReadText(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing")); return Task.CompletedTask; });
});
CheckAsync("view model validates input and preserves failed editor values", () => WithRepository(async (r, _) =>
{
    using var vm = new MainViewModel(r); await vm.InitializeAsync(); vm.Description = "Coffee"; vm.AmountText = "-1";
    await vm.SaveCommand.ExecuteAsync(); Assert(vm.ErrorMessage.Length > 0 && vm.Description == "Coffee" && vm.VisibleTransactions.Count == 0);
}));
CheckAsync("view model CRUD and monthly dashboard stay consistent", () => WithRepository(async (r, _) =>
{
    using var vm = new MainViewModel(r); await vm.InitializeAsync(); vm.MonthText = "2026-10"; vm.ApplyMonthCommand.Execute(null);
    vm.Description = "Coffee"; vm.AmountText = "100.25"; vm.DateText = "2026-10-09"; await vm.SaveCommand.ExecuteAsync();
    Assert(vm.VisibleTransactions.Count == 1 && vm.ExpenseText.Contains("100.25"));
    vm.SelectedTransaction = vm.VisibleTransactions[0]; vm.AmountText = "200.50"; await vm.SaveCommand.ExecuteAsync();
    Assert(vm.VisibleTransactions[0].Transaction.Amount == 200.50m);
    vm.SelectedTransaction = vm.VisibleTransactions[0]; await vm.DeleteCommand.ExecuteAsync();
    Assert(vm.VisibleTransactions.Count == 0 && vm.ExpenseText.Contains("0.00"));
}));
CheckAsync("view model shows a budget crossing and rejects reversed dates", () => WithRepository(async (r, _) =>
{
    using var vm = new MainViewModel(r); await vm.InitializeAsync(); vm.MonthText = "2026-10"; vm.ApplyMonthCommand.Execute(null);
    vm.BudgetText = "100"; await vm.SaveBudgetCommand.ExecuteAsync();
    vm.Description = "Food"; vm.AmountText = "101"; vm.DateText = "2026-10-09"; await vm.SaveCommand.ExecuteAsync();
    Assert(vm.WarningMessage.Contains("Budget exceeded"));
    vm.FromText = "2026-10-10"; vm.ToText = "2026-10-09"; vm.ApplyFiltersCommand.Execute(null);
    Assert(vm.ErrorMessage.Contains("cannot be after"));
}));
CheckAsync("view model corrupt-file load keeps editing disabled", () => WithRepository(async (r, _) =>
{
    await File.WriteAllTextAsync(r.FilePath, "bad JSON"); using var vm = new MainViewModel(r); await vm.InitializeAsync();
    Assert(!vm.CanEdit && !vm.SaveCommand.CanExecute(null) && vm.ErrorMessage.Length > 0);
}));
CheckAsync("async command observes an exception and re-enables itself", async () =>
{
    Exception? caught = null; var command = new AsyncCommand(() => throw new IOException("Simulated failure"), () => true, ex => caught = ex);
    await command.ExecuteAsync(); Assert(caught is IOException && command.CanExecute(null));
});

int failed = 0;
foreach (var (name, run) in checks)
{
    try { await run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}"); }
}
Console.WriteLine($"RESULT {checks.Count - failed}/{checks.Count} checks passed");
return failed == 0 ? 0 : 1;

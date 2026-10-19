using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using ExpenseTracker.Core.Models;
using ExpenseTracker.Core.Persistence;
using ExpenseTracker.Core.Services;
using Expence_Tracker_ECU.Commands;

namespace Expence_Tracker_ECU.ViewModels;

/// <summary>Coordinates editor validation, persistence, queries and notifications on the UI context.</summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ITransactionRepository _repository;
    private readonly BudgetMonitor _budgetMonitor = new();
    private IReadOnlyList<Transaction> _transactions = [];
    private DateOnly _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private Guid? _editingId;
    private bool _isBusy, _dataReady;
    private string _description = "", _amountText = "", _dateText = DateTime.Today.ToString("yyyy-MM-dd");
    private string _monthText = DateTime.Today.ToString("yyyy-MM"), _fromText = "", _toText = "";
    private string _sortSelection = "Date newest", _filterCategory = "All categories";
    private string _budgetText = "", _statusMessage = "Ready to load local data.", _errorMessage = "", _warningMessage = "";
    private string _incomeText = "LKR 0.00", _expenseText = "LKR 0.00", _balanceText = "LKR 0.00";
    private TransactionType _selectedType = TransactionType.Expense;
    private Category? _selectedCategory, _budgetCategory;
    private TransactionRow? _selectedTransaction;

    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExpenseTrackerECU", "tracker.json");
    public string DataFilePath { get; }
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<CategorySummary> Summaries { get; } = [];
    public ObservableCollection<TransactionRow> VisibleTransactions { get; } = [];
    public ObservableCollection<string> CategoryFilters { get; } = ["All categories"];
    public string[] SortOptions { get; } = ["Date newest", "Date oldest", "Amount high", "Amount low"];
    public TransactionType[] TransactionTypes { get; } = Enum.GetValues<TransactionType>();
    public AsyncCommand ReloadCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public AsyncCommand SaveBudgetCommand { get; }
    public RelayCommand NewCommand { get; }
    public RelayCommand ApplyFiltersCommand { get; }
    public RelayCommand ApplyMonthCommand { get; }

    public MainViewModel() : this(new JsonTransactionRepository(DefaultFilePath), DefaultFilePath) { }
    public MainViewModel(ITransactionRepository repository, string dataFilePath = "in-memory")
    {
        _repository = repository;
        DataFilePath = dataFilePath;
        _budgetMonitor.BudgetExceeded += OnBudgetExceeded;
        ReloadCommand = new(InitializeAsync, () => !IsBusy, ShowUnexpectedError);
        SaveCommand = new(SaveAsync, () => CanEdit, ShowUnexpectedError);
        DeleteCommand = new(DeleteAsync, () => CanEdit && _editingId.HasValue, ShowUnexpectedError);
        SaveBudgetCommand = new(SaveBudgetAsync, () => CanEdit, ShowUnexpectedError);
        NewCommand = new(ResetEditor, () => CanEdit);
        ApplyFiltersCommand = new(ApplyFilters, () => CanEdit);
        ApplyMonthCommand = new(ApplyMonth, () => CanEdit);
    }

    public bool IsBusy { get => _isBusy; private set { if (SetProperty(ref _isBusy, value)) UpdateCommands(); } }
    public bool CanEdit => _dataReady && !IsBusy;
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string AmountText { get => _amountText; set => SetProperty(ref _amountText, value); }
    public string DateText { get => _dateText; set => SetProperty(ref _dateText, value); }
    public string MonthText { get => _monthText; set => SetProperty(ref _monthText, value); }
    public string FromText { get => _fromText; set => SetProperty(ref _fromText, value); }
    public string ToText { get => _toText; set => SetProperty(ref _toText, value); }
    public string SortSelection { get => _sortSelection; set => SetProperty(ref _sortSelection, value); }
    public string FilterCategory { get => _filterCategory; set => SetProperty(ref _filterCategory, value); }
    public string BudgetText { get => _budgetText; set => SetProperty(ref _budgetText, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string WarningMessage { get => _warningMessage; private set => SetProperty(ref _warningMessage, value); }
    public string IncomeText { get => _incomeText; private set => SetProperty(ref _incomeText, value); }
    public string ExpenseText { get => _expenseText; private set => SetProperty(ref _expenseText, value); }
    public string BalanceText { get => _balanceText; private set => SetProperty(ref _balanceText, value); }
    public TransactionType SelectedType { get => _selectedType; set => SetProperty(ref _selectedType, value); }
    public Category? SelectedCategory { get => _selectedCategory; set => SetProperty(ref _selectedCategory, value); }
    public Category? BudgetCategory
    {
        get => _budgetCategory;
        set { if (SetProperty(ref _budgetCategory, value)) BudgetText = value?.MonthlyBudget.ToString("0.00", CultureInfo.InvariantCulture) ?? ""; }
    }
    public TransactionRow? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            if (!SetProperty(ref _selectedTransaction, value) || value is null) return;
            var transaction = value.Transaction;
            _editingId = transaction.Id;
            Description = transaction.Description;
            AmountText = transaction.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            DateText = transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            SelectedType = transaction.Type;
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == transaction.CategoryId);
            UpdateCommands();
        }
    }

    public Task InitializeAsync() => RunOperationAsync(async () =>
    {
        await RefreshDataAsync(notify: false);
        // Establish existing over-budget states without inventing a new crossing at startup.
        foreach (var month in _transactions.Select(t => new DateOnly(t.Date.Year, t.Date.Month, 1)).Distinct())
            _budgetMonitor.CheckBudget(_transactions, Categories, month, notify: false);
        ResetEditor();
        StatusMessage = "Local data loaded. Changes are saved after each successful operation.";
    });

    private Task SaveAsync() => RunOperationAsync(async () =>
    {
        if (!decimal.TryParse(AmountText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out decimal amount))
            throw new ArgumentException("Enter an amount using a decimal point, for example 1250.50.");
        if (!TryDate(DateText, out DateOnly date)) throw new ArgumentException("Use a date in yyyy-MM-dd format.");
        if (SelectedCategory is null) throw new ArgumentException("Select a category.");
        var transaction = new Transaction(_editingId ?? Guid.NewGuid(), Description.Trim(), amount,
            SelectedCategory.Id, date, SelectedType);
        DataRules.Validate(transaction, Categories);
        bool editing = _editingId.HasValue;
        if (editing) await _repository.UpdateAsync(transaction);
        else await _repository.AddAsync(transaction);
        await RefreshDataAsync(notify: true);
        if (date.Month != _month.Month || date.Year != _month.Year)
            _budgetMonitor.CheckBudget(_transactions, Categories, new(date.Year, date.Month, 1));
        ResetEditor();
        StatusMessage = editing ? "Transaction updated and saved." : "Transaction added and saved.";
    });

    private Task DeleteAsync() => RunOperationAsync(async () =>
    {
        if (_editingId is not Guid id) return;
        await _repository.DeleteAsync(id);
        await RefreshDataAsync(notify: true);
        ResetEditor();
        StatusMessage = "Selected transaction deleted and saved.";
    });

    private Task SaveBudgetAsync() => RunOperationAsync(async () =>
    {
        if (BudgetCategory is null) throw new ArgumentException("Select a budget category.");
        if (!decimal.TryParse(BudgetText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out decimal budget)) throw new ArgumentException("Enter a valid budget.");
        DataRules.ValidateAmount(budget, allowZero: true);
        await _repository.UpdateCategoryAsync(BudgetCategory with { MonthlyBudget = budget });
        await RefreshDataAsync(notify: true);
        StatusMessage = "Monthly category budget saved.";
    });

    private async Task RefreshDataAsync(bool notify)
    {
        Guid? selectedCategoryId = SelectedCategory?.Id, budgetCategoryId = BudgetCategory?.Id;
        var categories = await _repository.GetCategoriesAsync();
        var transactions = await _repository.GetAllAsync();
        // Apply one complete snapshot only after both reads succeed.
        _transactions = transactions;
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);
        CategoryFilters.Clear(); CategoryFilters.Add("All categories");
        foreach (var category in categories) CategoryFilters.Add(category.Name);
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == selectedCategoryId) ?? Categories.FirstOrDefault();
        BudgetCategory = Categories.FirstOrDefault(c => c.Id == budgetCategoryId) ?? Categories.FirstOrDefault();
        _dataReady = true;
        UpdateCommands();
        UpdateDashboard();
        ApplyFilters();
        _budgetMonitor.CheckBudget(_transactions, Categories, _month, notify);
        if (!Summaries.Any(s => s.Status == BudgetStatus.OverBudget)) WarningMessage = "";
    }

    private void UpdateDashboard()
    {
        Summaries.Clear();
        foreach (var summary in DashboardService.Summarise(_transactions, Categories, _month)) Summaries.Add(summary);
        var monthly = _transactions.Where(t => DashboardService.IsInMonth(t, _month)).ToArray();
        decimal income = monthly.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        decimal expense = monthly.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        IncomeText = $"LKR {income:N2}"; ExpenseText = $"LKR {expense:N2}"; BalanceText = $"LKR {income - expense:N2}";
    }

    private void ApplyFilters()
    {
        ErrorMessage = "";
        DateOnly? from = null, to = null;
        if (!string.IsNullOrWhiteSpace(FromText))
        {
            if (!TryDate(FromText, out var parsed)) { ErrorMessage = "From date must use yyyy-MM-dd."; return; }
            from = parsed;
        }
        if (!string.IsNullOrWhiteSpace(ToText))
        {
            if (!TryDate(ToText, out var parsed)) { ErrorMessage = "To date must use yyyy-MM-dd."; return; }
            to = parsed;
        }
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        { ErrorMessage = "From date cannot be after the to date."; return; }
        var sort = SortSelection switch
        {
            "Date oldest" => TransactionSort.DateOldest, "Amount high" => TransactionSort.AmountHigh,
            "Amount low" => TransactionSort.AmountLow, _ => TransactionSort.DateNewest
        };
        Guid? categoryId = Categories.FirstOrDefault(c => c.Name == FilterCategory)?.Id;
        var rows = DashboardService.FilterAndSort(_transactions, categoryId, from, to, sort);
        VisibleTransactions.Clear();
        foreach (var transaction in rows)
            VisibleTransactions.Add(new(transaction, Categories.First(c => c.Id == transaction.CategoryId).Name));
    }

    private void ApplyMonth()
    {
        ErrorMessage = "";
        if (!TryDate(MonthText + "-01", out var month)) { ErrorMessage = "Dashboard month must use yyyy-MM."; return; }
        _month = month;
        UpdateDashboard();
        _budgetMonitor.CheckBudget(_transactions, Categories, _month, notify: false);
        WarningMessage = "";
    }

    private void ResetEditor()
    {
        _editingId = null; SelectedTransaction = null;
        Description = ""; AmountText = ""; DateText = DateTime.Today.ToString("yyyy-MM-dd");
        SelectedType = TransactionType.Expense;
        UpdateCommands();
    }

    private async Task RunOperationAsync(Func<Task> operation)
    {
        if (IsBusy) return;
        IsBusy = true; ErrorMessage = "";
        try { await operation(); }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or
            ArgumentException or KeyNotFoundException)
        { ErrorMessage = ex.Message; StatusMessage = "Operation failed. Check the message and retry."; }
        finally { IsBusy = false; }
    }

    private static bool TryDate(string text, out DateOnly date)
        => DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            && date.Year >= 1900 && date.Year <= 2100;

    private void OnBudgetExceeded(object? sender, BudgetExceededEventArgs args)
        => WarningMessage = $"Budget exceeded: {args.Category.Name} spent LKR {args.Spent:N2} " +
            $"against LKR {args.Category.MonthlyBudget:N2} in {args.Month:yyyy-MM}.";

    private void ShowUnexpectedError(Exception exception)
    { ErrorMessage = $"Unexpected error: {exception.Message}"; StatusMessage = "Operation failed."; }

    private void UpdateCommands()
    {
        OnPropertyChanged(nameof(CanEdit));
        ReloadCommand?.NotifyCanExecuteChanged(); SaveCommand?.NotifyCanExecuteChanged();
        DeleteCommand?.NotifyCanExecuteChanged(); SaveBudgetCommand?.NotifyCanExecuteChanged();
        NewCommand?.NotifyCanExecuteChanged(); ApplyFiltersCommand?.NotifyCanExecuteChanged();
        ApplyMonthCommand?.NotifyCanExecuteChanged();
    }

    public void Dispose() => _budgetMonitor.BudgetExceeded -= OnBudgetExceeded;
}

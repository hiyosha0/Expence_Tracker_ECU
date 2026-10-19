using ExpenseTracker.Core.Models;

namespace ExpenseTracker.Core.Services;

public sealed class BudgetExceededEventArgs(Category category, decimal spent, DateOnly month) : EventArgs
{
    public Category Category { get; } = category;
    public decimal Spent { get; } = spent;
    public DateOnly Month { get; } = month;
}

/// <summary>Notifies once when a category crosses over budget; resets after recovery.</summary>
public sealed class BudgetMonitor
{
    private readonly HashSet<(Guid CategoryId, int Year, int Month)> _overBudget = [];
    public event EventHandler<BudgetExceededEventArgs>? BudgetExceeded;

    public void CheckBudget(IEnumerable<Transaction> transactions, IEnumerable<Category> categories,
        DateOnly month, bool notify = true)
    {
        foreach (var summary in DashboardService.Summarise(transactions, categories, month))
        {
            var key = (summary.Category.Id, month.Year, month.Month);
            if (summary.Status == BudgetStatus.OverBudget)
            {
                if (_overBudget.Add(key) && notify)
                    BudgetExceeded?.Invoke(this, new(summary.Category, summary.Spent, month));
            }
            else _overBudget.Remove(key);
        }
    }
}

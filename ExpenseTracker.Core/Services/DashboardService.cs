using ExpenseTracker.Core.Models;

namespace ExpenseTracker.Core.Services;

public enum BudgetStatus { Disabled, UnderBudget, NearLimit, OverBudget }
public enum TransactionSort { DateNewest, DateOldest, AmountHigh, AmountLow }

/// <summary>A read-only projection suitable for both GUI binding and isolated verification.</summary>
public sealed record CategorySummary(Category Category, decimal Spent, BudgetStatus Status)
{
    public string Colour => Status switch
    {
        BudgetStatus.OverBudget => "#B91C1C",
        BudgetStatus.NearLimit => "#B45309",
        BudgetStatus.UnderBudget => "#047857",
        _ => "#64748B"
    };
    public string StatusText => Status switch
    {
        BudgetStatus.OverBudget => "Over budget",
        BudgetStatus.NearLimit => "Near limit",
        BudgetStatus.UnderBudget => "Under budget",
        _ => "Budget disabled"
    };
    public string SpentText => $"LKR {Spent:N2}";
    public string BudgetText => $"Budget LKR {Category.MonthlyBudget:N2}";
}

/// <summary>Pure queries over a stable input snapshot; no GUI or file dependency.</summary>
public static class DashboardService
{
    public static IReadOnlyList<CategorySummary> Summarise(IEnumerable<Transaction> transactions,
        IEnumerable<Category> categories, DateOnly month)
    {
        var totals = transactions.Where(t => IsInMonth(t, month) && t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        return categories.Select(category =>
        {
            decimal spent = totals.GetValueOrDefault(category.Id);
            BudgetStatus status = category.MonthlyBudget == 0m ? BudgetStatus.Disabled
                : spent > category.MonthlyBudget ? BudgetStatus.OverBudget
                : spent >= category.MonthlyBudget * 0.8m ? BudgetStatus.NearLimit
                : BudgetStatus.UnderBudget;
            return new CategorySummary(category, spent, status);
        }).ToArray();
    }

    public static IReadOnlyList<Transaction> FilterAndSort(IEnumerable<Transaction> transactions,
        Guid? categoryId, DateOnly? from, DateOnly? to, TransactionSort sort)
    {
        var query = transactions.Where(t => (!categoryId.HasValue || t.CategoryId == categoryId.Value)
            && (!from.HasValue || t.Date >= from.Value) && (!to.HasValue || t.Date <= to.Value));
        return (sort switch
        {
            TransactionSort.DateOldest => query.OrderBy(t => t.Date).ThenBy(t => t.Id),
            TransactionSort.AmountHigh => query.OrderByDescending(t => t.Amount).ThenBy(t => t.Id),
            TransactionSort.AmountLow => query.OrderBy(t => t.Amount).ThenBy(t => t.Id),
            _ => query.OrderByDescending(t => t.Date).ThenBy(t => t.Id)
        }).ToArray();
    }

    public static bool IsInMonth(Transaction transaction, DateOnly month)
        => transaction.Date.Year == month.Year && transaction.Date.Month == month.Month;
}

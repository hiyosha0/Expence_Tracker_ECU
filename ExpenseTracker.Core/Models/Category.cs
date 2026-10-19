namespace ExpenseTracker.Core.Models;

/// <summary>A zero monthly budget disables warnings for this category.</summary>
public sealed record Category(Guid Id, string Name, string ColourHex, decimal MonthlyBudget);

/// <summary>The versioned local document; lists are private to each repository operation.</summary>
public sealed class TrackerData
{
    public int SchemaVersion { get; set; } = 1;
    public List<Category> Categories { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];

    public static TrackerData CreateDefault() => new()
    {
        Categories =
        [
            new(new("10000000-0000-0000-0000-000000000001"), "Food", "#2563EB", 5000m),
            new(new("10000000-0000-0000-0000-000000000002"), "Transport", "#7C3AED", 3000m),
            new(new("10000000-0000-0000-0000-000000000003"), "Bills", "#0891B2", 10000m),
            new(new("10000000-0000-0000-0000-000000000004"), "Shopping", "#DB2777", 4000m),
            new(new("10000000-0000-0000-0000-000000000005"), "Other", "#64748B", 2000m),
            new(new("10000000-0000-0000-0000-000000000006"), "Income", "#059669", 0m)
        ]
    };
}

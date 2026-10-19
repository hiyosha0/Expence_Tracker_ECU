namespace ExpenseTracker.Core.Models;

/// <summary>Checks both GUI input and deserialised records before data can be committed.</summary>
public static class DataRules
{
    public const decimal MaximumAmount = 1_000_000_000_000m;

    public static void ValidateAmount(decimal amount, bool allowZero = false)
    {
        if (amount < 0m || (!allowZero && amount == 0m) || amount > MaximumAmount)
            throw new ArgumentOutOfRangeException(nameof(amount),
                $"Amount must be {(allowZero ? "zero or positive" : "positive")} and at most {MaximumAmount:N0}.");
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Use at most two decimal places.", nameof(amount));
    }

    public static void Validate(Transaction transaction, IReadOnlyCollection<Category> categories)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (transaction.Id == Guid.Empty) throw new ArgumentException("A transaction needs an Id.");
        if (string.IsNullOrWhiteSpace(transaction.Description) || transaction.Description.Length > 120)
            throw new ArgumentException("Description must contain 1 to 120 characters.");
        ValidateAmount(transaction.Amount);
        if (!categories.Any(c => c.Id == transaction.CategoryId))
            throw new ArgumentException("Select an existing category.");
        if (transaction.Date.Year < 1900 || transaction.Date.Year > 2100)
            throw new ArgumentException("Date must be between 1900 and 2100.");
        if (!Enum.IsDefined(transaction.Type)) throw new ArgumentException("Unknown transaction type.");
    }

    public static void ValidateDocument(TrackerData data)
    {
        if (data.SchemaVersion != 1) throw new InvalidDataException("Unsupported data file version.");
        if (data.Categories is null || data.Transactions is null || data.Categories.Count == 0)
            throw new InvalidDataException("The data file requires categories and a transaction list.");
        if (data.Categories.Any(c => c is null) || data.Transactions.Any(t => t is null))
            throw new InvalidDataException("The data file contains null records.");
        if (data.Categories.Select(c => c.Id).Distinct().Count() != data.Categories.Count ||
            data.Transactions.Select(t => t.Id).Distinct().Count() != data.Transactions.Count)
            throw new InvalidDataException("The data file contains duplicate identifiers.");
        try
        {
            foreach (var category in data.Categories)
            {
                if (category.Id == Guid.Empty || string.IsNullOrWhiteSpace(category.Name) ||
                    category.Name.Length > 60 || category.ColourHex is null ||
                    !System.Text.RegularExpressions.Regex.IsMatch(category.ColourHex, "^#[0-9A-Fa-f]{6}$"))
                    throw new ArgumentException("Invalid category data.");
                ValidateAmount(category.MonthlyBudget, allowZero: true);
            }
            foreach (var transaction in data.Transactions) Validate(transaction, data.Categories);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("The data file contains invalid values.", ex);
        }
    }
}

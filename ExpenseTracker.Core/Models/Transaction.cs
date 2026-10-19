namespace ExpenseTracker.Core.Models;

/// <summary>Distinguishes money received from money spent; stored as a JSON name.</summary>
public enum TransactionType { Income, Expense }

/// <summary>An immutable transaction. Editing creates a replacement with the same Id.</summary>
public sealed record Transaction(
    Guid Id, string Description, decimal Amount, Guid CategoryId,
    DateOnly Date, TransactionType Type);

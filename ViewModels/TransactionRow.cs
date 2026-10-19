using ExpenseTracker.Core.Models;

namespace Expence_Tracker_ECU.ViewModels;

/// <summary>Presentation values are derived; currency formatting never changes stored amounts.</summary>
public sealed record TransactionRow(Transaction Transaction, string CategoryName)
{
    public string Description => Transaction.Description;
    public string DateText => Transaction.Date.ToString("yyyy-MM-dd");
    public string AmountText => $"LKR {Transaction.Amount:N2}";
    public string TypeText => Transaction.Type.ToString();
}

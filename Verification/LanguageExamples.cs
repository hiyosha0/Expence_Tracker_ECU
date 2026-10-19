using ExpenseTracker.Core.Models;
using Expence_Tracker_ECU.ViewModels;

namespace Verification;

/// <summary>Small executable illustrations for the report; not extra application features.</summary>
internal static class LanguageExamples
{
    public static void ChangeCopy(decimal amount) => amount = 99m;
    public static void ChangeCaller(ref decimal amount) => amount = 99m;
    public static Category ChangeReferenceCopy(Category category)
        => category with { MonthlyBudget = 99m };

    public static decimal ExpenseTotal(IEnumerable<Transaction> transactions)
        => transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

    // The declaration cannot communicate a checked exception effect to the C# compiler.
    public static string ReadText(string path) => File.ReadAllText(path);
}

internal sealed class NotificationExample : ViewModelBase
{
    private string _description = "";
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
}

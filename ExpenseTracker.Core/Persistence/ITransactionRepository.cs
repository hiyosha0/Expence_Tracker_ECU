using ExpenseTracker.Core.Models;

namespace ExpenseTracker.Core.Persistence;

/// <summary>Storage contract. I/O, JSON and invalid data exceptions propagate to the caller.</summary>
public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default);
}

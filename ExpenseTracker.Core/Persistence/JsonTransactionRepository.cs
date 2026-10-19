using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseTracker.Core.Models;

namespace ExpenseTracker.Core.Persistence;

/// <summary>
/// Serialises operations on one repository instance and replaces JSON only after a successful write.
/// This is single-user storage, not a database or a cross-process locking scheme.
/// </summary>
public sealed class JsonTransactionRepository : ITransactionRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    public string FilePath { get; }

    public JsonTransactionRepository(string filePath) => FilePath = Path.GetFullPath(filePath);

    public Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken cancellationToken = default)
        => WithDataAsync<IReadOnlyList<Transaction>>(data => data.Transactions.ToArray(), false, cancellationToken);

    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => WithDataAsync<IReadOnlyList<Category>>(data => data.Categories.ToArray(), false, cancellationToken);

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        => await WithDataAsync(data =>
        {
            DataRules.Validate(transaction, data.Categories);
            if (data.Transactions.Any(t => t.Id == transaction.Id))
                throw new ArgumentException("This transaction Id already exists.");
            data.Transactions.Add(transaction);
            return true;
        }, true, cancellationToken).ConfigureAwait(false);

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
        => await WithDataAsync(data =>
        {
            DataRules.Validate(transaction, data.Categories);
            int index = data.Transactions.FindIndex(t => t.Id == transaction.Id);
            if (index < 0) throw new KeyNotFoundException("The selected transaction no longer exists.");
            data.Transactions[index] = transaction;
            return true;
        }, true, cancellationToken).ConfigureAwait(false);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => await WithDataAsync(data =>
        {
            if (data.Transactions.RemoveAll(t => t.Id == id) == 0)
                throw new KeyNotFoundException("The selected transaction no longer exists.");
            return true;
        }, true, cancellationToken).ConfigureAwait(false);

    public async Task UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
        => await WithDataAsync(data =>
        {
            ArgumentNullException.ThrowIfNull(category);
            int index = data.Categories.FindIndex(c => c.Id == category.Id);
            if (index < 0) throw new KeyNotFoundException("The category no longer exists.");
            data.Categories[index] = category;
            DataRules.ValidateDocument(data);
            return true;
        }, true, cancellationToken).ConfigureAwait(false);

    private async Task<T> WithDataAsync<T>(Func<TrackerData, T> action, bool write,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await ReadCoreAsync(cancellationToken).ConfigureAwait(false);
            T result = action(data);
            if (write) await WriteCoreAsync(data, cancellationToken).ConfigureAwait(false);
            return result;
        }
        finally { _gate.Release(); }
    }

    private async Task<TrackerData> ReadCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FilePath)) return TrackerData.CreateDefault();
        await using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        var data = await JsonSerializer.DeserializeAsync<TrackerData>(stream, s_jsonOptions,
            cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The data file contains no document.");
        DataRules.ValidateDocument(data);
        return data;
    }

    private async Task WriteCoreAsync(TrackerData data, CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, data, s_jsonOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            // Keep the committed file intact; a failed operation never substitutes empty data.
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}

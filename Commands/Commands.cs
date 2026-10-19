using System.Windows.Input;

namespace Expence_Tracker_ECU.Commands;

/// <summary>An ordinary command for synchronous view actions.</summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) { if (CanExecute(parameter)) execute(); }
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// ICommand requires void Execute. The async boundary always observes errors and prevents re-entry.
/// ExecuteAsync lets the same command be verified without a GUI event loop.
/// </summary>
public sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute,
    Action<Exception> onError) : ICommand
{
    private bool _executing;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_executing && canExecute();
    public async void Execute(object? parameter) => await ExecuteAsync();
    public async Task ExecuteAsync()
    {
        if (!CanExecute(null)) return;
        _executing = true;
        NotifyCanExecuteChanged();
        try { await execute(); }
        catch (Exception ex) { onError(ex); }
        finally { _executing = false; NotifyCanExecuteChanged(); }
    }
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

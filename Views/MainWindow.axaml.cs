using Avalonia.Controls;
using Expence_Tracker_ECU.ViewModels;

namespace Expence_Tracker_ECU.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) =>
        {
            if (DataContext is MainViewModel model) await model.ReloadCommand.ExecuteAsync();
        };
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace Expence_Tracker_ECU.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";
}

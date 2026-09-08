using CommunityToolkit.Mvvm.ComponentModel;

namespace Basora.UI.ViewModels;

/// <summary>
/// Placeholder view model for the scaffold window. Replaced by the real application
/// shell at T-U03.
/// </summary>
public sealed partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}

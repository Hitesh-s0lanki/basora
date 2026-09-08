using CommunityToolkit.Mvvm.ComponentModel;

namespace Basora.UI.ViewModels;

/// <summary>
/// Base class for every view model in the application.
/// </summary>
/// <remarks>
/// Unsealed by design: <see cref="ViewLocator"/> uses derivation from this type as the
/// signal that an object is a view model it should resolve a view for.
/// </remarks>
public abstract class ViewModelBase : ObservableObject;

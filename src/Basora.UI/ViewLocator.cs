using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Basora.UI.ViewModels;

namespace Basora.UI;

/// <summary>
/// Resolves a view for a view model by convention: <c>Basora.UI.ViewModels.FooViewModel</c>
/// maps to <c>Basora.UI.Views.FooView</c>.
/// </summary>
/// <remarks>
/// This lives in Basora.UI rather than Basora.App because <see cref="Type.GetType(string)"/>
/// searches the calling assembly, and every view it resolves is in this one.
/// </remarks>
public sealed class ViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        string? name = param.GetType().FullName?.Replace("ViewModel", "View", StringComparison.Ordinal);
        Type? type = name is null ? null : Type.GetType(name);

        return type is null
            ? new TextBlock { Text = $"Not Found: {name}" }
            : (Control)Activator.CreateInstance(type)!;
    }

    /// <inheritdoc />
    public bool Match(object? data) => data is ViewModelBase;
}

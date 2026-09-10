using Avalonia.Styling;

namespace Basora.UI.Themes;

/// <summary>
/// The theme variants Basora ships beyond Avalonia's own light and dark.
/// </summary>
public static class BasoraThemeVariants
{
    /// <summary>
    /// The high contrast variant from docs/07-design-system.md section 11.
    /// </summary>
    /// <remarks>
    /// Declared as inheriting from <see cref="ThemeVariant.Dark"/>, which is what any
    /// resource Basora has not overridden falls back to. Inheriting from dark rather than
    /// light matters: this variant is black-on-white inverted, so a stray built-in
    /// Avalonia brush resolved against the light variant would come out as dark text on a
    /// black surface.
    /// </remarks>
    public static ThemeVariant HighContrast { get; } = new(nameof(HighContrast), ThemeVariant.Dark);
}

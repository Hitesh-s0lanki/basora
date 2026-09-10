using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Basora.UI.Themes;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// docs/07-design-system.md section 12: a theme change repaints in place, with no
/// restart.
/// </summary>
/// <remarks>
/// What makes that true is where the tokens live. Semantic tokens sit in
/// ThemeDictionaries, so switching the variant swaps the dictionary and every
/// DynamicResource binding re-resolves against the new one. These tests resolve the real
/// dictionary the application loads and check it answers differently per variant, which
/// is the mechanism itself rather than a proxy for it.
/// </remarks>
public sealed class ThemeSwitchingTests
{
    private static readonly ResourceDictionary Theme = Load();

    public static TheoryData<string> Variants => ["Light", "Dark", "HighContrast"];

    [Theory]
    [MemberData(nameof(Variants))]
    public void EveryVariant_ResolvesTheSemanticTokens(string variantName)
    {
        ThemeVariant variant = Variant(variantName);
        List<string> unresolved = [];

        foreach (string token in ThemeTokenTable.All[0].Brushes.Keys)
        {
            if (!Theme.TryGetResource(token, variant, out object? value) || value is not IBrush)
            {
                unresolved.Add(token);
            }
        }

        Assert.Empty(unresolved);
    }

    [Fact]
    public void SwitchingVariant_ChangesWhatTheSameTokenResolvesTo()
    {
        // The whole point. One key, three answers, chosen at resolution time.
        Color light = Resolve("surface.background", ThemeVariant.Light);
        Color dark = Resolve("surface.background", ThemeVariant.Dark);
        Color highContrast = Resolve("surface.background", BasoraThemeVariants.HighContrast);

        Assert.NotEqual(light, dark);
        Assert.NotEqual(dark, highContrast);
        Assert.NotEqual(light, highContrast);
    }

    [Fact]
    public void TheHighContrastVariant_FallsBackToDarkRatherThanLight()
    {
        // Any Avalonia built-in Basora has not overridden resolves through the inherit
        // variant. Inheriting from light here would put dark text on a black surface.
        Assert.Equal(ThemeVariant.Dark, BasoraThemeVariants.HighContrast.InheritVariant);
        Assert.Equal("HighContrast", BasoraThemeVariants.HighContrast.Key);
    }

    [Fact]
    public void TokensThatDoNotDependOnTheTheme_ResolveWithoutOne()
    {
        // Spacing, radius and the type scale are merged rather than themed, so they
        // survive a variant switch untouched instead of being redefined three times.
        Assert.True(Theme.TryGetResource("space.4", null, out object? spacing));
        Assert.Equal(16d, spacing);

        Assert.True(Theme.TryGetResource("type.body.size", null, out object? body));
        Assert.Equal(13d, body);

        Assert.True(Theme.TryGetResource("density.row.height", null, out object? row));
        Assert.Equal(26d, row);
    }

    private static Color Resolve(string token, ThemeVariant variant)
    {
        Assert.True(Theme.TryGetResource(token, variant, out object? value), $"'{token}' did not resolve.");
        return Assert.IsType<SolidColorBrush>(value).Color;
    }

    private static ThemeVariant Variant(string name) => name switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        "HighContrast" => BasoraThemeVariants.HighContrast,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static ResourceDictionary Load()
    {
        HeadlessAvalonia.EnsureStarted();

        return (ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Basora.UI/Themes/BasoraTheme.axaml"));
    }
}

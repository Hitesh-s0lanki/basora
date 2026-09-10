using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// docs/07-design-system.md section 7: three densities, and one token that every list,
/// tree and grid reads.
/// </summary>
public sealed class DensityTests
{
    private static readonly string[] DensityKeys =
    [
        "density.row.height",
        "density.tree.row.height",
        "density.toolbar.height",
        "density.control.height",
    ];

    public static TheoryData<string, double, double, double, double> DocumentedTable => new()
    {
        // Mode, grid row, tree row, toolbar, control — the table from section 7.
        { "Compact", 22, 22, 32, 26 },
        { "Default", 26, 26, 36, 30 },
        { "Comfortable", 32, 30, 42, 34 },
    };

    [Theory]
    [MemberData(nameof(DocumentedTable))]
    public void EachDensity_MatchesTheDocumentedTable(
        string mode,
        double gridRow,
        double treeRow,
        double toolbar,
        double control)
    {
        ResourceDictionary density = Load(mode);

        Assert.Equal(gridRow, Value(density, "density.row.height"));
        Assert.Equal(treeRow, Value(density, "density.tree.row.height"));
        Assert.Equal(toolbar, Value(density, "density.toolbar.height"));
        Assert.Equal(control, Value(density, "density.control.height"));
    }

    [Fact]
    public void EveryDensity_DefinesTheSameTokens()
    {
        // Swapping to a density that is missing a key leaves the previous one's value in
        // place, so the app would half-switch.
        foreach (string mode in new[] { "Compact", "Default", "Comfortable" })
        {
            ResourceDictionary density = Load(mode);

            Assert.Equal(DensityKeys.Length, density.Count);
            Assert.All(DensityKeys, key => Assert.True(density.ContainsKey(key), $"{mode} lacks '{key}'."));
        }
    }

    [Fact]
    public void SwappingTheDensity_ChangesEveryHeightThroughItsToken()
    {
        ResourceDictionary compact = Load("Compact");
        ResourceDictionary comfortable = Load("Comfortable");

        foreach (string key in DensityKeys)
        {
            Assert.True(
                Value(comfortable, key) > Value(compact, key),
                $"'{key}' does not grow from Compact to Comfortable.");
        }
    }

    [Fact]
    public void TheRowHeightToken_IsSharedByListsTreesAndGrids()
    {
        // The document asks for one row-height token. The tree carries a second only
        // because the Comfortable row differs, at 30 against the grid's 32; at the other
        // two densities they are deliberately equal.
        Assert.Equal(Value(Load("Compact"), "density.row.height"), Value(Load("Compact"), "density.tree.row.height"));
        Assert.Equal(Value(Load("Default"), "density.row.height"), Value(Load("Default"), "density.tree.row.height"));
        Assert.NotEqual(Value(Load("Comfortable"), "density.row.height"), Value(Load("Comfortable"), "density.tree.row.height"));
    }

    private static double Value(ResourceDictionary dictionary, string key)
    {
        Assert.True(dictionary.TryGetResource(key, null, out object? value), $"'{key}' did not resolve.");
        return Assert.IsType<double>(value);
    }

    private static ResourceDictionary Load(string mode)
    {
        HeadlessAvalonia.EnsureStarted();

        return (ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri($"avares://Basora.UI/Themes/Density.{mode}.axaml"));
    }
}

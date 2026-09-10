using System.Text.RegularExpressions;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// docs/07-design-system.md section 2: the UI never references a primitive directly.
/// </summary>
/// <remarks>
/// Three layers exist so that a colour can be retuned in one place. A screen that reaches
/// past the semantic layer to <c>gray.500</c> is a screen that will not follow when
/// <c>text.tertiary</c> is changed, and worse, one that keeps its light-theme grey in the
/// dark theme.
/// <para>
/// Scoped to colour, which is this task's acceptance criterion. The wider rule, that no
/// view states a literal size or margin either, arrives with T-U02.
/// </para>
/// </remarks>
public sealed partial class PrimitiveLeakTests
{
    /// <summary>Every key any theme declares as a primitive.</summary>
    private static readonly HashSet<string> Primitives =
        [.. ThemeTokenTable.All.SelectMany(theme => theme.Primitives.Keys)];

    [Fact]
    public void NoMarkupOutsideTheThemeDictionaries_ReferencesAPrimitive()
    {
        List<string> leaks = [];

        foreach (string path in ThemeFiles.AllViewMarkup())
        {
            foreach (Match match in ResourceReference().Matches(File.ReadAllText(path)))
            {
                string key = match.Groups["key"].Value;

                if (Primitives.Contains(key))
                {
                    leaks.Add($"{Path.GetFileName(path)} references the primitive '{key}'.");
                }
            }
        }

        Assert.Empty(leaks);
    }

    [Fact]
    public void NoMarkupOutsideTheThemeDictionaries_StatesALiteralColour()
    {
        List<string> literals = [];

        foreach (string path in ThemeFiles.AllViewMarkup())
        {
            foreach (Match match in LiteralColour().Matches(File.ReadAllText(path)))
            {
                literals.Add($"{Path.GetFileName(path)} states the literal colour {match.Value}.");
            }
        }

        Assert.Empty(literals);
    }

    [Fact]
    public void TheScan_ActuallyReachesTheMarkupItClaimsTo()
    {
        // Both tests above pass trivially over an empty file list, and the list is built
        // by walking the source tree. This is what stops that silence counting as a pass.
        IReadOnlyList<string> scanned = ThemeFiles.AllViewMarkup();

        Assert.NotEmpty(scanned);
        Assert.Contains(scanned, path => Path.GetFileName(path) == "MainWindow.axaml");
        Assert.Contains(scanned, path => Path.GetFileName(path) == "App.axaml");
        Assert.DoesNotContain(scanned, path => Path.GetFileName(path) == "Light.axaml");
    }

    [Fact]
    public void TheRules_RejectTheThingsTheyExistToCatch()
    {
        Assert.Contains("gray.500", Primitives);
        Assert.Matches(ResourceReference(), "Background=\"{DynamicResource gray.500}\"");
        Assert.Equal("gray.500", ResourceReference().Match("{StaticResource gray.500}").Groups["key"].Value);

        Assert.Matches(LiteralColour(), "Foreground=\"#FF0000\"");
        Assert.Matches(LiteralColour(), "Background=\"#80101828\"");
        Assert.DoesNotMatch(LiteralColour(), "Margin=\"8\"");
    }

    [GeneratedRegex(@"\{(?:Static|Dynamic)Resource\s+(?<key>[^\s}]+)\s*\}", RegexOptions.CultureInvariant)]
    private static partial Regex ResourceReference();

    [GeneratedRegex("\"#(?:[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\"", RegexOptions.CultureInvariant)]
    private static partial Regex LiteralColour();
}

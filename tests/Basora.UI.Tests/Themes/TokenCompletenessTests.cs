namespace Basora.UI.Tests.Themes;

/// <summary>
/// docs/07-design-system.md section 12: every semantic token is defined in every theme.
/// </summary>
/// <remarks>
/// A token missing from one variant does not fail at build time and does not throw at
/// runtime. Avalonia resolves it to a built-in Fluent brush, so the screen renders in a
/// colour nobody chose, in one theme only, and it is found by a user. Hence a test.
/// <para>
/// This is the narrower half of the rule: that the three dictionaries agree with each
/// other. The wider half, that every token any XAML references exists at all, arrives
/// with T-U02 once there is markup to scan.
/// </para>
/// </remarks>
public sealed class TokenCompletenessTests
{
    [Fact]
    public void EveryTheme_DefinesExactlyTheSameSemanticTokens()
    {
        ThemeTokenTable reference = ThemeTokenTable.All[0];
        List<string> problems = [];

        foreach (ThemeTokenTable theme in ThemeTokenTable.All.Skip(1))
        {
            foreach (string missing in reference.SemanticKeys.Except(theme.SemanticKeys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                problems.Add($"{theme.Name} is missing '{missing}', which {reference.Name} defines.");
            }

            foreach (string extra in theme.SemanticKeys.Except(reference.SemanticKeys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                problems.Add($"{theme.Name} defines '{extra}', which {reference.Name} does not.");
            }
        }

        Assert.Empty(problems);
    }

    [Fact]
    public void AllThreeThemes_AreActuallyLoaded()
    {
        // Guards the test above from passing because nothing was read.
        Assert.Equal(3, ThemeTokenTable.All.Count);
        Assert.Equal(["Light", "Dark", "HighContrast"], ThemeTokenTable.All.Select(t => t.Name));
        Assert.All(ThemeTokenTable.All, theme => Assert.True(theme.SemanticKeys.Count > 50));
    }

    [Theory]
    [InlineData("surface.background")]
    [InlineData("surface.panel")]
    [InlineData("surface.raised")]
    [InlineData("surface.overlay")]
    [InlineData("surface.inset")]
    [InlineData("border.subtle")]
    [InlineData("border.default")]
    [InlineData("border.strong")]
    [InlineData("border.focus")]
    [InlineData("text.primary")]
    [InlineData("text.secondary")]
    [InlineData("text.tertiary")]
    [InlineData("text.disabled")]
    [InlineData("text.inverse")]
    [InlineData("text.null")]
    [InlineData("status.success")]
    [InlineData("status.warning")]
    [InlineData("status.danger")]
    [InlineData("status.info")]
    [InlineData("status.ai")]
    [InlineData("env.local")]
    [InlineData("env.dev")]
    [InlineData("env.staging")]
    [InlineData("env.production")]
    [InlineData("data.modified")]
    [InlineData("data.inserted")]
    [InlineData("data.deleted")]
    [InlineData("data.invalid")]
    [InlineData("data.selection")]
    [InlineData("data.match")]
    [InlineData("elevation.0")]
    [InlineData("elevation.1")]
    [InlineData("elevation.2")]
    [InlineData("elevation.3")]
    public void EveryTokenTheDocumentNames_Exists(string token)
    {
        // The names in docs/07-design-system.md section 4 are the contract other UI tasks
        // consume. Renaming one here would compile and would silently repaint screens.
        foreach (ThemeTokenTable theme in ThemeTokenTable.All)
        {
            Assert.True(
                theme.SemanticKeys.Contains(token),
                $"{theme.Name} has no token named '{token}'.");
        }
    }

    [Fact]
    public void EverySemanticToken_TakesItsColourFromAPrimitive()
    {
        // Enforced during parsing: a brush that states a literal hex, or points at
        // something the file does not define, fails to load. This pins the rule itself.
        Assert.All(
            ThemeTokenTable.All,
            theme => Assert.NotEmpty(theme.Primitives));
    }
}

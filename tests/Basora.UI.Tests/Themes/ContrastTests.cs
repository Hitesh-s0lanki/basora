using System.Globalization;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// docs/07-design-system.md section 3: all text meets WCAG AA, asserted over the token
/// table rather than by eye.
/// </summary>
/// <remarks>
/// The pairs are stated here as data because the requirement is about combinations, not
/// about colours. A token can be perfectly readable on the app background and illegible
/// on a badge fill, and only naming the pair catches that.
/// </remarks>
public sealed class ContrastTests
{
    /// <summary>
    /// The surfaces text is drawn on. A text token has to clear the floor on all of them,
    /// since a label moves between a panel and a dialog without changing token.
    /// </summary>
    private static readonly string[] Surfaces =
        ["surface.background", "surface.panel", "surface.raised", "surface.inset"];

    private static readonly string[] StatusFamilies =
        ["status.success", "status.warning", "status.danger", "status.info", "status.ai"];

    private static readonly string[] EnvironmentFamilies =
        ["env.local", "env.dev", "env.staging", "env.production"];

    private static readonly string[] DataStates =
        ["data.modified", "data.inserted", "data.deleted", "data.invalid"];

    public static TheoryData<string> Themes => [.. ThemeTokenTable.All.Select(t => t.Name)];

    [Theory]
    [MemberData(nameof(Themes))]
    public void BodyText_ClearsTheAaFloorOnEverySurface(string themeName)
    {
        ThemeTokenTable theme = Theme(themeName);
        List<string> failures = [];

        foreach (string text in new[] { "text.primary", "text.secondary", "text.tertiary", "text.null" })
        {
            foreach (string surface in Surfaces)
            {
                Check(theme, text, surface, Wcag.BodyTextMinimum, failures);
            }
        }

        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TextOnAnAccentFill_ClearsTheAaFloor(string themeName)
    {
        ThemeTokenTable theme = Theme(themeName);
        List<string> failures = [];

        Check(theme, "text.inverse", "accent.default", Wcag.BodyTextMinimum, failures);
        Check(theme, "text.inverse", "accent.hover", Wcag.BodyTextMinimum, failures);
        Check(theme, "text.inverse", "accent.pressed", Wcag.BodyTextMinimum, failures);

        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void StatusAndEnvironmentText_ClearsTheAaFloorOnItsOwnBadgeAndOnTheApp(string themeName)
    {
        ThemeTokenTable theme = Theme(themeName);
        List<string> failures = [];

        foreach (string family in StatusFamilies.Concat(EnvironmentFamilies))
        {
            // The badge fill is transparent in the high contrast theme, so the text sits
            // on whatever is behind it. Compositing handles that without a special case.
            Check(theme, family + ".text", family + ".background", Wcag.BodyTextMinimum, failures,
                behind: "surface.panel");
            Check(theme, family + ".text", "surface.background", Wcag.BodyTextMinimum, failures);
        }

        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void IndicatorColours_ClearTheGlyphFloor(string themeName)
    {
        // Icons, indicator bars and the environment chrome are graphical objects, so the
        // 1.4.11 floor of 3:1 applies rather than the text floor.
        ThemeTokenTable theme = Theme(themeName);
        List<string> failures = [];

        foreach (string token in StatusFamilies.Concat(EnvironmentFamilies).Concat(DataStates).Append("accent.default"))
        {
            Check(theme, token, "surface.background", Wcag.LargeTextAndGlyphMinimum, failures);
            Check(theme, token, "surface.panel", Wcag.LargeTextAndGlyphMinimum, failures);
        }

        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void TheFocusRing_IsVisibleAgainstEverySurface(string themeName)
    {
        // The focus ring is the one thing a keyboard user cannot work without, and it is
        // drawn over every surface in the app.
        ThemeTokenTable theme = Theme(themeName);
        List<string> failures = [];

        foreach (string surface in Surfaces)
        {
            Check(theme, "border.focus", surface, Wcag.LargeTextAndGlyphMinimum, failures);
        }

        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void CellTextStaysReadable_OnEveryDataStateTint(string themeName)
    {
        // A modified, inserted, deleted, selected or matched cell still shows its value.
        // Tinting the row must not cost the value its legibility.
        ThemeTokenTable theme = Theme(themeName);
        List<string> failures = [];

        string[] tints =
        [
            "data.modified.background",
            "data.inserted.background",
            "data.deleted.background",
            "data.invalid.background",
            "data.selection",
            "data.match",
        ];

        foreach (string tint in tints)
        {
            Check(theme, "text.primary", tint, Wcag.BodyTextMinimum, failures, behind: "surface.background");
            Check(theme, "text.null", tint, Wcag.BodyTextMinimum, failures, behind: "surface.background");
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void DisabledText_IsTheOnlyTextTokenExemptFromTheFloor()
    {
        // WCAG 1.4.3 excludes inactive controls. Stated as a test so the exemption is a
        // decision on the record rather than a token quietly left out of the table above.
        ThemeTokenTable light = Theme("Light");

        double ratio = Ratio(light, "text.disabled", "surface.background", behind: null);

        Assert.True(ratio < Wcag.BodyTextMinimum);
        Assert.True(ratio > 2.0, "Disabled text is exempt from the floor, not licence to make it invisible.");
    }

    [Fact]
    public void TheContrastRule_RejectsAPairThatFails()
    {
        // Without this the suite would pass just as happily if the maths returned 21 for
        // everything.
        Rgba lightGrey = Rgba.Parse("#CCCCCC");
        Rgba white = Rgba.Parse("#FFFFFF");
        Rgba black = Rgba.Parse("#000000");

        Assert.True(Wcag.ContrastRatio(lightGrey, white) < Wcag.BodyTextMinimum);
        Assert.True(Wcag.ContrastRatio(black, white) > 20.9);
        Assert.Equal(1.0, Wcag.ContrastRatio(white, white), 3);
    }

    [Fact]
    public void Compositing_AccountsForOpacityRatherThanIgnoringIt()
    {
        // data.selection and text.null are both defined with an opacity. A test that read
        // their colour and skipped the alpha would report a ratio nobody ever sees.
        Rgba halfBlack = Rgba.Parse("#000000").WithAlpha(0.5);
        Rgba composited = halfBlack.Over(Rgba.Parse("#FFFFFF"));

        Assert.Equal("#808080", composited.ToHex());
        Assert.Equal(1.0, composited.Alpha);
    }

    private static ThemeTokenTable Theme(string name) =>
        ThemeTokenTable.All.Single(t => t.Name == name);

    private static void Check(
        ThemeTokenTable theme,
        string foreground,
        string background,
        double minimum,
        List<string> failures,
        string? behind = null)
    {
        double ratio = Ratio(theme, foreground, background, behind);

        if (ratio < minimum)
        {
            failures.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{theme.Name}: '{foreground}' on '{background}' is {ratio:F2}:1, below {minimum:F1}:1."));
        }
    }

    private static double Ratio(ThemeTokenTable theme, string foreground, string background, string? behind)
    {
        Rgba surface = theme.Brush(background);

        if (surface.Alpha < 1.0)
        {
            surface = surface.Over(theme.Brush(behind ?? "surface.background"));
        }

        return Wcag.ContrastRatio(theme.Brush(foreground).Over(surface), surface);
    }
}

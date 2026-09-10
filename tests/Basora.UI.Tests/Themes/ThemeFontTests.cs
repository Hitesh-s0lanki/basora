using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// docs/07-design-system.md section 5: the code and data face is monospaced, and it is
/// bundled rather than assumed.
/// </summary>
/// <remarks>
/// Alignment is the entire reason the design specifies a monospace face for cell values.
/// A proportional fallback puts a column of numbers out of line, and that is the one
/// thing a person reading a result set cannot work around. So these tests resolve the
/// face the way the renderer does and measure it, rather than checking that a string in
/// the token happens to say JetBrains Mono.
/// </remarks>
public sealed class ThemeFontTests
{
    private static readonly ResourceDictionary Theme = Load();

    [Fact]
    public void TheCodeFace_ResolvesToTheBundledJetBrainsMono()
    {
        Assert.Equal("JetBrains Mono", Resolve("font.code").FamilyName);
    }

    [Fact]
    public void TheCodeFace_IsActuallyMonospaced()
    {
        // The property that matters, measured. A fallback would fail this even if the
        // family name somehow matched.
        GlyphTypeface face = Resolve("font.code");

        ushort[] widths = [.. "iWl0m. ".Select(character => Advance(face, character))];

        Assert.All(widths, width => Assert.Equal(widths[0], width));
        Assert.True(widths[0] > 0);
    }

    [Fact]
    public void TheUiFace_ResolvesToTheBundledInter()
    {
        Assert.Equal("Inter", Resolve("font.ui").FamilyName);
    }

    [Fact]
    public void TheUiFace_IsProportional()
    {
        // Guards the test above from passing against a monospace fallback, and pins the
        // distinction the two tokens exist to draw.
        GlyphTypeface face = Resolve("font.ui");

        Assert.True(Advance(face, 'W') > Advance(face, 'i'));
    }

    [Fact]
    public void EveryWeightAndSlantTheDesignUses_IsARealFaceRatherThanASimulatedOne()
    {
        // Regular carries body code, bold carries SQL keyword highlighting, and the
        // italics carry the NULL badge. A missing face is synthesised by slanting or
        // smearing the regular one, which for a monospace face distorts the widths that
        // are the whole reason it was chosen.
        (FontWeight Weight, FontStyle Style)[] wanted =
        [
            (FontWeight.Normal, FontStyle.Normal),
            (FontWeight.Normal, FontStyle.Italic),
            (FontWeight.Bold, FontStyle.Normal),
            (FontWeight.Bold, FontStyle.Italic),
        ];

        foreach ((FontWeight weight, FontStyle style) in wanted)
        {
            Typeface typeface = new(CodeFamily(), style, weight);

            Assert.True(
                FontManager.Current.TryGetGlyphTypeface(typeface, out GlyphTypeface? face),
                $"JetBrains Mono has no {weight} {style} face.");
            Assert.Equal("JetBrains Mono", face.FamilyName);
            Assert.Equal(FontSimulations.None, face.FontSimulations);
        }
    }

    [Fact]
    public void TheOpenFontLicence_ShipsWithTheFont()
    {
        // Redistributing an OFL face without its licence text is a licence breach, and it
        // is the kind of thing that goes unnoticed until someone audits a release.
        string directory = FontDirectory();
        string licence = File.ReadAllText(Path.Combine(directory, "OFL.txt"));

        Assert.Contains("SIL Open Font License", licence, StringComparison.Ordinal);
        Assert.Contains("JetBrains Mono", licence, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(directory, "AUTHORS.txt")));
    }

    [Fact]
    public void OnlyTheFourFacesTheDesignUses_AreCarried()
    {
        // The upstream release ships eight weights with italics plus a no-ligature
        // variant. Carrying all of them is about ten megabytes in every installer for
        // faces nothing renders.
        string[] carried =
        [
            .. Directory.EnumerateFiles(FontDirectory(), "*.ttf")
                .Select(Path.GetFileName)
                .OfType<string>()
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(
            [
                "JetBrainsMono-Bold.ttf",
                "JetBrainsMono-BoldItalic.ttf",
                "JetBrainsMono-Italic.ttf",
                "JetBrainsMono-Regular.ttf",
            ],
            carried);
    }

    private static string FontDirectory() => Path.Combine(
        Path.GetDirectoryName(ThemeFiles.Directory)!,
        "Assets",
        "Fonts",
        "JetBrainsMono");

    private static ushort Advance(GlyphTypeface face, char character)
    {
        Assert.True(
            face.CharacterToGlyphMap.TryGetGlyph(character, out ushort glyph),
            $"{face.FamilyName} has no glyph for '{character}'.");
        Assert.True(
            face.TryGetHorizontalGlyphAdvance(glyph, out ushort advance),
            $"{face.FamilyName} reports no advance for '{character}'.");

        return advance;
    }

    private static FontFamily CodeFamily()
    {
        Assert.True(Theme.TryGetResource("font.code", null, out object? value));
        return Assert.IsType<FontFamily>(value);
    }

    private static GlyphTypeface Resolve(string token)
    {
        Assert.True(Theme.TryGetResource(token, null, out object? value), $"'{token}' did not resolve.");
        FontFamily family = Assert.IsType<FontFamily>(value);

        Assert.True(
            FontManager.Current.TryGetGlyphTypeface(new Typeface(family), out GlyphTypeface? face),
            $"'{token}' resolved to no usable face.");

        return face;
    }

    private static ResourceDictionary Load()
    {
        HeadlessAvalonia.EnsureStarted();

        return (ResourceDictionary)AvaloniaXamlLoader.Load(
            new Uri("avares://Basora.UI/Themes/BasoraTheme.axaml"));
    }
}

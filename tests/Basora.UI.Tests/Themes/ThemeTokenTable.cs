using System.Globalization;
using System.Xml.Linq;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// The tokens one theme dictionary declares, read out of the AXAML.
/// </summary>
/// <remarks>
/// Read from the file rather than from the loaded application. A running Avalonia
/// instance would resolve a missing token by falling back to a built-in Fluent brush,
/// which is exactly the failure these tests exist to catch: the token would look present
/// and paint the wrong colour. The file cannot fall back.
/// </remarks>
internal sealed class ThemeTokenTable
{
    private static readonly XNamespace Avalonia = "https://github.com/avaloniaui";
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    private ThemeTokenTable(
        string name,
        IReadOnlyDictionary<string, Rgba> primitives,
        IReadOnlyDictionary<string, Rgba> brushes,
        IReadOnlyList<string> nonColourKeys)
    {
        Name = name;
        Primitives = primitives;
        Brushes = brushes;
        NonColourKeys = nonColourKeys;
    }

    /// <summary>The variant's name, for failure messages.</summary>
    public string Name { get; }

    /// <summary>The <c>Color</c> entries: the primitive layer.</summary>
    public IReadOnlyDictionary<string, Rgba> Primitives { get; }

    /// <summary>The <c>SolidColorBrush</c> entries with their opacity already folded in.</summary>
    public IReadOnlyDictionary<string, Rgba> Brushes { get; }

    /// <summary>Everything else keyed in the file, such as the elevation shadows.</summary>
    public IReadOnlyList<string> NonColourKeys { get; }

    /// <summary>Every semantic token name, which must match across all three themes.</summary>
    public IReadOnlyCollection<string> SemanticKeys => [.. Brushes.Keys, .. NonColourKeys];

    /// <summary>The three shipped variants.</summary>
    public static IReadOnlyList<ThemeTokenTable> All { get; } =
    [
        Load("Light"),
        Load("Dark"),
        Load("HighContrast"),
    ];

    /// <summary>Looks up a semantic token, failing the test rather than returning null.</summary>
    public Rgba Brush(string key)
    {
        Assert.True(Brushes.ContainsKey(key), $"{Name} has no token named '{key}'.");
        return Brushes[key];
    }

    private static ThemeTokenTable Load(string variant)
    {
        XDocument document = XDocument.Load(ThemeFiles.Path(variant + ".axaml"));

        Dictionary<string, Rgba> primitives = [];
        Dictionary<string, Rgba> brushes = [];
        List<string> others = [];

        foreach (XElement element in document.Root!.Elements())
        {
            string? key = element.Attribute(Xaml + "Key")?.Value;
            if (key is null)
            {
                continue;
            }

            if (element.Name == Avalonia + "Color")
            {
                primitives[key] = Rgba.Parse(element.Value.Trim());
            }
            else if (element.Name == Avalonia + "SolidColorBrush")
            {
                brushes[key] = ReadBrush(element, primitives, variant, key);
            }
            else
            {
                others.Add(key);
            }
        }

        return new ThemeTokenTable(variant, primitives, brushes, others);
    }

    private static Rgba ReadBrush(
        XElement element,
        Dictionary<string, Rgba> primitives,
        string variant,
        string key)
    {
        string colour = element.Attribute("Color")?.Value
            ?? throw new InvalidOperationException($"{variant}: brush '{key}' declares no Color.");

        Rgba value;

        if (colour.StartsWith('#'))
        {
            value = Rgba.Parse(colour);
        }
        else
        {
            string reference = ExtractResourceKey(colour, variant, key);
            Assert.True(
                primitives.ContainsKey(reference),
                $"{variant}: brush '{key}' points at primitive '{reference}', which the file does not "
                    + "define above it.");
            value = primitives[reference];
        }

        string? opacity = element.Attribute("Opacity")?.Value;
        return opacity is null
            ? value
            : value.WithAlpha(double.Parse(opacity, CultureInfo.InvariantCulture));
    }

    private static string ExtractResourceKey(string markup, string variant, string key)
    {
        // {StaticResource gray.500}
        int space = markup.IndexOf(' ', StringComparison.Ordinal);
        Assert.True(
            markup.StartsWith("{StaticResource ", StringComparison.Ordinal) && markup.EndsWith('}'),
            $"{variant}: brush '{key}' uses '{markup}'. A semantic token takes its colour from a "
                + "primitive in the same dictionary, so the whole theme can be retuned in one place.");

        return markup[(space + 1)..^1].Trim();
    }
}

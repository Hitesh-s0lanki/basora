using System.Globalization;

namespace Basora.UI.Tests.Themes;

/// <summary>An sRGB colour with straight alpha, in the 0-255 space the token files use.</summary>
internal readonly record struct Rgba(byte R, byte G, byte B, double Alpha)
{
    /// <summary>Parses <c>#RRGGBB</c> or <c>#AARRGGBB</c>.</summary>
    public static Rgba Parse(string hex)
    {
        string digits = hex.TrimStart('#');

        return digits.Length switch
        {
            6 => new Rgba(Byte(digits, 0), Byte(digits, 2), Byte(digits, 4), 1.0),
            8 => new Rgba(Byte(digits, 2), Byte(digits, 4), Byte(digits, 6), Byte(digits, 0) / 255.0),
            _ => throw new FormatException($"'{hex}' is not #RRGGBB or #AARRGGBB."),
        };

        static byte Byte(string text, int offset) =>
            byte.Parse(text.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    public Rgba WithAlpha(double alpha) => this with { Alpha = Alpha * alpha };

    /// <summary>Paints this colour over an opaque one, which is what the screen does.</summary>
    public Rgba Over(Rgba background)
    {
        if (Alpha >= 1.0)
        {
            return this;
        }

        double alpha = Alpha;

        return new Rgba(
            Mix(R, background.R),
            Mix(G, background.G),
            Mix(B, background.B),
            1.0);

        byte Mix(byte foreground, byte behind) =>
            (byte)Math.Round((foreground * alpha) + (behind * (1 - alpha)));
    }

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary>
/// The WCAG 2.1 contrast formulas, written from the specification.
/// </summary>
/// <remarks>
/// The point of asserting contrast rather than eyeballing it is that a token can be
/// retuned by someone who cannot see the result, and the ratio still has to hold. So the
/// maths lives here and the thresholds live with the pairs that use them.
/// </remarks>
internal static class Wcag
{
    /// <summary>Body text and anything below 18px, or below 14px bold.</summary>
    public const double BodyTextMinimum = 4.5;

    /// <summary>Large text, and the graphical objects and UI components floor from 1.4.11.</summary>
    public const double LargeTextAndGlyphMinimum = 3.0;

    /// <summary>Relative luminance, WCAG 2.1 definition.</summary>
    public static double RelativeLuminance(Rgba colour)
    {
        if (colour.Alpha < 1.0)
        {
            throw new ArgumentException(
                "Luminance is only defined for an opaque colour. Composite it over its background first.",
                nameof(colour));
        }

        return (0.2126 * Channel(colour.R)) + (0.7152 * Channel(colour.G)) + (0.0722 * Channel(colour.B));

        static double Channel(byte value)
        {
            double srgb = value / 255.0;
            return srgb <= 0.03928
                ? srgb / 12.92
                : Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }
    }

    /// <summary>The contrast ratio between two opaque colours, from 1 to 21.</summary>
    public static double ContrastRatio(Rgba first, Rgba second)
    {
        double a = RelativeLuminance(first);
        double b = RelativeLuminance(second);
        (double lighter, double darker) = a >= b ? (a, b) : (b, a);

        return (lighter + 0.05) / (darker + 0.05);
    }
}

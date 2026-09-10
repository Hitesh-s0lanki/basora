using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Basora.Core.Errors;

/// <summary>
/// What Basora knows about one PostgreSQL SQLSTATE: the stable code it maps to, the
/// sentence to show, and what the user can do about it.
/// </summary>
/// <param name="SqlState">The five-character SQLSTATE, or a two-character class prefix for a class fallback.</param>
/// <param name="Code">The stable code from <see cref="BasoraErrorCodes"/>.</param>
/// <param name="MessageTemplate">
/// The sentence to show, with named placeholders in braces — <c>{Role}</c>,
/// <c>{Object}</c>. Whoever maps the exception fills them, because only that layer knows
/// the statement, the connection and the metadata cache. Nothing here formats.
/// </param>
/// <param name="Remediation">What would fix it, in the same placeholder syntax.</param>
public sealed partial record SqlStateDescriptor(
    string SqlState,
    string Code,
    string MessageTemplate,
    string Remediation)
{
    /// <summary>
    /// The distinct placeholder names used across <see cref="MessageTemplate"/> and
    /// <see cref="Remediation"/>, in the order they first appear. What the mapping layer
    /// has to supply for this descriptor to render.
    /// </summary>
    public ImmutableArray<string> Placeholders { get; } =
    [
        .. PlaceholderPattern()
            .Matches(MessageTemplate + " " + Remediation)
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal),
    ];

    [GeneratedRegex(@"\{(?<name>[A-Za-z][A-Za-z0-9]*)\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}

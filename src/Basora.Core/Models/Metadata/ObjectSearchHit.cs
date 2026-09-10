namespace Basora.Core.Models.Metadata;

/// <summary>One result from an object search, with what the palette needs to draw it.</summary>
/// <param name="Ref">The object found.</param>
/// <param name="DisplayName">The label to draw, which <paramref name="MatchIndices"/> indexes into.</param>
/// <param name="Qualifier">The context line under the name, such as the schema or the owning table.</param>
/// <param name="Rank">The score, higher first. Comparable only within one search.</param>
/// <param name="MatchIndices">
/// The positions in <paramref name="DisplayName"/> that matched, so the palette can
/// highlight them. Produced by whatever did the matching, because reconstructing them
/// afterwards gets a fuzzy match wrong.
/// </param>
public sealed record ObjectSearchHit(
    DbObjectRef Ref,
    string DisplayName,
    string? Qualifier,
    double Rank,
    IReadOnlyList<int> MatchIndices);

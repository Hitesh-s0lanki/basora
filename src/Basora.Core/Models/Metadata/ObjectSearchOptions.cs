namespace Basora.Core.Models.Metadata;

/// <summary>How wide an object search should cast.</summary>
/// <remarks>
/// Every field exists to bound the query. Open Anything runs against databases with tens
/// of thousands of objects, where an unbounded search is both slow and useless.
/// </remarks>
public sealed record ObjectSearchOptions
{
    /// <summary>How many hits to return. The palette renders about fifty.</summary>
    public int Limit { get; init; } = 50;

    /// <summary>Restrict to these kinds, or leave empty to search them all.</summary>
    public IReadOnlyList<DbObjectKind> Kinds { get; init; } = [];

    /// <summary>Restrict to these schemas, or leave empty to search every schema the role can see.</summary>
    public IReadOnlyList<string> Schemas { get; init; } = [];

    /// <summary>
    /// The schema to rank above the others, normally the one the user is working in.
    /// </summary>
    public string? PreferredSchema { get; init; }

    /// <summary>Whether to include system catalogs, which are hidden by default.</summary>
    public bool IncludeSystemObjects { get; init; }
}

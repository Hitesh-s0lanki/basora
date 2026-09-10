namespace Basora.Core.Models.Metadata;

/// <summary>
/// Which part of an object's metadata a cache entry holds, and which parts a change
/// invalidates.
/// </summary>
/// <remarks>
/// Flags rather than a plain enum because invalidation is nearly always partial.
/// <c>ANALYZE</c> changes the statistics and nothing else; <c>CREATE INDEX</c> changes
/// the indexes and nothing else. Throwing away a whole descriptor for either means
/// refetching a table's columns because someone built an index.
/// </remarks>
[Flags]
public enum MetadataAspect
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>The list of an object's children in the tree.</summary>
    Children = 1 << 0,

    /// <summary>Column definitions.</summary>
    Columns = 1 << 1,

    /// <summary>Indexes.</summary>
    Indexes = 1 << 2,

    /// <summary>Constraints.</summary>
    Constraints = 1 << 3,

    /// <summary>Foreign keys in both directions.</summary>
    ForeignKeys = 1 << 4,

    /// <summary>Triggers.</summary>
    Triggers = 1 << 5,

    /// <summary>Sizes, tuple counts and maintenance times.</summary>
    Statistics = 1 << 6,

    /// <summary>The object's source text, such as a view's body or a function's definition.</summary>
    Definition = 1 << 7,

    /// <summary>Everything Basora caches about an object.</summary>
    All = Children | Columns | Indexes | Constraints | ForeignKeys | Triggers | Statistics | Definition,
}

namespace Basora.Core.Models.Metadata;

/// <summary>One index, with the usage statistics the index analyzer needs.</summary>
/// <remarks>
/// The statistics are nullable on purpose. <c>pg_stat_user_indexes</c> is unavailable to
/// some roles and meaningless after a statistics reset, and a rule that guesses in that
/// case will tell someone to drop an index their application depends on. Absent
/// statistics suppress a finding rather than producing one.
/// </remarks>
public sealed record IndexDescriptor
{
    /// <summary>The index name, unquoted.</summary>
    public required string Name { get; init; }

    /// <summary>The key columns, in index order, which is the order that decides what the index can serve.</summary>
    public required IReadOnlyList<string> Columns { get; init; }

    /// <summary>Non-key columns carried by <c>INCLUDE</c>.</summary>
    public IReadOnlyList<string> IncludedColumns { get; init; } = [];

    /// <summary>The access method: <c>btree</c>, <c>gin</c>, <c>gist</c>, <c>brin</c> or <c>hash</c>.</summary>
    public required string Method { get; init; }

    /// <summary>Whether the index enforces uniqueness.</summary>
    public bool IsUnique { get; init; }

    /// <summary>Whether this index backs the primary key.</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Whether this index backs an exclusion constraint.</summary>
    public bool IsExclusion { get; init; }

    /// <summary>
    /// Whether the index is valid. A false value usually means a
    /// <c>CREATE INDEX CONCURRENTLY</c> that failed and left the index behind unusable.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>The <c>WHERE</c> clause of a partial index.</summary>
    public string? Predicate { get; init; }

    /// <summary>The full <c>CREATE INDEX</c> text from <c>pg_get_indexdef</c>.</summary>
    public string? Definition { get; init; }

    /// <summary>Size on disk, or <see langword="null"/> when not measured.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Scans of this index since the last statistics reset, or <see langword="null"/> when unavailable.</summary>
    public long? ScanCount { get; init; }

    /// <summary>Index entries returned since the last statistics reset.</summary>
    public long? TuplesRead { get; init; }

    /// <summary>Table rows fetched through this index since the last statistics reset.</summary>
    public long? TuplesFetched { get; init; }
}

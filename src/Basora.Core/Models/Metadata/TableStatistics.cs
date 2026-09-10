namespace Basora.Core.Models.Metadata;

/// <summary>
/// What <c>pg_stat_user_tables</c> and <c>pg_class</c> say about a relation's size and
/// its maintenance history.
/// </summary>
/// <remarks>
/// Every number here is nullable, and that is the point. A role without access to the
/// statistics views, a relation that has never been analyzed, and a server whose
/// statistics were reset an hour ago all produce absent values rather than zeroes. A rule
/// that reads a missing value as zero concludes a table is empty, or that an index is
/// unused, and then advises someone to act on it.
/// </remarks>
public sealed record TableStatistics
{
    /// <summary>
    /// The planner's row estimate, or <see langword="null"/> when unknown.
    /// </summary>
    /// <remarks>
    /// <c>reltuples</c> is -1 on a relation that has never been analyzed, and stale
    /// otherwise. Readers map any negative value to null so that "-1 rows" cannot reach a
    /// screen, and the UI labels what is left as an estimate.
    /// </remarks>
    public long? EstimatedRows { get; init; }

    /// <summary>Live rows, as the statistics collector counts them.</summary>
    public long? LiveTuples { get; init; }

    /// <summary>
    /// Dead rows awaiting vacuum. Read next to <see cref="LiveTuples"/>: the ratio is
    /// what indicates bloat, not the count.
    /// </summary>
    public long? DeadTuples { get; init; }

    /// <summary>Size of the table's own heap.</summary>
    public long? TableSizeBytes { get; init; }

    /// <summary>Combined size of every index on the table.</summary>
    public long? IndexesSizeBytes { get; init; }

    /// <summary>Size of the TOAST relation holding oversized values.</summary>
    public long? ToastSizeBytes { get; init; }

    /// <summary>Everything the relation occupies, from <c>pg_total_relation_size</c>.</summary>
    public long? TotalSizeBytes { get; init; }

    /// <summary>When someone last ran <c>VACUUM</c> by hand.</summary>
    public DateTimeOffset? LastVacuum { get; init; }

    /// <summary>When autovacuum last ran.</summary>
    public DateTimeOffset? LastAutoVacuum { get; init; }

    /// <summary>When someone last ran <c>ANALYZE</c> by hand.</summary>
    public DateTimeOffset? LastAnalyze { get; init; }

    /// <summary>When autoanalyze last ran.</summary>
    public DateTimeOffset? LastAutoAnalyze { get; init; }

    /// <summary>Sequential scans since the last statistics reset.</summary>
    public long? SequentialScans { get; init; }

    /// <summary>Index scans since the last statistics reset.</summary>
    public long? IndexScans { get; init; }

    /// <summary>
    /// When the statistics were last reset, when the server reports it.
    /// </summary>
    /// <remarks>
    /// A recent reset makes every count here an undercount. The index analyzer downgrades
    /// its unused-index findings to informational rather than dropping them, because the
    /// counts are not wrong, only young.
    /// </remarks>
    public DateTimeOffset? StatisticsResetAt { get; init; }

    /// <summary>Whether the relation has ever been analyzed, by hand or automatically.</summary>
    public bool HasEverBeenAnalyzed => LastAnalyze is not null || LastAutoAnalyze is not null;
}

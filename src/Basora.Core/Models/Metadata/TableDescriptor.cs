namespace Basora.Core.Models.Metadata;

/// <summary>
/// Everything Basora knows about one relation, assembled from every catalog reader.
/// </summary>
/// <remarks>
/// This is the unit the structure view renders, the DDL generator diffs and the grid uses
/// to decide whether a row can be edited. It covers views, matviews and foreign tables as
/// well as tables; the collections that do not apply to those simply come back empty.
/// </remarks>
public sealed record TableDescriptor
{
    /// <summary>Which relation this describes.</summary>
    public required DbObjectRef Ref { get; init; }

    /// <summary>The columns, in <c>attnum</c> order.</summary>
    public required IReadOnlyList<ColumnDescriptor> Columns { get; init; }

    /// <summary>The indexes on this relation.</summary>
    public IReadOnlyList<IndexDescriptor> Indexes { get; init; } = [];

    /// <summary>The constraints on this relation.</summary>
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; init; } = [];

    /// <summary>Foreign keys declared on this relation, pointing outwards.</summary>
    public IReadOnlyList<ForeignKeyDescriptor> OutgoingForeignKeys { get; init; } = [];

    /// <summary>Foreign keys on other relations that point at this one.</summary>
    public IReadOnlyList<ForeignKeyDescriptor> IncomingForeignKeys { get; init; } = [];

    /// <summary>The triggers on this relation, excluding the internal ones that enforce foreign keys.</summary>
    public IReadOnlyList<TriggerDescriptor> Triggers { get; init; } = [];

    /// <summary>
    /// The primary key's columns, in key order.
    /// </summary>
    /// <remarks>
    /// Key order, not table order. A key on <c>(tenant_id, id)</c> is a different index
    /// from one on <c>(id, tenant_id)</c>, and generated <c>WHERE</c> clauses have to
    /// preserve it.
    /// </remarks>
    public IReadOnlyList<string> PrimaryKeyColumns { get; init; } = [];

    /// <summary>The owning role.</summary>
    public string? Owner { get; init; }

    /// <summary>A non-default tablespace, when one is set.</summary>
    public string? Tablespace { get; init; }

    /// <summary>Whether the relation is partitioned, so its rows live in its partitions.</summary>
    public bool IsPartitioned { get; init; }

    /// <summary>The partitioning strategy, as written: <c>RANGE</c>, <c>LIST</c> or <c>HASH</c>.</summary>
    public string? PartitionStrategy { get; init; }

    /// <summary>
    /// Whether the table is unlogged, meaning its contents do not survive a crash and are
    /// not replicated. Worth surfacing before anyone stores something important in it.
    /// </summary>
    public bool IsUnlogged { get; init; }

    /// <summary>The relation's comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Size and maintenance statistics, when they could be read.</summary>
    public TableStatistics? Statistics { get; init; }

    /// <summary>Whether the relation has a primary key.</summary>
    public bool HasPrimaryKey => PrimaryKeyColumns.Count > 0;

    /// <summary>Finds a column by name, using PostgreSQL's own case sensitivity: exact.</summary>
    /// <param name="name">The column name, unquoted.</param>
    /// <returns>The column, or <see langword="null"/> when this relation has no such column.</returns>
    public ColumnDescriptor? FindColumn(string name) =>
        Columns.FirstOrDefault(column => string.Equals(column.Name, name, StringComparison.Ordinal));
}

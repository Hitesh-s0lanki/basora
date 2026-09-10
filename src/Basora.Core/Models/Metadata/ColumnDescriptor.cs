namespace Basora.Core.Models.Metadata;

/// <summary>Everything the structure view, the grid and the DDL generator need about one column.</summary>
public sealed record ColumnDescriptor
{
    /// <summary>The column name, unquoted.</summary>
    public required string Name { get; init; }

    /// <summary>The column's position, one-based, as <c>attnum</c> reports it.</summary>
    public required int OrdinalPosition { get; init; }

    /// <summary>The column's type.</summary>
    public required PgType Type { get; init; }

    /// <summary>Whether the column accepts null.</summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// The default, as <c>pg_get_expr</c> renders it. Echoed verbatim into generated DDL,
    /// never parsed.
    /// </summary>
    public string? DefaultExpression { get; init; }

    /// <summary>Whether and how the column generates its own values.</summary>
    public IdentityKind Identity { get; init; }

    /// <summary>The expression of a <c>GENERATED ALWAYS AS</c> stored column.</summary>
    public string? GeneratedExpression { get; init; }

    /// <summary>A non-default collation, when one is declared.</summary>
    public string? Collation { get; init; }

    /// <summary>The column's comment.</summary>
    public string? Comment { get; init; }

    /// <summary>Whether the column is part of the primary key.</summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>Whether a unique constraint or index covers this column on its own.</summary>
    public bool IsUnique { get; init; }

    /// <summary>The foreign key this column participates in, when it does.</summary>
    public ForeignKeyDescriptor? References { get; init; }

    /// <summary>
    /// Whether a value can be written to this column. A generated column is computed by
    /// the server, so the grid shows it read-only rather than failing on save.
    /// </summary>
    public bool IsWritable => GeneratedExpression is null && Identity is not IdentityKind.Always;
}

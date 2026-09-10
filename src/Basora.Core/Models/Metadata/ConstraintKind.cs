namespace Basora.Core.Models.Metadata;

/// <summary>The kind of a table constraint.</summary>
public enum ConstraintKind
{
    /// <summary>A primary key.</summary>
    PrimaryKey,

    /// <summary>A foreign key.</summary>
    ForeignKey,

    /// <summary>A unique constraint.</summary>
    Unique,

    /// <summary>A check constraint.</summary>
    Check,

    /// <summary>An exclusion constraint.</summary>
    Exclusion,

    /// <summary>A not-null constraint, which PostgreSQL records on the column rather than as a named constraint.</summary>
    NotNull,
}

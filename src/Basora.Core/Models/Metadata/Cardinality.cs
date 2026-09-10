namespace Basora.Core.Models.Metadata;

/// <summary>The shape of a relationship between two tables, as inferred from its keys.</summary>
/// <remarks>
/// Inferred, not declared: PostgreSQL records a foreign key, and whether that key is also
/// unique is what makes the relationship one-to-one rather than many-to-one.
/// </remarks>
public enum Cardinality
{
    /// <summary>Each row on one side matches at most one row on the other.</summary>
    OneToOne,

    /// <summary>Each row on this side matches many rows on the other.</summary>
    OneToMany,

    /// <summary>Many rows on this side match one row on the other.</summary>
    ManyToOne,

    /// <summary>Many on both sides, which in PostgreSQL means a join table.</summary>
    ManyToMany,
}

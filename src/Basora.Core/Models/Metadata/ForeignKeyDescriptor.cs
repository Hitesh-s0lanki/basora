namespace Basora.Core.Models.Metadata;

/// <summary>One foreign key, described from both ends so the relations tab can read it either way.</summary>
public sealed record ForeignKeyDescriptor
{
    /// <summary>The constraint name, unquoted.</summary>
    public required string Name { get; init; }

    /// <summary>The table holding the referencing columns.</summary>
    public required DbObjectRef SourceTable { get; init; }

    /// <summary>The referencing columns, paired positionally with <see cref="TargetColumns"/>.</summary>
    public required IReadOnlyList<string> SourceColumns { get; init; }

    /// <summary>The referenced table, which may be the source table itself.</summary>
    public required DbObjectRef TargetTable { get; init; }

    /// <summary>The referenced columns, paired positionally with <see cref="SourceColumns"/>.</summary>
    public required IReadOnlyList<string> TargetColumns { get; init; }

    /// <summary>What happens to referencing rows when the referenced row is updated.</summary>
    public ReferentialAction OnUpdate { get; init; }

    /// <summary>What happens to referencing rows when the referenced row is deleted.</summary>
    public ReferentialAction OnDelete { get; init; }

    /// <summary>The relationship's shape, inferred from whether the referencing columns are themselves unique.</summary>
    public Cardinality Cardinality { get; init; }

    /// <summary>Whether the key points back at its own table.</summary>
    public bool IsSelfReference => SourceTable == TargetTable;
}

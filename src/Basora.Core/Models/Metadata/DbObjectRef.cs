namespace Basora.Core.Models.Metadata;

/// <summary>
/// Names one database object, everywhere in Basora.
/// </summary>
/// <remarks>
/// The whole product addresses objects through this record: the tree, the metadata
/// cache key, the safety engine, the DDL generator and every service signature. Keeping
/// one reference type is what lets a node in the tree, a tab, a cache entry and a
/// generated statement all be talking about provably the same object.
/// <para>
/// <paramref name="Oid"/> is carried when it is known because it survives a rename,
/// while the name does not. It is not part of identity, because an object read from a
/// saved workspace has no OID yet.
/// </para>
/// </remarks>
/// <param name="Kind">What sort of object this is.</param>
/// <param name="Schema">The containing schema, or <see langword="null"/> for an object that has none, such as a database or a role.</param>
/// <param name="Name">The object's own name, unquoted, exactly as the catalog holds it.</param>
/// <param name="Oid">The catalog OID, when it is known.</param>
/// <param name="Parent">The name of the owning relation, for a column, index, constraint or trigger.</param>
public sealed record DbObjectRef(
    DbObjectKind Kind,
    string? Schema,
    string Name,
    uint? Oid = null,
    string? Parent = null)
{
    /// <summary>
    /// The name as a person writes it, <c>schema.name</c> or just <c>name</c>. For
    /// display and for logs. Not safe to put in SQL — use <see cref="QuotedName"/>.
    /// </summary>
    public string QualifiedName => Schema is null ? Name : $"{Schema}.{Name}";

    /// <summary>
    /// The name as SQL needs it, always quoted. This is what generated statements use.
    /// </summary>
    public string QuotedName => SqlIdentifier.QuoteQualified(Schema, Name);

    /// <summary>Whether this object holds rows a grid could read: a table, view, matview or foreign table.</summary>
    public bool IsRelation => Kind is DbObjectKind.Table
        or DbObjectKind.PartitionedTable
        or DbObjectKind.ForeignTable
        or DbObjectKind.View
        or DbObjectKind.MaterializedView;
}

namespace Basora.Core.Models.Metadata;

/// <summary>
/// A PostgreSQL type as Basora needs to know it: what to call it, how to behave towards
/// it, and the modifiers that change how a value is rendered.
/// </summary>
public sealed record PgType
{
    /// <summary>The catalog name, such as <c>varchar</c>, <c>int4</c> or <c>_text</c> for an array.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// The name as a person writes it, from <c>format_type</c>, such as
    /// <c>character varying(255)</c>. What a column header shows.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>The behaviour class that drives rendering, editing, filtering and export.</summary>
    public required PgTypeCategory Category { get; init; }

    /// <summary>The declared length for a character type, or <see langword="null"/> when unbounded.</summary>
    public int? Length { get; init; }

    /// <summary>The declared precision for a numeric type.</summary>
    public int? Precision { get; init; }

    /// <summary>The declared scale for a numeric type.</summary>
    public int? Scale { get; init; }

    /// <summary>Whether this is an array type.</summary>
    public bool IsArray { get; init; }

    /// <summary>The element type's name when <see cref="IsArray"/> is set.</summary>
    public string? ElementType { get; init; }

    /// <summary>
    /// The labels of an enum type, in the catalog's sort order rather than alphabetical,
    /// because that order is what comparisons use.
    /// </summary>
    public IReadOnlyList<string>? EnumValues { get; init; }

    /// <summary>The type's catalog OID.</summary>
    public uint Oid { get; init; }
}

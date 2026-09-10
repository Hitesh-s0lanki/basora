namespace Basora.Core.Models.Metadata;

/// <summary>
/// The behaviour class of a PostgreSQL type.
/// </summary>
/// <remarks>
/// This drives grid cell rendering, cell editor selection, filter operator lists and
/// export formatting, so a type Basora does not recognise still behaves sensibly as long
/// as it lands in the right category. Adding a member is a coordinated change: the
/// mapping table in docs/05-postgresql-data-layer.md section 7, the editors in T-D04 and
/// the operator catalogue in T-F06 all switch on it.
/// </remarks>
public enum PgTypeCategory
{
    /// <summary>Integers, <c>numeric</c> and floating point.</summary>
    Numeric,

    /// <summary><c>text</c>, <c>varchar</c>, <c>char</c> and <c>citext</c>.</summary>
    Text,

    /// <summary><c>bool</c>.</summary>
    Boolean,

    /// <summary>Dates, times, timestamps with and without a zone, and intervals.</summary>
    DateTime,

    /// <summary><c>uuid</c>.</summary>
    Uuid,

    /// <summary><c>json</c> and <c>jsonb</c>, both kept as raw text.</summary>
    Json,

    /// <summary><c>bytea</c>.</summary>
    Binary,

    /// <summary>An array of any element type.</summary>
    Array,

    /// <summary>A range or multirange type.</summary>
    Range,

    /// <summary>A composite type.</summary>
    Composite,

    /// <summary>An enumerated type.</summary>
    Enum,

    /// <summary>A domain, which renders as its underlying type plus a constraint hint.</summary>
    Domain,

    /// <summary>The geometric types, such as <c>point</c> and <c>polygon</c>.</summary>
    Geometric,

    /// <summary>The network address types, such as <c>inet</c> and <c>cidr</c>.</summary>
    Network,

    /// <summary>pgvector's <c>vector</c>.</summary>
    Vector,

    /// <summary>
    /// Anything Basora has no mapping for. Degrades to text rather than failing, per
    /// docs/05-postgresql-data-layer.md section 7.
    /// </summary>
    Unknown,
}

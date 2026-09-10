using Basora.Core.Models.Metadata;
using Basora.Core.Results;

namespace Basora.Core.Interfaces.Metadata;

/// <summary>
/// Resolves the types a database actually has, as opposed to the ones every PostgreSQL
/// has.
/// </summary>
/// <remarks>
/// The built-in mapping from type name to <see cref="PgTypeCategory"/> is a static table
/// and needs no server. This contract covers what the table cannot know: the enums,
/// domains and composites a particular database defines.
/// <para>
/// Nothing here ever fails because a type is unrecognised. An unmapped type resolves to
/// <see cref="PgTypeCategory.Unknown"/> and renders as text, because the alternative is a
/// grid that crashes on somebody's custom type.
/// </para>
/// </remarks>
public interface ITypeCatalog
{
    /// <summary>Resolves a type by its catalog OID.</summary>
    /// <param name="oid">The type's OID, as a result-set schema reports it.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<PgType>> ResolveAsync(uint oid, CancellationToken cancellationToken);

    /// <summary>Lists the enums, domains and composites defined in a schema.</summary>
    /// <param name="schema">The schema to list.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<PgType>>> ListUserTypesAsync(string schema, CancellationToken cancellationToken);

    /// <summary>
    /// Reads an enum's labels in the catalog's sort order.
    /// </summary>
    /// <remarks>
    /// Sort order, not alphabetical. It is the order comparisons use and the order the
    /// author chose, so a dropdown that alphabetises it is showing something else.
    /// </remarks>
    /// <param name="enumType">The enum type.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<string>>> GetEnumValuesAsync(
        DbObjectRef enumType,
        CancellationToken cancellationToken);
}

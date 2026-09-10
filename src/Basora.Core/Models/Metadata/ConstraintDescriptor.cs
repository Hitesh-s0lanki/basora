namespace Basora.Core.Models.Metadata;

/// <summary>One table constraint.</summary>
/// <param name="Name">The constraint name, unquoted.</param>
/// <param name="Kind">What sort of constraint it is.</param>
/// <param name="Columns">The columns it covers, in declaration order.</param>
/// <param name="Expression">The check or exclusion expression, when there is one.</param>
/// <param name="Definition">The full text from <c>pg_get_constraintdef</c>, echoed verbatim into generated DDL.</param>
/// <param name="IsDeferrable">Whether the check can be deferred to commit.</param>
/// <param name="IsValidated">
/// Whether existing rows have been checked. A constraint added <c>NOT VALID</c> applies
/// to new rows only until someone validates it, which the structure view has to say out
/// loud rather than showing it as an ordinary constraint.
/// </param>
public sealed record ConstraintDescriptor(
    string Name,
    ConstraintKind Kind,
    IReadOnlyList<string> Columns,
    string? Expression,
    string? Definition,
    bool IsDeferrable,
    bool IsValidated);

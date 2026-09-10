namespace Basora.Core.Models.Metadata;

/// <summary>One generated DDL statement, with the sentence shown next to it for review.</summary>
/// <remarks>
/// Generated DDL is always shown before it runs, so a statement is never just its SQL.
/// The summary is what a reviewer reads in a list of twenty statements to decide whether
/// the whole script does what they meant.
/// <para>
/// Statements keep their order. Dropping a constraint before altering the column it
/// covers is not a preference.
/// </para>
/// </remarks>
/// <param name="Sql">The statement, ready to run.</param>
/// <param name="Summary">One line saying what it does, for the review list.</param>
public sealed record DdlStatement(string Sql, string Summary);

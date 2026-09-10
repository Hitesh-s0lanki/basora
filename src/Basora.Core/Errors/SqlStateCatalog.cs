using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Basora.Core.Errors;

/// <summary>
/// Maps a PostgreSQL SQLSTATE to a stable Basora code, a message and a remediation hint.
/// </summary>
/// <remarks>
/// The table is docs/05-postgresql-data-layer.md section 10. Everything here is data and
/// every lookup is total: an unrecognised SQLSTATE degrades to its class, and an
/// unrecognised class degrades to
/// <see cref="BasoraErrorCodes.UnknownDatabaseError"/>. Nothing throws, because this runs
/// on the path that is already handling a failure.
/// </remarks>
public static class SqlStateCatalog
{
    /// <summary>SQLSTATE 57014, which the server raises when a statement is cancelled on request.</summary>
    public const string QueryCancelledSqlState = "57014";

    /// <summary>
    /// Every SQLSTATE Basora recognises exactly, keyed by the five-character state.
    /// </summary>
    public static ImmutableDictionary<string, SqlStateDescriptor> Descriptors { get; } = Build(
    [
        new SqlStateDescriptor(
            "28P01",
            BasoraErrorCodes.InvalidPassword,
            "Authentication failed for user {Role}.",
            "Check the password for {Role} on {Host} and try again."),

        new SqlStateDescriptor(
            "28000",
            BasoraErrorCodes.NotAuthorized,
            "The server refused a connection for user {Role}.",
            "No pg_hba.conf rule on {Host} lets {Role} connect to {Database} from this address with "
                + "this authentication method. An administrator has to add one and reload the server."),

        new SqlStateDescriptor(
            "3D000",
            BasoraErrorCodes.DatabaseNotFound,
            "The database {Database} does not exist on {Host}.",
            "Choose one of the databases the server does have, or create {Database} first."),

        new SqlStateDescriptor(
            "42501",
            BasoraErrorCodes.PrivilegeDenied,
            "The role {Role} lacks {Privilege} on {Object}.",
            "Run as an owner or superuser: GRANT {Privilege} ON {Object} TO {Role};"),

        new SqlStateDescriptor(
            "42P01",
            BasoraErrorCodes.UndefinedTable,
            "The table {Object} does not exist.",
            "Check the spelling and the search_path. Objects with a similar name are offered as "
                + "corrections."),

        new SqlStateDescriptor(
            "42703",
            BasoraErrorCodes.UndefinedColumn,
            "The column {Column} does not exist on {Object}.",
            "Check the spelling against the columns of {Object}, which are offered as corrections."),

        new SqlStateDescriptor(
            "42601",
            BasoraErrorCodes.SyntaxError,
            "Syntax error in the statement.",
            "The caret is on the position the server reported."),

        new SqlStateDescriptor(
            "23505",
            BasoraErrorCodes.UniqueViolation,
            "{Object} already has a row where {Constraint} holds these values.",
            "Change the conflicting values in {Column}, or decide what a conflict should do with "
                + "ON CONFLICT."),

        new SqlStateDescriptor(
            "23503",
            BasoraErrorCodes.ForeignKeyViolation,
            "{Constraint} on {Object} requires a matching row in {ReferencedObject}.",
            "Insert the row in {ReferencedObject} first, or remove the rows in {Object} that refer "
                + "to it."),

        new SqlStateDescriptor(
            "23502",
            BasoraErrorCodes.NotNullViolation,
            "The column {Column} on {Object} does not accept null.",
            "Supply a value for {Column}, or give the column a default."),

        new SqlStateDescriptor(
            "40001",
            BasoraErrorCodes.SerializationFailure,
            "The transaction conflicted with a concurrent one and was rolled back.",
            "Nothing is wrong with the statement. Run it again."),

        new SqlStateDescriptor(
            "40P01",
            BasoraErrorCodes.Deadlock,
            "The transaction deadlocked with another and was rolled back.",
            "Run it again. If it keeps happening, the lock viewer shows which statements contend."),

        new SqlStateDescriptor(
            "53300",
            BasoraErrorCodes.TooManyConnections,
            "{Host} is at its connection limit, {Used} of {Limit}.",
            "Close a connection you are not using, or ask an administrator to raise max_connections."),

        new SqlStateDescriptor(
            QueryCancelledSqlState,
            BasoraErrorCodes.QueryCancelled,
            "Cancelled.",
            "The statement stopped on request. Inside a transaction nothing was left half-applied."),

        new SqlStateDescriptor(
            "57P01",
            BasoraErrorCodes.AdminShutdown,
            "{Host} closed the connection.",
            "The server is shutting down, or an administrator terminated the backend. Reconnect once "
                + "it is back."),

        new SqlStateDescriptor(
            "22P02",
            BasoraErrorCodes.InvalidTextRepresentation,
            "{Value} is not a valid {Type}.",
            "Correct the value at {Location}, or read the column as a type that accepts it."),
    ]);

    /// <summary>
    /// What an unrecognised SQLSTATE degrades to, keyed by its two-character class. A
    /// class is coarse but never wrong, which is the right trade when the alternative is
    /// showing the raw state.
    /// </summary>
    public static ImmutableDictionary<string, SqlStateDescriptor> ClassDescriptors { get; } = Build(
    [
        new SqlStateDescriptor(
            "08",
            BasoraErrorCodes.ConnectionFailed,
            "The connection to {Host} failed.",
            "Check that the server is reachable and still accepting connections, then reconnect."),

        new SqlStateDescriptor(
            "22",
            BasoraErrorCodes.InvalidValue,
            "The server rejected a value in the statement.",
            "Check the values against the types of the columns they are being written to."),

        new SqlStateDescriptor(
            "23",
            BasoraErrorCodes.ConstraintViolated,
            "The statement violates a constraint on {Object}.",
            "The structure view lists the constraints on {Object} and what each one requires."),

        new SqlStateDescriptor(
            "25",
            BasoraErrorCodes.InvalidTransactionState,
            "The statement is not allowed in the transaction's current state.",
            "Commit or roll the transaction back, then run the statement again."),

        new SqlStateDescriptor(
            "28",
            BasoraErrorCodes.AuthenticationFailed,
            "The server refused to authenticate {Role}.",
            "Check the credentials and the server's authentication rules for {Host}."),

        new SqlStateDescriptor(
            "3D",
            BasoraErrorCodes.DatabaseNotFound,
            "The database {Database} does not exist on {Host}.",
            "Choose one of the databases the server does have, or create {Database} first."),

        new SqlStateDescriptor(
            "40",
            BasoraErrorCodes.TransactionRolledBack,
            "The transaction was rolled back.",
            "Nothing was applied. Run it again."),

        new SqlStateDescriptor(
            "42",
            BasoraErrorCodes.QueryRejected,
            "The server rejected the statement.",
            "Check the object names, the column names and the privileges the statement needs."),

        new SqlStateDescriptor(
            "53",
            BasoraErrorCodes.InsufficientResources,
            "{Host} is short of a resource the statement needs.",
            "Wait and retry, or ask an administrator what the server is short of."),

        new SqlStateDescriptor(
            "54",
            BasoraErrorCodes.LimitExceeded,
            "The statement exceeds a limit built into the server.",
            "Reduce what the statement asks for, for example the number of columns or the "
                + "nesting depth."),

        new SqlStateDescriptor(
            "55",
            BasoraErrorCodes.ObjectNotReady,
            "{Object} is not in a state that allows this.",
            "Something else is using {Object}, or it needs preparing first. Retry once that "
                + "finishes."),

        new SqlStateDescriptor(
            "57",
            BasoraErrorCodes.OperatorIntervention,
            "The statement stopped because of an intervention on {Host}.",
            "An administrator or the server itself ended the statement. Reconnect and try again."),

        new SqlStateDescriptor(
            "58",
            BasoraErrorCodes.SystemError,
            "{Host} hit a system error outside PostgreSQL.",
            "The server could not read or write something it needed. This is one for whoever "
                + "runs it."),

        new SqlStateDescriptor(
            "XX",
            BasoraErrorCodes.InternalServerError,
            "PostgreSQL reported an internal error.",
            "This can mean data corruption. Stop writing to {Database} and tell whoever runs the "
                + "server."),
    ]);

    /// <summary>
    /// The descriptor used when neither the SQLSTATE nor its class is recognised, and
    /// when there is no SQLSTATE at all.
    /// </summary>
    public static SqlStateDescriptor Unknown { get; } = new(
        SqlState: "",
        BasoraErrorCodes.UnknownDatabaseError,
        "The database reported an error.",
        "The server's own message is in the detail below.");

    /// <summary>
    /// The best descriptor for <paramref name="sqlState"/>: the exact match, else its
    /// class, else <see cref="Unknown"/>. Total — null, empty and malformed input all
    /// return <see cref="Unknown"/> rather than throwing.
    /// </summary>
    /// <param name="sqlState">A SQLSTATE as PostgreSQL reports it, or <see langword="null"/>.</param>
    public static SqlStateDescriptor Describe(string? sqlState)
    {
        if (string.IsNullOrEmpty(sqlState))
        {
            return Unknown;
        }

        if (Descriptors.TryGetValue(sqlState, out SqlStateDescriptor? exact))
        {
            return exact;
        }

        return sqlState.Length >= 2
            && ClassDescriptors.TryGetValue(sqlState[..2], out SqlStateDescriptor? byClass)
                ? byClass
                : Unknown;
    }

    /// <summary>
    /// The stable code for <paramref name="sqlState"/>. Shorthand for
    /// <see cref="Describe"/> when only the code is wanted.
    /// </summary>
    /// <param name="sqlState">A SQLSTATE as PostgreSQL reports it, or <see langword="null"/>.</param>
    public static string CodeFor(string? sqlState) => Describe(sqlState).Code;

    /// <summary>
    /// Whether <paramref name="sqlState"/> means the user cancelled. Cancellation is
    /// control flow, not a failure, and must never be logged or shown as one
    /// (docs/12-coding-standards.md section 2).
    /// </summary>
    /// <param name="sqlState">A SQLSTATE as PostgreSQL reports it, or <see langword="null"/>.</param>
    public static bool IsCancellation([NotNullWhen(true)] string? sqlState) =>
        string.Equals(sqlState, QueryCancelledSqlState, StringComparison.Ordinal);

    private static ImmutableDictionary<string, SqlStateDescriptor> Build(
        IEnumerable<SqlStateDescriptor> descriptors) =>
        descriptors.ToImmutableDictionary(d => d.SqlState, StringComparer.Ordinal);
}

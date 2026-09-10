using System.Collections.Immutable;

namespace Basora.Core.Errors;

/// <summary>
/// Every stable error code Basora produces.
/// </summary>
/// <remarks>
/// A code is the contract between a failure and the screen that renders it: the UI keys
/// its empty states, its inline prompts and its localisation off the code, never off the
/// message text (docs/04-domain-model.md section 11). So a code is append-only. Renaming
/// one silently changes behaviour in every screen that branches on it.
/// <para>
/// The shape is <c>basora.&lt;area&gt;.&lt;reason&gt;</c>, lowercase, with underscores
/// inside a segment. <c>ClassCodes</c> in <see cref="SqlStateCatalog"/> holds the coarse
/// code an unrecognised SQLSTATE degrades to.
/// </para>
/// </remarks>
public static class BasoraErrorCodes
{
    // ---- Connection -------------------------------------------------------------

    /// <summary>The host refused the TCP connection, or no server is listening.</summary>
    public const string ConnectionRefused = "basora.connection.refused";

    /// <summary>The connection attempt exceeded its timeout.</summary>
    public const string ConnectionTimeout = "basora.connection.timeout";

    /// <summary>The connection failed for a reason inside SQLSTATE class 08.</summary>
    public const string ConnectionFailed = "basora.connection.failed";

    /// <summary>SQLSTATE 3D000: the named database does not exist on this server.</summary>
    public const string DatabaseNotFound = "basora.connection.database_not_found";

    /// <summary>SQLSTATE 53300: the server is at <c>max_connections</c>.</summary>
    public const string TooManyConnections = "basora.connection.too_many_connections";

    /// <summary>SQLSTATE 57P01: the server or an administrator closed the backend.</summary>
    public const string AdminShutdown = "basora.connection.admin_shutdown";

    // ---- Authentication and privilege -------------------------------------------

    /// <summary>SQLSTATE 28P01: the password was wrong.</summary>
    public const string InvalidPassword = "basora.auth.invalid_password";

    /// <summary>SQLSTATE 28000: no <c>pg_hba.conf</c> rule allows this role from this host.</summary>
    public const string NotAuthorized = "basora.auth.not_authorized";

    /// <summary>Authentication failed for a reason inside SQLSTATE class 28.</summary>
    public const string AuthenticationFailed = "basora.auth.failed";

    /// <summary>SQLSTATE 42501: the role lacks a privilege on an object.</summary>
    public const string PrivilegeDenied = "basora.privilege.denied";

    // ---- Statements and schema --------------------------------------------------

    /// <summary>SQLSTATE 42601: the server rejected the statement's syntax.</summary>
    public const string SyntaxError = "basora.query.syntax_error";

    /// <summary>SQLSTATE 57014: the statement was cancelled on request. Not a failure to apologise for.</summary>
    public const string QueryCancelled = "basora.query.cancelled";

    /// <summary>The server rejected the statement for a reason inside SQLSTATE class 42.</summary>
    public const string QueryRejected = "basora.query.rejected";

    /// <summary>SQLSTATE 42P01: the statement names a table that does not exist.</summary>
    public const string UndefinedTable = "basora.schema.undefined_table";

    /// <summary>SQLSTATE 42703: the statement names a column that does not exist.</summary>
    public const string UndefinedColumn = "basora.schema.undefined_column";

    // ---- Data and constraints ---------------------------------------------------

    /// <summary>SQLSTATE 22P02: a literal is not valid for the type it is being read as.</summary>
    public const string InvalidTextRepresentation = "basora.data.invalid_text_representation";

    /// <summary>A value was rejected for a reason inside SQLSTATE class 22.</summary>
    public const string InvalidValue = "basora.data.invalid_value";

    /// <summary>SQLSTATE 23505: a unique constraint or index already has these values.</summary>
    public const string UniqueViolation = "basora.constraint.unique_violation";

    /// <summary>SQLSTATE 23503: a foreign key has no matching row in the referenced table.</summary>
    public const string ForeignKeyViolation = "basora.constraint.foreign_key_violation";

    /// <summary>SQLSTATE 23502: a not-null column was given null.</summary>
    public const string NotNullViolation = "basora.constraint.not_null_violation";

    /// <summary>A constraint was violated for a reason inside SQLSTATE class 23.</summary>
    public const string ConstraintViolated = "basora.constraint.violated";

    // ---- Transactions -----------------------------------------------------------

    /// <summary>SQLSTATE 40001: a serialisable transaction lost a conflict and rolled back.</summary>
    public const string SerializationFailure = "basora.transaction.serialization_failure";

    /// <summary>SQLSTATE 40P01: the transaction deadlocked and was chosen as the victim.</summary>
    public const string Deadlock = "basora.transaction.deadlock";

    /// <summary>The transaction rolled back for a reason inside SQLSTATE class 40.</summary>
    public const string TransactionRolledBack = "basora.transaction.rolled_back";

    /// <summary>The statement is not valid in the transaction's current state (SQLSTATE class 25).</summary>
    public const string InvalidTransactionState = "basora.transaction.invalid_state";

    // ---- Server -----------------------------------------------------------------

    /// <summary>The server is out of a resource other than connections (SQLSTATE class 53).</summary>
    public const string InsufficientResources = "basora.server.insufficient_resources";

    /// <summary>A built-in server limit was exceeded (SQLSTATE class 54).</summary>
    public const string LimitExceeded = "basora.server.limit_exceeded";

    /// <summary>The object is not in the state the statement requires (SQLSTATE class 55).</summary>
    public const string ObjectNotReady = "basora.object.not_ready";

    /// <summary>An operator intervened, for example a shutdown or a terminated backend (SQLSTATE class 57).</summary>
    public const string OperatorIntervention = "basora.server.intervention";

    /// <summary>The server hit an external system error such as a failed read (SQLSTATE class 58).</summary>
    public const string SystemError = "basora.server.system_error";

    /// <summary>The server reported an internal error, including data corruption (SQLSTATE class XX).</summary>
    public const string InternalServerError = "basora.server.internal_error";

    /// <summary>The server reported a SQLSTATE Basora does not recognise at all.</summary>
    public const string UnknownDatabaseError = "basora.database.unknown_error";

    // ---- Basora itself ----------------------------------------------------------

    /// <summary>
    /// A <c>Result</c> was read before anything assigned to it. Always a defect in
    /// Basora; see <see cref="Results.Result"/>.
    /// </summary>
    public const string UninitialisedResult = "basora.internal.uninitialised_result";

    /// <summary>
    /// Every code declared here, so a test can hold the whole set to one shape and to
    /// uniqueness rather than checking them one at a time.
    /// </summary>
    public static ImmutableArray<string> All { get; } =
    [
        ConnectionRefused,
        ConnectionTimeout,
        ConnectionFailed,
        DatabaseNotFound,
        TooManyConnections,
        AdminShutdown,
        InvalidPassword,
        NotAuthorized,
        AuthenticationFailed,
        PrivilegeDenied,
        SyntaxError,
        QueryCancelled,
        QueryRejected,
        UndefinedTable,
        UndefinedColumn,
        InvalidTextRepresentation,
        InvalidValue,
        UniqueViolation,
        ForeignKeyViolation,
        NotNullViolation,
        ConstraintViolated,
        SerializationFailure,
        Deadlock,
        TransactionRolledBack,
        InvalidTransactionState,
        InsufficientResources,
        LimitExceeded,
        ObjectNotReady,
        OperatorIntervention,
        SystemError,
        InternalServerError,
        UnknownDatabaseError,
        UninitialisedResult,
    ];
}

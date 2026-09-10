namespace Basora.Core.Models.Metadata;

/// <summary>
/// Every kind of database object Basora can name, show in the tree or act on.
/// </summary>
/// <remarks>
/// The list is docs/04-domain-model.md section 3 and is deliberately PostgreSQL's
/// vocabulary rather than a portable abstraction over it: a partitioned table and a
/// foreign table behave differently enough that flattening them into "table" would lose
/// the distinctions the explorer and the structure editor need.
/// </remarks>
public enum DbObjectKind
{
    /// <summary>A database on the connected server.</summary>
    Database,

    /// <summary>A schema inside a database.</summary>
    Schema,

    /// <summary>An ordinary table.</summary>
    Table,

    /// <summary>A table declared <c>PARTITION BY</c>, whose rows live in its partitions.</summary>
    PartitionedTable,

    /// <summary>A table backed by a foreign data wrapper.</summary>
    ForeignTable,

    /// <summary>A view.</summary>
    View,

    /// <summary>A materialised view, which holds its own rows and is refreshed explicitly.</summary>
    MaterializedView,

    /// <summary>A column of a relation.</summary>
    Column,

    /// <summary>An index.</summary>
    Index,

    /// <summary>A constraint.</summary>
    Constraint,

    /// <summary>A trigger.</summary>
    Trigger,

    /// <summary>A function.</summary>
    Function,

    /// <summary>A procedure, which unlike a function may control transactions.</summary>
    Procedure,

    /// <summary>An aggregate function.</summary>
    Aggregate,

    /// <summary>A sequence.</summary>
    Sequence,

    /// <summary>A composite or base type.</summary>
    Type,

    /// <summary>A domain, a base type with constraints attached.</summary>
    Domain,

    /// <summary>An enumerated type.</summary>
    Enum,

    /// <summary>An installed extension.</summary>
    Extension,

    /// <summary>A foreign data wrapper.</summary>
    ForeignDataWrapper,

    /// <summary>A foreign server.</summary>
    Server,

    /// <summary>A role, which PostgreSQL uses for both users and groups.</summary>
    Role,

    /// <summary>A row-level security policy.</summary>
    Policy,

    /// <summary>A logical replication publication.</summary>
    Publication,

    /// <summary>A logical replication subscription.</summary>
    Subscription,

    /// <summary>A tablespace.</summary>
    Tablespace,
}

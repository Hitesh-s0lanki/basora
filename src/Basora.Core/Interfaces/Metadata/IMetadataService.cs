using Basora.Core.Models.Metadata;
using Basora.Core.Results;

namespace Basora.Core.Interfaces.Metadata;

/// <summary>
/// Reads schema metadata for one session, through the cache and the metadata channel.
/// </summary>
/// <remarks>
/// An implementation is bound to a single session, which is why nothing here takes a
/// session identifier. It also always uses the metadata channel: catalog reads must never
/// share the connection a user's query is running on, or expanding a tree node blocks
/// behind a long <c>SELECT</c>.
/// </remarks>
public interface IMetadataService
{
    /// <summary>The session these reads belong to, and the session the cache is keyed by.</summary>
    Guid SessionId { get; }

    /// <summary>Lists the databases on the connected server.</summary>
    /// <param name="includeTemplates">Whether to include template databases, hidden by default.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<ObjectNode>>> GetDatabasesAsync(
        bool includeTemplates,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists what sits under <paramref name="parent"/> in the tree.
    /// </summary>
    /// <remarks>
    /// A role that cannot see the children gets an empty list, not a failure. Being denied
    /// a schema is an ordinary state of affairs, and an error dialog for each one makes
    /// the tree unusable on a locked-down server.
    /// </remarks>
    /// <param name="parent">The node being expanded.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<IReadOnlyList<ObjectNode>>> GetChildrenAsync(
        DbObjectRef parent,
        CancellationToken cancellationToken);

    /// <summary>Assembles the full descriptor for a relation.</summary>
    /// <param name="table">The relation to describe.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<TableDescriptor>> GetTableAsync(DbObjectRef table, CancellationToken cancellationToken);

    /// <summary>Reads size and maintenance statistics for a relation.</summary>
    /// <param name="table">The relation to measure.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<TableStatistics>> GetStatisticsAsync(DbObjectRef table, CancellationToken cancellationToken);

    /// <summary>Discards what is cached for an object and reads it again.</summary>
    /// <param name="scope">The object to refresh.</param>
    /// <param name="aspects">Which parts to refresh.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result> RefreshAsync(DbObjectRef scope, MetadataAspect aspects, CancellationToken cancellationToken);

    /// <summary>
    /// Marks cached metadata stale without reading anything.
    /// </summary>
    /// <remarks>
    /// Called by the executor after any DDL statement runs through Basora, which is what
    /// keeps the tree honest without polling.
    /// </remarks>
    /// <param name="scope">The object that changed.</param>
    /// <param name="aspects">Which parts changed.</param>
    void Invalidate(DbObjectRef scope, MetadataAspect aspects);
}

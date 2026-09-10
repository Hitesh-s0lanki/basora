using Basora.Core.Models.Metadata;

namespace Basora.Core.Interfaces.Metadata;

/// <summary>
/// Holds read metadata for the life of a session, and nothing longer.
/// </summary>
/// <remarks>
/// Synchronous on purpose. A cache lookup that can await is a cache lookup that can
/// block a tree expansion, and every method here is expected to be an in-memory
/// dictionary operation. Fetching on a miss is the service's job, not the cache's.
/// </remarks>
public interface IMetadataCache
{
    /// <summary>Reads an entry, together with when it was loaded.</summary>
    /// <typeparam name="T">What was cached under this key.</typeparam>
    /// <param name="key">The entry to read.</param>
    /// <param name="entry">The cached value and its timestamp, when there is one.</param>
    /// <returns><see langword="true"/> when the entry was present.</returns>
    bool TryGet<T>(MetadataCacheKey key, out CachedMetadata<T> entry);

    /// <summary>Stores a value, replacing whatever was there.</summary>
    /// <typeparam name="T">What is being cached.</typeparam>
    /// <param name="key">Where to store it.</param>
    /// <param name="value">The value.</param>
    /// <param name="loadedAt">When it was read from the server.</param>
    void Store<T>(MetadataCacheKey key, T value, DateTimeOffset loadedAt);

    /// <summary>
    /// Drops what is cached for an object and everything under it.
    /// </summary>
    /// <param name="sessionId">The session to invalidate within.</param>
    /// <param name="scope">The object that changed. Invalidating a schema invalidates its relations.</param>
    /// <param name="aspects">Which parts to drop.</param>
    void Invalidate(Guid sessionId, DbObjectRef scope, MetadataAspect aspects);

    /// <summary>Drops everything held for a session, on disconnect.</summary>
    /// <param name="sessionId">The session that ended.</param>
    void InvalidateSession(Guid sessionId);
}

using Basora.Core.Models.Metadata;
using Basora.Core.Results;

namespace Basora.Core.Interfaces.Metadata;

/// <summary>Finds database objects by name, for Open Anything.</summary>
/// <remarks>
/// Ranked server-side, because the databases this has to work on hold tens of thousands
/// of objects and preloading them all to rank locally is the thing this exists to avoid.
/// The palette queries the cache first for an instant answer, then merges these results
/// as they arrive.
/// </remarks>
public interface IObjectSearchService
{
    /// <summary>Searches for objects matching <paramref name="term"/>.</summary>
    /// <param name="term">What the user typed. Never interpolated into SQL.</param>
    /// <param name="options">How wide to search and what to rank first.</param>
    /// <param name="cancellationToken">
    /// Cancels the search. Expected to fire on nearly every keystroke, so an
    /// implementation must cancel cheaply.
    /// </param>
    Task<Result<IReadOnlyList<ObjectSearchHit>>> SearchAsync(
        string term,
        ObjectSearchOptions options,
        CancellationToken cancellationToken);
}

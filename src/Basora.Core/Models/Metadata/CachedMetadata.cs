namespace Basora.Core.Models.Metadata;

/// <summary>A cached value together with when it was read.</summary>
/// <remarks>
/// The timestamp travels with the value because the UI is required to say how old what it
/// is showing is. Metadata that silently goes stale is how someone ends up writing a
/// migration against a schema that changed twenty minutes ago.
/// <para>
/// Age is not computed here. Reading the clock inside a value type makes the same entry
/// compare differently at different moments; the caller has the clock and does the
/// subtraction.
/// </para>
/// </remarks>
/// <typeparam name="T">What was cached.</typeparam>
/// <param name="Value">The cached value.</param>
/// <param name="LoadedAt">When it was read from the server.</param>
public sealed record CachedMetadata<T>(T Value, DateTimeOffset LoadedAt);

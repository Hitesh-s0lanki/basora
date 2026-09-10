namespace Basora.Core.Models.Metadata;

/// <summary>Identifies one cached piece of metadata.</summary>
/// <remarks>
/// The session is part of the key and never optional. A reconnect can land on a different
/// server behind a load balancer, and a cache that outlived its session would then be
/// describing a database nobody is looking at.
/// </remarks>
/// <param name="SessionId">The session the metadata was read on.</param>
/// <param name="Target">The object it describes.</param>
/// <param name="Aspect">Which part of that object's metadata it is.</param>
public readonly record struct MetadataCacheKey(Guid SessionId, DbObjectRef Target, MetadataAspect Aspect);

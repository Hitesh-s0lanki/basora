namespace Basora.Core.Models.Metadata;

/// <summary>
/// One row in the object explorer: what to draw, without what it would cost to load.
/// </summary>
/// <remarks>
/// Deliberately shallow. The tree renders thousands of these at sixty frames a second,
/// so a node carries only what a row shows and never the descriptor behind it. Expanding
/// or opening a node is what fetches the rest.
/// </remarks>
public sealed record ObjectNode
{
    /// <summary>What this node points at.</summary>
    public required DbObjectRef Ref { get; init; }

    /// <summary>The label to draw, which is not always the name — a function shows its signature.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The object's comment, shown as a tooltip.</summary>
    public string? Comment { get; init; }

    /// <summary>Whether the node can be expanded, known without loading the children.</summary>
    public bool HasChildren { get; init; }

    /// <summary>
    /// The planner's row estimate, or <see langword="null"/> when it is unknown.
    /// </summary>
    /// <remarks>
    /// An estimate, and shown as one. <c>reltuples</c> is -1 on a relation that has never
    /// been analyzed and stale otherwise, so the readers map any negative value to null
    /// rather than letting "-1 rows" reach a screen.
    /// </remarks>
    public long? EstimatedRows { get; init; }

    /// <summary>Total size on disk including indexes and TOAST, or <see langword="null"/> when not measured.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Short labels drawn on the row, such as <c>PK</c>, <c>FK</c> or <c>unlogged</c>.</summary>
    public IReadOnlyList<string> Badges { get; init; } = [];
}

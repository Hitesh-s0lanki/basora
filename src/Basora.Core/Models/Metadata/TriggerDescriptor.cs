namespace Basora.Core.Models.Metadata;

/// <summary>One trigger on a relation.</summary>
/// <remarks>
/// Triggers that PostgreSQL creates to enforce foreign keys are marked
/// <c>tgisinternal</c> and are excluded by the reader, because listing them tells the
/// user nothing they did not already learn from the constraint.
/// <para>
/// The firing conditions are kept as the catalog's own text rather than parsed into
/// flags. <c>pg_get_triggerdef</c> is exact, survives PostgreSQL adding new options, and
/// is what the user would have typed.
/// </para>
/// </remarks>
public sealed record TriggerDescriptor
{
    /// <summary>The trigger name, unquoted.</summary>
    public required string Name { get; init; }

    /// <summary>The relation the trigger is attached to.</summary>
    public required DbObjectRef Table { get; init; }

    /// <summary>The function the trigger calls.</summary>
    public required string FunctionName { get; init; }

    /// <summary>When it fires, as written: <c>BEFORE</c>, <c>AFTER</c> or <c>INSTEAD OF</c>.</summary>
    public required string Timing { get; init; }

    /// <summary>What it fires on, as written, such as <c>INSERT OR UPDATE</c>.</summary>
    public required string Events { get; init; }

    /// <summary>Whether it fires once per row rather than once per statement.</summary>
    public bool IsRowLevel { get; init; }

    /// <summary>The <c>WHEN</c> clause, when there is one.</summary>
    public string? Condition { get; init; }

    /// <summary>Whether the trigger is currently enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>The full text from <c>pg_get_triggerdef</c>.</summary>
    public string? Definition { get; init; }

    /// <summary>The trigger's comment.</summary>
    public string? Comment { get; init; }
}

using System.Collections.Immutable;

namespace Basora.Core.Tests.Architecture;

/// <summary>
/// The allowed-reference table from docs/02-architecture.md section 3, as data.
/// </summary>
/// <remarks>
/// Kept as a pure function over names so the rule can be exercised against fabricated
/// input as well as against the real assemblies. A rule that has never been shown to
/// fail is not evidence of anything.
/// </remarks>
internal static class LayerGraph
{
    public static ImmutableDictionary<string, ImmutableHashSet<string>> Allowed { get; } =
        new Dictionary<string, ImmutableHashSet<string>>(StringComparer.Ordinal)
        {
            ["Basora.Core"] = [],
            ["Basora.Infrastructure"] = ["Basora.Core"],
            ["Basora.Security"] = ["Basora.Core"],
            ["Basora.Analytics"] = ["Basora.Core"],
            ["Basora.AI"] = ["Basora.Core"],
            ["Basora.PostgreSQL"] = ["Basora.Core", "Basora.Security"],
            ["Basora.UI"] = ["Basora.Core", "Basora.Infrastructure"],
            ["Basora.App"] =
            [
                "Basora.AI",
                "Basora.Analytics",
                "Basora.Core",
                "Basora.Infrastructure",
                "Basora.PostgreSQL",
                "Basora.Security",
                "Basora.UI",
            ],
        }.ToImmutableDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Returns one message per disallowed reference. An assembly missing from the table
    /// is itself a violation: a new project has to declare where it sits.
    /// </summary>
    public static IReadOnlyList<string> Violations(string assemblyName, IEnumerable<string> referenced)
    {
        if (!Allowed.TryGetValue(assemblyName, out ImmutableHashSet<string>? allowed))
        {
            return [$"{assemblyName} is not in the layer table in docs/02-architecture.md section 3."];
        }

        return
        [
            .. referenced
                .Where(r => r.StartsWith("Basora.", StringComparison.Ordinal))
                .Where(r => !allowed.Contains(r))
                .Order(StringComparer.Ordinal)
                .Select(r => $"{assemblyName} must not reference {r}."),
        ];
    }
}

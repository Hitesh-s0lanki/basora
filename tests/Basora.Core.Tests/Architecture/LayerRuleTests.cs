namespace Basora.Core.Tests.Architecture;

/// <summary>
/// The layer rules from docs/02-architecture.md section 3, checked against the compiled
/// assemblies.
/// </summary>
public sealed class LayerRuleTests
{
    [Fact]
    public void SourceAssemblies_ReferenceOnlyWhatTheLayerTableAllows()
    {
        List<string> violations = [];

        foreach (string assembly in BasoraAssemblies.SourceNames)
        {
            violations.AddRange(
                LayerGraph.Violations(assembly, BasoraAssemblies.ReferencedAssembliesOf(assembly)));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Core_ReferencesNothingButThePlatform()
    {
        IReadOnlyList<string> referenced = BasoraAssemblies.ReferencedAssembliesOf("Basora.Core");

        string[] offenders = [.. referenced.Where(r => !BasoraAssemblies.IsPlatformAssembly(r))];

        Assert.Empty(offenders);
    }

    [Fact]
    public void Ui_DoesNotReferencePostgreSql()
    {
        IReadOnlyList<string> referenced = BasoraAssemblies.ReferencedAssembliesOf("Basora.UI");

        Assert.DoesNotContain("Basora.PostgreSQL", referenced);
        Assert.DoesNotContain("Npgsql", referenced);
    }

    [Fact]
    public void OnlyPostgreSql_ReferencesTheNpgsqlDriver()
    {
        string[] offenders =
        [
            .. BasoraAssemblies.SourceNames
                .Where(a => a != "Basora.PostgreSQL")
                .Where(a => BasoraAssemblies.ReferencedAssembliesOf(a)
                    .Any(r => r.StartsWith("Npgsql", StringComparison.Ordinal))),
        ];

        Assert.Empty(offenders);
    }

    [Fact]
    public void NothingReferencesTheApplicationProject()
    {
        string[] offenders =
        [
            .. BasoraAssemblies.SourceNames
                .Where(a => a != "Basora.App")
                .Where(a => BasoraAssemblies.ReferencedAssembliesOf(a).Contains("Basora.App")),
        ];

        Assert.Empty(offenders);
    }

    [Fact]
    public void Analytics_PerformsNoIo()
    {
        string[] ioNamespaces = ["System.IO", "System.Net", "System.Data"];

        string[] offenders =
        [
            .. BasoraAssemblies.ReferencedNamespacesOf("Basora.Analytics")
                .Where(ns => ioNamespaces.Any(io =>
                    ns.Equals(io, StringComparison.Ordinal)
                    || ns.StartsWith(io + ".", StringComparison.Ordinal))),
        ];

        Assert.Empty(offenders);
    }

    // The rule itself has to be shown to bite, or a green suite proves nothing.

    [Fact]
    public void LayerGraph_RejectsUiReferencingPostgreSql()
    {
        IReadOnlyList<string> violations =
            LayerGraph.Violations("Basora.UI", ["Basora.Core", "Basora.PostgreSQL"]);

        Assert.Equal(["Basora.UI must not reference Basora.PostgreSQL."], violations);
    }

    [Fact]
    public void LayerGraph_RejectsAnyReferenceFromCore()
    {
        IReadOnlyList<string> violations = LayerGraph.Violations("Basora.Core", ["Basora.Infrastructure"]);

        Assert.Equal(["Basora.Core must not reference Basora.Infrastructure."], violations);
    }

    [Fact]
    public void LayerGraph_RejectsAProjectThatIsNotInTheTable()
    {
        IReadOnlyList<string> violations = LayerGraph.Violations("Basora.Unlisted", []);

        Assert.Single(violations);
    }

    [Fact]
    public void LayerGraph_AllowsTheDeclaredEdges()
    {
        Assert.Empty(LayerGraph.Violations("Basora.PostgreSQL", ["Basora.Core", "Basora.Security"]));
        Assert.Empty(LayerGraph.Violations("Basora.UI", ["Basora.Core", "Basora.Infrastructure"]));
    }
}

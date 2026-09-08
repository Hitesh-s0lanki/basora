using System.Diagnostics.CodeAnalysis;

namespace Basora.Core.Tests.Architecture;

/// <summary>
/// docs/12-coding-standards.md section 2: every class is sealed unless it is designed for
/// inheritance, and designing for inheritance requires saying why.
/// </summary>
public sealed class SealedTypeTests
{
    [Fact]
    public void PublicClasses_AreSealedOrDocumentAWhy()
    {
        JustificationDocumentation documentation =
            JustificationDocumentation.ForAssemblies(BasoraAssemblies.SourceNames);

        List<string> violations = [];

        foreach (string assembly in BasoraAssemblies.SourceNames)
        {
            violations.AddRange(
                ContractRules.UnsealedTypesWithoutJustification(
                    BasoraAssemblies.PublicTypesOf(assembly),
                    documentation));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Rule_RejectsAnUndocumentedOpenClass()
    {
        IReadOnlyList<string> violations = ContractRules.UnsealedTypesWithoutJustification(
            [typeof(UndocumentedOpenClass)],
            JustificationDocumentation.ForAssemblies([]));

        string violation = Assert.Single(violations);
        Assert.Contains(nameof(UndocumentedOpenClass), violation, StringComparison.Ordinal);
    }

    [Fact]
    public void Rule_AcceptsASealedClass()
    {
        Assert.Empty(ContractRules.UnsealedTypesWithoutJustification(
            [typeof(SealedTypeTests)],
            JustificationDocumentation.ForAssemblies([])));
    }

    [Fact]
    public void Rule_AcceptsAStaticClass()
    {
        Assert.Empty(ContractRules.UnsealedTypesWithoutJustification(
            [typeof(LayerGraph)],
            JustificationDocumentation.ForAssemblies([])));
    }

    [Fact]
    public void Rule_AcceptsAnOpenClassWithADocumentedReason()
    {
        // ViewModelBase is the one open class Basora ships, and its <remarks> says why.
        Type viewModelBase = BasoraAssemblies.PublicTypesOf("Basora.UI")
            .Single(t => t.Name == "ViewModelBase");

        Assert.False(viewModelBase.IsSealed);
        Assert.Empty(ContractRules.UnsealedTypesWithoutJustification(
            [viewModelBase],
            JustificationDocumentation.ForAssemblies(BasoraAssemblies.SourceNames)));
    }

    [SuppressMessage(
        "Performance",
        "CA1852:Seal internal types",
        Justification = "The point of this fixture is to be unsealed and undocumented.")]
    private class UndocumentedOpenClass;
}

using System.Reflection;
using System.Text.RegularExpressions;
using Basora.Core.Errors;

namespace Basora.Core.Tests.Results;

public sealed partial class BasoraErrorCodesTests
{
    [Fact]
    public void EveryCode_IsListedInAll()
    {
        // All is what the other tests, and the fakes in Basora.TestKit, enumerate. A
        // constant that is not in it is invisible to every check here.
        string[] declared =
        [
            .. typeof(BasoraErrorCodes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f is { IsLiteral: true, FieldType.FullName: "System.String" })
                .Select(f => (string)f.GetRawConstantValue()!),
        ];

        Assert.Equal(
            declared.Order(StringComparer.Ordinal),
            BasoraErrorCodes.All.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryCode_IsDistinct()
    {
        string[] duplicates =
        [
            .. BasoraErrorCodes.All
                .GroupBy(code => code, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key),
        ];

        Assert.Empty(duplicates);
    }

    [Fact]
    public void EveryCode_HasTheDocumentedShape()
    {
        string[] malformed = [.. BasoraErrorCodes.All.Where(code => !CodePattern().IsMatch(code))];

        Assert.Empty(malformed);
    }

    [Fact]
    public void TheCodesTheDomainModelNamesExplicitly_StillExist()
    {
        // docs/04-domain-model.md section 11 names these three as the example of the
        // contract. They are the ones most likely to be relied on by name.
        Assert.Contains("basora.connection.refused", BasoraErrorCodes.All);
        Assert.Contains("basora.query.cancelled", BasoraErrorCodes.All);
        Assert.Contains("basora.privilege.denied", BasoraErrorCodes.All);
    }

    /// <summary>basora.&lt;area&gt;.&lt;reason&gt;, lowercase, underscores inside a segment.</summary>
    [GeneratedRegex("^basora(\\.[a-z][a-z_]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}

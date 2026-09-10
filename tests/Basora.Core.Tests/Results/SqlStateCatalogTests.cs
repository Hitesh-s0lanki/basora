using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Basora.Core.Errors;

namespace Basora.Core.Tests.Results;

public sealed partial class SqlStateCatalogTests
{
    private static readonly string[] PrivilegePlaceholders = ["Role", "Privilege", "Object"];

    public static TheoryData<string> SpecifiedSqlStates =>
        [.. SqlStateSpecTable.SqlStates];

    [Theory]
    [MemberData(nameof(SpecifiedSqlStates))]
    public void EverySqlStateInTheSpec_HasACodeAMessageAndARemediation(string sqlState)
    {
        Assert.True(
            SqlStateCatalog.Descriptors.ContainsKey(sqlState),
            $"docs/05-postgresql-data-layer.md section 10 lists {sqlState}, which the catalogue does not map.");

        SqlStateDescriptor descriptor = SqlStateCatalog.Describe(sqlState);

        Assert.Equal(sqlState, descriptor.SqlState);
        Assert.Contains(descriptor.Code, BasoraErrorCodes.All);
        Assert.NotEqual(BasoraErrorCodes.UnknownDatabaseError, descriptor.Code);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Remediation));
    }

    [Fact]
    public void TheCatalogue_MapsNothingTheSpecDoesNotList()
    {
        // Extra entries are not wrong in themselves, but each one is a code the UI may
        // branch on, so it belongs in the spec table before it belongs here.
        string[] unspecified =
        [
            .. SqlStateCatalog.Descriptors.Keys
                .Except(SqlStateSpecTable.SqlStates, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];

        Assert.Empty(unspecified);
    }

    [Theory]
    [InlineData("08006", BasoraErrorCodes.ConnectionFailed)]
    [InlineData("23514", BasoraErrorCodes.ConstraintViolated)]
    [InlineData("25P02", BasoraErrorCodes.InvalidTransactionState)]
    [InlineData("42P07", BasoraErrorCodes.QueryRejected)]
    [InlineData("53200", BasoraErrorCodes.InsufficientResources)]
    [InlineData("55P03", BasoraErrorCodes.ObjectNotReady)]
    [InlineData("XX001", BasoraErrorCodes.InternalServerError)]
    public void AnUnlistedSqlState_DegradesToItsClass(string sqlState, string expectedCode)
    {
        Assert.Equal(expectedCode, SqlStateCatalog.CodeFor(sqlState));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("9")]
    [InlineData("99")]
    [InlineData("99999")]
    [InlineData("not a sqlstate")]
    public void AnUnrecognisedSqlState_DegradesToAGenericCodeWithoutThrowing(string? sqlState)
    {
        SqlStateDescriptor descriptor = SqlStateCatalog.Describe(sqlState);

        Assert.Same(SqlStateCatalog.Unknown, descriptor);
        Assert.Equal(BasoraErrorCodes.UnknownDatabaseError, SqlStateCatalog.CodeFor(sqlState));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.Remediation));
    }

    [Fact]
    public void ClassDescriptors_AreKeyedByTwoCharacterClasses()
    {
        Assert.All(SqlStateCatalog.ClassDescriptors.Keys, key => Assert.Equal(2, key.Length));
    }

    [Fact]
    public void EveryClassDescriptor_UsesADeclaredCode()
    {
        string[] undeclared =
        [
            .. SqlStateCatalog.ClassDescriptors.Values
                .Select(d => d.Code)
                .Except(BasoraErrorCodes.All, StringComparer.Ordinal),
        ];

        Assert.Empty(undeclared);
    }

    [Fact]
    public void EverySqlStateHasAClassToDegradeTo()
    {
        // A state that maps exactly but whose class does not is a latent gap: the next
        // unlisted state in that class would fall all the way to the generic code.
        string[] classless =
        [
            .. SqlStateCatalog.Descriptors.Keys
                .Select(state => state[..2])
                .Distinct(StringComparer.Ordinal)
                .Where(prefix => !SqlStateCatalog.ClassDescriptors.ContainsKey(prefix))
                .Order(StringComparer.Ordinal),
        ];

        Assert.Empty(classless);
    }

    [Fact]
    public void Cancellation_IsRecognisedAndIsNotTreatedAsAnUnknownError()
    {
        Assert.True(SqlStateCatalog.IsCancellation("57014"));
        Assert.False(SqlStateCatalog.IsCancellation("57P01"));
        Assert.False(SqlStateCatalog.IsCancellation(null));

        Assert.Equal(BasoraErrorCodes.QueryCancelled, SqlStateCatalog.CodeFor("57014"));
    }

    [Fact]
    public void Placeholders_AreTheNamesUsedAcrossTheMessageAndTheRemediation()
    {
        SqlStateDescriptor privilege = SqlStateCatalog.Describe("42501");

        Assert.Equal(PrivilegePlaceholders, privilege.Placeholders.ToArray());
    }

    [Fact]
    public void ADescriptorWithNoPlaceholders_RendersAsWritten()
    {
        SqlStateDescriptor cancelled = SqlStateCatalog.Describe("57014");

        Assert.Empty(cancelled.Placeholders);
        Assert.Equal("Cancelled.", cancelled.MessageTemplate);
    }

    [Fact]
    public void NoTemplate_UsesPositionalPlaceholders()
    {
        // {0} is not the syntax SqlStateDescriptor recognises, so it would survive
        // substitution and reach the user as literal braces.
        ImmutableArray<SqlStateDescriptor> all =
        [
            .. SqlStateCatalog.Descriptors.Values,
            .. SqlStateCatalog.ClassDescriptors.Values,
            SqlStateCatalog.Unknown,
        ];

        string[] offenders =
        [
            .. all
                .Where(d => PositionalPlaceholder().IsMatch(d.MessageTemplate + d.Remediation))
                .Select(d => d.SqlState),
        ];

        Assert.Empty(offenders);
    }

    [Fact]
    public void EveryTemplate_ClosesEveryBraceItOpens()
    {
        ImmutableArray<SqlStateDescriptor> all =
        [
            .. SqlStateCatalog.Descriptors.Values,
            .. SqlStateCatalog.ClassDescriptors.Values,
            SqlStateCatalog.Unknown,
        ];

        foreach (SqlStateDescriptor descriptor in all)
        {
            string text = descriptor.MessageTemplate + " " + descriptor.Remediation;

            Assert.Equal(
                text.Count(c => c == '{'),
                descriptor.Placeholders.Sum(name => CountOccurrences(text, "{" + name + "}")));
            Assert.Equal(text.Count(c => c == '{'), text.Count(c => c == '}'));
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        for (int index = text.IndexOf(value, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    [GeneratedRegex(@"\{\d", RegexOptions.CultureInvariant)]
    private static partial Regex PositionalPlaceholder();
}

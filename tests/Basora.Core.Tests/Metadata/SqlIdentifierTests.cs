using System.Globalization;
using System.Text;
using Basora.Core.Models.Metadata;

namespace Basora.Core.Tests.Metadata;

public sealed class SqlIdentifierTests
{
    [Theory]
    [InlineData("users", "\"users\"")]
    [InlineData("Users", "\"Users\"")]
    [InlineData("my table", "\"my table\"")]
    [InlineData("select", "\"select\"")]
    [InlineData("order", "\"order\"")]
    [InlineData("_private", "\"_private\"")]
    [InlineData("Ünïcøde", "\"Ünïcøde\"")]
    public void Quote_AlwaysQuotes_EvenWhenItLooksUnnecessary(string identifier, string expected)
    {
        Assert.Equal(expected, SqlIdentifier.Quote(identifier));
    }

    [Theory]
    [InlineData("wei\"rd", "\"wei\"\"rd\"")]
    [InlineData("\"", "\"\"\"\"")]
    [InlineData("\"\"", "\"\"\"\"\"\"")]
    [InlineData("a\"b\"c", "\"a\"\"b\"\"c\"")]
    public void Quote_DoublesEveryEmbeddedQuote(string identifier, string expected)
    {
        Assert.Equal(expected, SqlIdentifier.Quote(identifier));
    }

    [Theory]
    [InlineData("\0")]
    [InlineData("users\0")]
    [InlineData("us\0ers")]
    public void Quote_RejectsANullByte(string identifier)
    {
        // PostgreSQL cannot store one, so there is no correct output. Returning something
        // plausible would mean generating SQL that names an object that cannot exist.
        ArgumentException thrown = Assert.ThrowsAny<ArgumentException>(() => SqlIdentifier.Quote(identifier));

        Assert.Contains("null byte", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Quote_RejectsAnAbsentIdentifier(string? identifier)
    {
        Assert.ThrowsAny<ArgumentException>(() => SqlIdentifier.Quote(identifier!));
    }

    [Fact]
    public void QuoteQualified_QuotesBothPartsSeparately()
    {
        Assert.Equal("\"public\".\"orders\"", SqlIdentifier.QuoteQualified("public", "orders"));
        Assert.Equal("\"my schema\".\"my table\"", SqlIdentifier.QuoteQualified("my schema", "my table"));
        Assert.Equal("\"s\"\"a\".\"t\"\"b\"", SqlIdentifier.QuoteQualified("s\"a", "t\"b"));
    }

    [Fact]
    public void QuoteQualified_LeavesTheNameAloneWhenThereIsNoSchema()
    {
        Assert.Equal("\"orders\"", SqlIdentifier.QuoteQualified(null, "orders"));
    }

    [Fact]
    public void QuoteQualified_NeverProducesAnUnquotedDotThatCouldSplitTheName()
    {
        // A schema or table whose name contains a dot must not become two identifiers.
        string quoted = SqlIdentifier.QuoteQualified("a.b", "c.d");

        Assert.Equal("\"a.b\".\"c.d\"", quoted);
    }

    [Theory]
    [InlineData("plain", "'plain'")]
    [InlineData("it's", "'it''s'")]
    [InlineData("''", "''''''")]
    [InlineData("", "''")]
    public void QuoteLiteral_DoublesEveryApostrophe(string value, string expected)
    {
        Assert.Equal(expected, SqlIdentifier.QuoteLiteral(value));
    }

    [Fact]
    public void QuoteLiteral_RejectsANullByte()
    {
        Assert.ThrowsAny<ArgumentException>(() => SqlIdentifier.QuoteLiteral("a\0b"));
    }

    [Fact]
    public void WouldBeTruncated_MeasuresBytesRatherThanCharacters()
    {
        Assert.False(SqlIdentifier.WouldBeTruncated(new string('a', 63)));
        Assert.True(SqlIdentifier.WouldBeTruncated(new string('a', 64)));

        // Each of these is one character and two bytes, so 32 of them fit and 33 do not.
        Assert.False(SqlIdentifier.WouldBeTruncated(new string('é', 31)));
        Assert.True(SqlIdentifier.WouldBeTruncated(new string('é', 32)));
    }

    // The property: for any string PostgreSQL could hold as a name, Quote produces a
    // valid quoted identifier that reads back as exactly that name.

    [Fact]
    public void Quote_ProducesAValidIdentifierThatReadsBackUnchanged_ForAnyInput()
    {
        List<string> failures = [];

        foreach (string name in Names())
        {
            string quoted = SqlIdentifier.Quote(name);
            string? readBack = QuotedIdentifier.Unquote(quoted);

            if (readBack is null)
            {
                failures.Add($"{Describe(name)} quoted to {quoted}, which is not a valid quoted identifier.");
            }
            else if (!string.Equals(readBack, name, StringComparison.Ordinal))
            {
                failures.Add($"{Describe(name)} quoted to {quoted}, which reads back as {Describe(readBack)}.");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void TheProperty_IsCheckedAgainstEnoughCases_AndTheReaderCanFail()
    {
        // A property test that generated three inputs, or whose oracle accepted anything,
        // would pass silently. Both halves are pinned here.
        Assert.True(Names().Count > 1000);

        Assert.Null(QuotedIdentifier.Unquote("users"));
        Assert.Null(QuotedIdentifier.Unquote("\"un\"balanced\""));
        Assert.Null(QuotedIdentifier.Unquote("\"\""));
        Assert.Equal("a\"b", QuotedIdentifier.Unquote("\"a\"\"b\""));
    }

    /// <summary>The adversarial corpus, then a deterministic random sweep over it.</summary>
    private static List<string> Names()
    {
        List<string> names =
        [
            "a",
            "users",
            "SELECT",
            "\"",
            "\"\"",
            "\"\"\"",
            "a\"b",
            "'",
            "';DROP TABLE users;--",
            "--",
            "/*",
            "$$",
            ".",
            "..",
            "a.b",
            " ",
            "\t",
            "\n",
            "\r\n",
            "\\",
            "\\\"",
            "%s",
            "{0}",
            "Ünïcøde",
            "日本語",
            "🙂",
            "🙂\"🙂",
            "́",
            "﻿",
            new string('a', 63),
            new string('a', 200),
        ];

        // Seeded, so a failure is reproducible rather than a flake someone reruns away.
        Random random = new(20260910);
        const string alphabet = "ab\"'.\\ \t\n;-/*$_ÜéЖ日🙂";

        for (int iteration = 0; iteration < 2000; iteration++)
        {
            int length = random.Next(1, 24);
            StringBuilder generated = new(length);

            for (int index = 0; index < length; index++)
            {
                generated.Append(alphabet[random.Next(alphabet.Length)]);
            }

            names.Add(generated.ToString());
        }

        return names;
    }

    private static string Describe(string value) =>
        string.Create(CultureInfo.InvariantCulture, $"[{string.Join(' ', value.Select(c => ((int)c).ToString("X4", CultureInfo.InvariantCulture)))}]");
}

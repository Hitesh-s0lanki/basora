using System.Text;

namespace Basora.Core.Models.Metadata;

/// <summary>
/// The one place Basora turns a name into an identifier that is safe to put in SQL.
/// </summary>
/// <remarks>
/// docs/05-postgresql-data-layer.md section 6. Two rules carry the whole design.
/// <para>
/// Quote <b>always</b>, not only when it looks necessary. Deciding when to quote means
/// keeping a keyword list, a folding rule and a character class in agreement forever, and
/// that is exactly where injection bugs and "table not found" reports come from.
/// <c>users</c> becoming <c>"users"</c> is uglier in generated SQL and it is always
/// correct.
/// </para>
/// <para>
/// Never build a value this way. Values are parameters. The only text that is ever
/// interpolated is generated DDL echoed back from <c>pg_get_expr</c> for the user to
/// review before running.
/// </para>
/// </remarks>
public static class SqlIdentifier
{
    /// <summary>
    /// The longest identifier PostgreSQL stores, in bytes, with the default
    /// <c>NAMEDATALEN</c> of 64.
    /// </summary>
    public const int MaxLengthBytes = 63;

    /// <summary>
    /// Quotes <paramref name="identifier"/> for use in SQL, doubling any embedded
    /// quote. Always quotes.
    /// </summary>
    /// <param name="identifier">The name as it exists in the database, unquoted.</param>
    /// <returns>The identifier wrapped in double quotes.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="identifier"/> is empty, or contains a null byte. PostgreSQL has no
    /// way to express either, so there is no correct output to return.
    /// </exception>
    public static string Quote(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        if (identifier.Contains('\0', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An identifier cannot contain a null byte. PostgreSQL cannot store one, so a name "
                    + "containing one did not come from the database.",
                nameof(identifier));
        }

        StringBuilder quoted = new(identifier.Length + 2);
        quoted.Append('"');

        foreach (char character in identifier)
        {
            if (character == '"')
            {
                quoted.Append('"');
            }

            quoted.Append(character);
        }

        return quoted.Append('"').ToString();
    }

    /// <summary>
    /// Quotes a possibly schema-qualified name, as <c>"schema"."name"</c> or just
    /// <c>"name"</c> when there is no schema.
    /// </summary>
    /// <param name="schema">The schema, or <see langword="null"/> to leave the name unqualified.</param>
    /// <param name="name">The object's own name.</param>
    /// <exception cref="ArgumentException">Either part is empty or contains a null byte.</exception>
    public static string QuoteQualified(string? schema, string name) =>
        schema is null ? Quote(name) : Quote(schema) + "." + Quote(name);

    /// <summary>
    /// Quotes a string literal by doubling any embedded apostrophe.
    /// </summary>
    /// <remarks>
    /// A last resort, and never for a value that came from the user. Values are bound as
    /// parameters; this exists for the generated DDL that has to read back as a script.
    /// </remarks>
    /// <param name="value">The literal text.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> contains a null byte.</exception>
    public static string QuoteLiteral(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Contains('\0', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A string literal cannot contain a null byte.",
                nameof(value));
        }

        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    /// <summary>
    /// Whether PostgreSQL would truncate <paramref name="identifier"/>, which happens at
    /// <see cref="MaxLengthBytes"/> bytes of UTF-8 and is silent.
    /// </summary>
    /// <remarks>
    /// Basora does not truncate on the caller's behalf. A truncated identifier names a
    /// different object, and doing that quietly inside a DDL generator is how a script
    /// ends up altering the wrong table.
    /// </remarks>
    /// <param name="identifier">The name to measure.</param>
    public static bool WouldBeTruncated(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        return Encoding.UTF8.GetByteCount(identifier) > MaxLengthBytes;
    }
}

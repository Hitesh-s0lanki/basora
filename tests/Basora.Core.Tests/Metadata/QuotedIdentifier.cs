namespace Basora.Core.Tests.Metadata;

/// <summary>
/// PostgreSQL's own rule for a quoted identifier, written independently of the code
/// under test.
/// </summary>
/// <remarks>
/// A property test is only worth the name if the property is stated separately from the
/// implementation. Asserting that Quote's output round-trips through the same escaping
/// logic Quote used would pass for any consistent bug, so this reader is written from the
/// grammar: a quoted identifier is a double quote, a body in which every double quote
/// appears doubled, and a closing double quote.
/// </remarks>
internal static class QuotedIdentifier
{
    /// <summary>
    /// Reads a quoted identifier back to the name it denotes, or returns
    /// <see langword="null"/> when the text is not one.
    /// </summary>
    public static string? Unquote(string text)
    {
        if (text.Length < 3 || text[0] != '"' || text[^1] != '"')
        {
            return null;
        }

        string body = text[1..^1];
        System.Text.StringBuilder name = new(body.Length);

        for (int index = 0; index < body.Length; index++)
        {
            if (body[index] != '"')
            {
                name.Append(body[index]);
                continue;
            }

            // A quote inside the body is only legal as part of a doubled pair.
            if (index + 1 >= body.Length || body[index + 1] != '"')
            {
                return null;
            }

            name.Append('"');
            index++;
        }

        return name.Length == 0 ? null : name.ToString();
    }
}

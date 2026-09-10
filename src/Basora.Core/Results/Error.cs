using System.Diagnostics.CodeAnalysis;

namespace Basora.Core.Results;

/// <summary>
/// An expected failure, in the shape the UI can act on: a stable code to branch on, one
/// sentence to show, and optional detail, remediation and provenance.
/// </summary>
/// <param name="Code">
/// A stable <c>basora.*</c> code from <see cref="Errors.BasoraErrorCodes"/>. Never a
/// server code and never localised — this is what the UI keys off.
/// </param>
/// <param name="Message">The sentence to show the user. Already resolved, not a template.</param>
/// <param name="Detail">Supporting detail, shown when the user asks for it.</param>
/// <param name="Hint">What the user can do about it. Usually the descriptor's remediation, resolved.</param>
/// <param name="SqlState">The raw PostgreSQL SQLSTATE when the failure came from the server.</param>
/// <param name="Exception">
/// The originating exception, for the log. Never rendered: raw exception text is what
/// this type exists to replace (docs/12-coding-standards.md section 8).
/// </param>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Error is the name docs/04-domain-model.md section 11 gives this type, and it "
        + "reads correctly at every call site. The rule is about consumption from Visual Basic, "
        + "which is not a language Basora is consumed from.")]
public sealed record Error(
    string Code,
    string Message,
    string? Detail = null,
    string? Hint = null,
    string? SqlState = null,
    Exception? Exception = null)
{
    private readonly string _code = RequireText(Code, nameof(Code));
    private readonly string _message = RequireText(Message, nameof(Message));

    /// <summary>The stable <c>basora.*</c> code. Never empty.</summary>
    public string Code
    {
        get => _code;
        init => _code = RequireText(value, nameof(Code));
    }

    /// <summary>The sentence to show the user. Never empty.</summary>
    public string Message
    {
        get => _message;
        init => _message = RequireText(value, nameof(Message));
    }

    private static string RequireText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}

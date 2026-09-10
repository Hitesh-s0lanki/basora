using Basora.Core.Errors;

namespace Basora.Core.Results;

/// <summary>
/// The failure that <c>default(Result)</c> and <c>default(Result{T})</c> carry.
/// </summary>
/// <remarks>
/// A struct's zero value is reachable without going through a factory method, so the
/// two result types define it as a failure rather than leaving it as a success with no
/// value. Reaching this error in a running application means a result was declared and
/// never assigned — a bug, which is why the message says so rather than trying to be
/// useful to the person at the keyboard.
/// </remarks>
internal static class UninitialisedResult
{
    internal static Error Instance { get; } = new(
        BasoraErrorCodes.UninitialisedResult,
        "The operation did not report an outcome.",
        Detail: "A Result was read before anything assigned to it. This is a defect in Basora, not a "
            + "problem with the database or the statement.",
        Hint: "Retry the operation. If it keeps happening, please report it.");
}

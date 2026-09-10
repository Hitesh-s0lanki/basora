using Basora.Core.Errors;

namespace Basora.Core.Results;

/// <summary>
/// The outcome of an operation that either succeeds or fails with an <see cref="Error"/>
/// and has no value to return.
/// </summary>
/// <remarks>
/// Expected failures come back as a <c>Result</c>; exceptions stay for bugs and truly
/// exceptional conditions (docs/02-architecture.md section 4.4).
/// <para>
/// A struct has a zero value nobody constructs, so <c>default</c> is defined as a
/// failure carrying <see cref="BasoraErrorCodes.UninitialisedResult"/>. That keeps the
/// invariant total: a value of this type is a success, or it is a failure with a
/// non-null error. It can never be a success that also carries an error, a failure with
/// no error, or an uninitialised value that reads as success.
/// </para>
/// </remarks>
public readonly record struct Result
{
    private readonly Error? _error;
    private readonly bool _isSuccess;

    private Result(bool isSuccess, Error? error)
    {
        _isSuccess = isSuccess;
        _error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>Whether the operation failed. Always the negation of <see cref="IsSuccess"/>.</summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>The failure, or <see langword="null"/> on success. Never null on failure.</summary>
    public Error? Error => _isSuccess ? null : _error ?? UninitialisedResult.Instance;

    /// <summary>A success.</summary>
    public static Result Ok() => new(isSuccess: true, error: null);

    /// <summary>A failure carrying <paramref name="error"/>.</summary>
    /// <param name="error">The failure. Required.</param>
    public static Result Fail(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(isSuccess: false, error);
    }

    /// <summary>
    /// Reads the failure without a nullable check, so a caller that has already tested
    /// <see cref="IsFailure"/> does not need the null-forgiving operator to propagate it.
    /// </summary>
    /// <param name="error">The failure, when this is one.</param>
    /// <returns><see langword="true"/> when this is a failure.</returns>
    public bool TryGetError(out Error error)
    {
        // Error is non-null on every failure by construction, and the caller is told by the
        // return value whether this is one.
        error = Error!;
        return IsFailure;
    }

    /// <summary>
    /// Restates this failure as a <see cref="Result{T}"/>, for propagating one operation's
    /// failure out of another that returns a value.
    /// </summary>
    /// <typeparam name="T">The value type of the result being returned.</typeparam>
    /// <exception cref="InvalidOperationException">This is a success, so there is no failure to restate.</exception>
    public Result<T> ToFailure<T>() => TryGetError(out Error error)
        ? Result<T>.Fail(error)
        : throw new InvalidOperationException("A successful Result has no failure to restate.");
}

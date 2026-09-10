using System.Diagnostics.CodeAnalysis;
using Basora.Core.Errors;

namespace Basora.Core.Results;

/// <summary>
/// The outcome of an operation that either succeeds with a <typeparamref name="T"/> or
/// fails with an <see cref="Error"/>.
/// </summary>
/// <typeparam name="T">The value a successful operation produces.</typeparam>
/// <remarks>
/// The invariant is total, including for <c>default</c>, which is a failure carrying
/// <see cref="BasoraErrorCodes.UninitialisedResult"/>. There is no way to reach a success
/// that carries an error, or a failure that carries a value: <see cref="Ok"/> takes no
/// error, <see cref="Fail"/> takes no value, and <see cref="Value"/> reads as
/// <see langword="default"/> on any failure.
/// </remarks>
[SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Ok and Fail are the type's only constructors, and the shape is fixed by "
        + "docs/04-domain-model.md section 11. Result<T>.Ok(value) infers T; a non-generic factory "
        + "would not be able to.")]
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;
    private readonly bool _isSuccess;

    private Result(bool isSuccess, T? value, Error? error)
    {
        _isSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>Whether the operation succeeded.</summary>
    public bool IsSuccess => _isSuccess;

    /// <summary>Whether the operation failed. Always the negation of <see cref="IsSuccess"/>.</summary>
    public bool IsFailure => !_isSuccess;

    /// <summary>
    /// The value on success, <see langword="default"/> on failure. Prefer
    /// <see cref="TryGetValue"/>, which tells the two apart without a null check that is
    /// wrong for a value type.
    /// </summary>
    public T? Value => _isSuccess ? _value : default;

    /// <summary>The failure, or <see langword="null"/> on success. Never null on failure.</summary>
    public Error? Error => _isSuccess ? null : _error ?? UninitialisedResult.Instance;

    /// <summary>A success carrying <paramref name="value"/>.</summary>
    /// <param name="value">
    /// The value. Not null-checked: <typeparamref name="T"/> may legitimately be a
    /// nullable type, and the runtime cannot tell <c>Result{string}</c> from
    /// <c>Result{string?}</c>.
    /// </param>
    public static Result<T> Ok(T value) => new(isSuccess: true, value, error: null);

    /// <summary>A failure carrying <paramref name="error"/>.</summary>
    /// <param name="error">The failure. Required.</param>
    public static Result<T> Fail(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(isSuccess: false, value: default, error);
    }

    /// <summary>Reads the value, and says whether there was one.</summary>
    /// <param name="value">The value, when this is a success.</param>
    /// <returns><see langword="true"/> when this is a success.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        // _value is only ever null on a failure, and MaybeNullWhen tells the caller's flow
        // analysis that reading it is only valid once the return value has been checked.
        value = _value!;
        return _isSuccess;
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

    /// <summary>Discards the value, keeping only whether the operation succeeded and why not.</summary>
    public Result WithoutValue() => TryGetError(out Error error) ? Result.Fail(error) : Result.Ok();

    /// <summary>
    /// Restates this failure as a <see cref="Result{TOther}"/>, for propagating one
    /// operation's failure out of another that returns a different value.
    /// </summary>
    /// <typeparam name="TOther">The value type of the result being returned.</typeparam>
    /// <exception cref="InvalidOperationException">This is a success, so there is no failure to restate.</exception>
    public Result<TOther> ToFailure<TOther>() => TryGetError(out Error error)
        ? Result<TOther>.Fail(error)
        : throw new InvalidOperationException("A successful Result<T> has no failure to restate.");
}

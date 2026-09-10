using Basora.Core.Errors;
using Basora.Core.Results;

namespace Basora.Core.Tests.Results;

public sealed class ResultTests
{
    private static readonly Error Failure = new(BasoraErrorCodes.ConnectionRefused, "The connection was refused.");

    [Fact]
    public void Ok_IsASuccessWithNoError()
    {
        Result result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_IsAFailureCarryingTheError()
    {
        Result result = Result.Fail(Failure);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(Failure, result.Error);
    }

    [Fact]
    public void Fail_RejectsANullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Fail(null!));
    }

    [Fact]
    public void Default_IsAFailureCarryingTheUninitialisedError()
    {
        // The zero value of a struct is reachable without a factory method, so it has to
        // be a state the invariant covers. Failing closed is the only safe choice.
        Result result = default;

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(BasoraErrorCodes.UninitialisedResult, result.Error.Code);
    }

    [Fact]
    public void TryGetError_YieldsTheErrorOnlyOnAFailure()
    {
        Assert.True(Result.Fail(Failure).TryGetError(out Error onFailure));
        Assert.Same(Failure, onFailure);

        Assert.False(Result.Ok().TryGetError(out _));
    }

    [Fact]
    public void ToFailure_CarriesTheErrorIntoAResultOfT()
    {
        Result<int> propagated = Result.Fail(Failure).ToFailure<int>();

        Assert.True(propagated.IsFailure);
        Assert.Same(Failure, propagated.Error);
    }

    [Fact]
    public void ToFailure_RefusesToInventAFailureFromASuccess()
    {
        Assert.Throws<InvalidOperationException>(() => Result.Ok().ToFailure<int>());
    }

    [Fact]
    public void Result_ComparesByValue()
    {
        Assert.Equal(Result.Ok(), Result.Ok());
        Assert.Equal(Result.Fail(Failure), Result.Fail(Failure));
        Assert.NotEqual(Result.Ok(), Result.Fail(Failure));
    }
}

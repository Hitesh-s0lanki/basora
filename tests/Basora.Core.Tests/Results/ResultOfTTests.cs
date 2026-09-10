using System.Reflection;
using Basora.Core.Errors;
using Basora.Core.Results;

namespace Basora.Core.Tests.Results;

public sealed class ResultOfTTests
{
    private static readonly Error Failure = new(BasoraErrorCodes.UndefinedTable, "The table public.orders does not exist.");

    [Fact]
    public void Ok_IsASuccessWithTheValueAndNoError()
    {
        Result<string> result = Result<string>.Ok("orders");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("orders", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_IsAFailureWithTheErrorAndNoValue()
    {
        Result<string> result = Result<string>.Fail(Failure);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Same(Failure, result.Error);
    }

    [Fact]
    public void Fail_LeavesAValueTypeAtItsDefaultRatherThanCarryingOne()
    {
        Result<int> result = Result<int>.Fail(Failure);

        Assert.Equal(0, result.Value);
        Assert.False(result.TryGetValue(out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Ok_AcceptsTheDefaultOfAValueTypeAsAGenuineValue()
    {
        // Result<int>.Ok(0) is a success carrying zero, not a disguised failure. This is
        // why success is tracked by its own flag rather than inferred from the value.
        Result<int> result = Result<int>.Ok(0);

        Assert.True(result.IsSuccess);
        Assert.True(result.TryGetValue(out int value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void Fail_RejectsANullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Fail(null!));
    }

    [Fact]
    public void Default_IsAFailureCarryingTheUninitialisedError()
    {
        Result<string> result = default;

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(BasoraErrorCodes.UninitialisedResult, result.Error.Code);
        Assert.False(result.TryGetValue(out _));
    }

    [Fact]
    public void TryGetValue_YieldsTheValueOnlyOnASuccess()
    {
        Assert.True(Result<string>.Ok("orders").TryGetValue(out string? onSuccess));
        Assert.Equal("orders", onSuccess);

        Assert.False(Result<string>.Fail(Failure).TryGetValue(out _));
    }

    [Fact]
    public void WithoutValue_KeepsTheOutcomeAndDropsTheValue()
    {
        Assert.True(Result<string>.Ok("orders").WithoutValue().IsSuccess);

        Result dropped = Result<string>.Fail(Failure).WithoutValue();
        Assert.True(dropped.IsFailure);
        Assert.Same(Failure, dropped.Error);
    }

    [Fact]
    public void ToFailure_CarriesTheErrorIntoADifferentValueType()
    {
        Result<int> propagated = Result<string>.Fail(Failure).ToFailure<int>();

        Assert.True(propagated.IsFailure);
        Assert.Same(Failure, propagated.Error);
    }

    [Fact]
    public void ToFailure_RefusesToInventAFailureFromASuccess()
    {
        Assert.Throws<InvalidOperationException>(() => Result<string>.Ok("orders").ToFailure<int>());
    }

    // The acceptance criterion is that an invalid state cannot be constructed at all.
    // The tests above show the factory methods never produce one; these two show there is
    // no other way in — no public constructor, and nothing settable afterwards, so `with`
    // cannot reintroduce what the factories rule out.

    [Theory]
    [InlineData(typeof(Result))]
    [InlineData(typeof(Result<string>))]
    public void ResultTypes_ExposeNoPublicConstructor(Type resultType)
    {
        ConstructorInfo[] constructors = resultType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Empty(constructors);
    }

    [Theory]
    [InlineData(typeof(Result))]
    [InlineData(typeof(Result<string>))]
    public void ResultTypes_ExposeNoSettableMember(Type resultType)
    {
        string[] settable =
        [
            .. resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod is not null)
                .Select(p => p.Name),
            .. resultType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly)
                .Select(f => f.Name),
        ];

        Assert.Empty(settable);
    }
}

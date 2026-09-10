using Basora.Core.Errors;
using Basora.Core.Results;

namespace Basora.Core.Tests.Results;

public sealed class ErrorTests
{
    [Fact]
    public void Error_KeepsEveryFieldItWasGiven()
    {
        var exception = new InvalidOperationException("boom");

        var error = new Error(
            BasoraErrorCodes.PrivilegeDenied,
            "The role app lacks SELECT on public.orders.",
            Detail: "Reported while opening the table.",
            Hint: "GRANT SELECT ON public.orders TO app;",
            SqlState: "42501",
            Exception: exception);

        Assert.Equal(BasoraErrorCodes.PrivilegeDenied, error.Code);
        Assert.Equal("The role app lacks SELECT on public.orders.", error.Message);
        Assert.Equal("Reported while opening the table.", error.Detail);
        Assert.Equal("GRANT SELECT ON public.orders TO app;", error.Hint);
        Assert.Equal("42501", error.SqlState);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void Error_DefaultsTheOptionalFieldsToNull()
    {
        var error = new Error(BasoraErrorCodes.ConnectionRefused, "The connection was refused.");

        Assert.Null(error.Detail);
        Assert.Null(error.Hint);
        Assert.Null(error.SqlState);
        Assert.Null(error.Exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Error_RejectsACodeThatIsNotText(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(code!, "A message."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Error_RejectsAMessageThatIsNotText(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(BasoraErrorCodes.ConnectionRefused, message!));
    }

    [Fact]
    public void Error_ValidatesOnWithAsWellAsOnConstruction()
    {
        var error = new Error(BasoraErrorCodes.ConnectionRefused, "The connection was refused.");

        Assert.ThrowsAny<ArgumentException>(() => error with { Code = " " });
        Assert.ThrowsAny<ArgumentException>(() => error with { Message = "" });
    }

    [Fact]
    public void Error_ComparesByValue()
    {
        var first = new Error(BasoraErrorCodes.Deadlock, "Deadlocked.", SqlState: "40P01");
        var second = new Error(BasoraErrorCodes.Deadlock, "Deadlocked.", SqlState: "40P01");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}

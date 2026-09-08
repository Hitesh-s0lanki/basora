using System.Diagnostics.CodeAnalysis;

namespace Basora.Core.Tests.Architecture;

/// <summary>
/// docs/02-architecture.md section 4.3: every public async method on a
/// <c>Basora.Core</c> interface takes a <see cref="CancellationToken"/>. No exceptions.
/// </summary>
public sealed class CoreContractTests
{
    [Fact]
    public void CoreInterfaces_TakeACancellationTokenOnEveryAsyncMethod()
    {
        IEnumerable<Type> contracts = BasoraAssemblies.PublicTypesOf("Basora.Core").Where(t => t.IsInterface);

        IReadOnlyList<string> violations = ContractRules.AsyncMethodsMissingCancellationToken(contracts);

        Assert.Empty(violations);
    }

    // Basora.Core is still empty, so the test above is vacuous today. These pin the rule
    // itself down, and they are what make the test above meaningful the moment T-F04
    // lands the first contract.

    [Fact]
    public void Rule_RejectsAnAsyncMethodWithNoCancellationToken()
    {
        IReadOnlyList<string> violations =
            ContractRules.AsyncMethodsMissingCancellationToken([typeof(IUncancellableContract)]);

        string violation = Assert.Single(violations);
        Assert.Contains(nameof(IUncancellableContract.GetValueAsync), violation, StringComparison.Ordinal);
    }

    [Fact]
    public void Rule_RejectsACancellationTokenThatIsNotLast()
    {
        IReadOnlyList<string> violations =
            ContractRules.AsyncMethodsMissingCancellationToken([typeof(IMisorderedContract)]);

        Assert.Single(violations);
    }

    [Fact]
    public void Rule_RejectsACancellationTokenWithADefaultValue()
    {
        IReadOnlyList<string> violations =
            ContractRules.AsyncMethodsMissingCancellationToken([typeof(IOptionalTokenContract)]);

        string violation = Assert.Single(violations);
        Assert.Contains("default value", violation, StringComparison.Ordinal);
    }

    [Fact]
    public void Rule_AcceptsAWellFormedContract()
    {
        Assert.Empty(ContractRules.AsyncMethodsMissingCancellationToken([typeof(IWellFormedContract)]));
    }

    [Fact]
    public void Rule_IgnoresSynchronousMethods()
    {
        Assert.Empty(ContractRules.AsyncMethodsMissingCancellationToken([typeof(ISynchronousContract)]));
    }

    private interface IUncancellableContract
    {
        Task<int> GetValueAsync(string key);
    }

    [SuppressMessage(
        "Design",
        "CA1068:CancellationToken parameters must come last",
        Justification = "The point of this fixture is to violate the rule so the test can catch it.")]
    private interface IMisorderedContract
    {
        Task GetValueAsync(CancellationToken cancellationToken, string key);
    }

    private interface IOptionalTokenContract
    {
        Task GetValueAsync(string key, CancellationToken cancellationToken = default);
    }

    private interface IWellFormedContract
    {
        Task<int> GetValueAsync(string key, CancellationToken cancellationToken);

        ValueTask<int> CountAsync(CancellationToken cancellationToken);

        IAsyncEnumerable<int> StreamAsync(string key, CancellationToken cancellationToken);
    }

    private interface ISynchronousContract
    {
        int GetValue(string key);
    }
}

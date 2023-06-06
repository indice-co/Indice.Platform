namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Abstracts a rule that is executed by the risk engine.</summary>
/// <typeparam name="TTransaction">The type of transaction that the engine manages.</typeparam>
public interface IRule<TTransaction> where TTransaction : TransactionBase
{
    /// <summary>The name of the rule.</summary>
    public string Name { get; }
    /// <summary>Executes the rule asynchronously.</summary>
    /// <param name="transaction">The transaction occurred.</param>
    /// <returns>The result after rule execution.</returns>
    ValueTask<RuleExecutionResult> ExecuteAsync(TTransaction transaction);
}

using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Abstracts a rule that is executed by the risk engine.</summary>
public abstract class RiskRuleBase
{
    /// <summary>Creates a new instance of <see cref="RiskRuleBase"/>.</summary>
    /// <param name="ruleName">The name of the rule.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskRuleBase(string ruleName) {
        Name = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
    }

    /// <summary>The name of the rule.</summary>
    public string Name { get; }
    
    /// <summary>Executes the rule asynchronously.</summary>
    /// <param name="event">The event occurred.</param>
    /// <returns>The result of rule execution.</returns>
    public abstract ValueTask<RuleExecutionResult> ExecuteAsync(DbRiskEvent @event);
}

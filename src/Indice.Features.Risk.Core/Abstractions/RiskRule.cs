using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;

namespace Indice.Features.Risk.Core.Abstractions;

/// <summary>Abstracts a rule that is executed by the risk engine.</summary>
public abstract class RiskRule
{
    /// <summary>Creates a new instance of <see cref="RiskRule"/>.</summary>
    /// <param name="ruleName">The name of the rule.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RiskRule(string ruleName) {
        Name = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
    }

    /// <summary>The name of the rule. Also used as a unique RuleId in the system.</summary>
    public string Name { get; }

    /// <summary>The options of the rule.</summary>
    public RuleOptions Options { get; set; }

    /// <summary>Executes the rule asynchronously.</summary>
    /// <param name="event">The event occurred.</param>
    /// <returns>The result of rule execution.</returns>
    public abstract ValueTask<RuleExecutionResult> ExecuteAsync(RiskEvent @event);
}
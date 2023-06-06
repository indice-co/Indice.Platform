namespace Indice.Features.Risk.Core;

/// <summary>Describes the result that was created after executing all the rules registered in the system.</summary>
public class OverallRuleExecutionResult
{
    internal OverallRuleExecutionResult(
        int numberOfRulesExecuted,
        IEnumerable<RuleExecutionResult>? results
    ) {
        NumberOfRulesExecuted = numberOfRulesExecuted;
        Results = results ?? new List<RuleExecutionResult>();
    }

    /// <summary>The total number of rules executed.</summary>
    public int NumberOfRulesExecuted { get; }
    /// <summary>The result of each individual rule run by the engine.</summary>
    public IEnumerable<RuleExecutionResult> Results { get; }
}

/// <summary>Describes the result that was calculated after executing an individual rule registered in the system.</summary>
public class RuleExecutionResult
{
    /// <summary>Creates a new instance of <see cref="RuleExecutionResult"/> class.</summary>
    /// <param name="riskLevel">Describes the risk level that came up after a rule run by the engine.</param>
    /// <param name="reason">A possible reason usually when we have a <see cref="RiskLevel.High"/> or <see cref="RiskLevel.Medium"/> risk rule.</param>
    public RuleExecutionResult(RiskLevel? riskLevel, string? reason) {
        RiskLevel = riskLevel;
        Reason = reason;
    }

    /// <summary>Creates a new instance of <see cref="RuleExecutionResult"/> class.</summary>
    /// <param name="riskLevel">Describes the risk level that came up after a rule run by the engine.</param>
    public RuleExecutionResult(RiskLevel? riskLevel) : this(riskLevel, null) { }

    /// <summary>Describes the risk level that came up after a rule run by the engine.</summary>
    public RiskLevel? RiskLevel { get; }
    /// <summary>A possible reason usually when we have a <see cref="RiskLevel.High"/> or <see cref="RiskLevel.Medium"/> risk rule.</summary>
    public string? Reason { get; }

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.High"/> risk level.</summary>
    /// <param name="reason">A possible reason usually when we have a <see cref="RiskLevel.High"/> or <see cref="RiskLevel.Medium"/> risk rule.</param>
    public static RuleExecutionResult HighRisk(string reason) => new(Core.RiskLevel.High, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Medium"/> risk level.</summary>
    /// <param name="reason">A possible reason usually when we have a <see cref="RiskLevel.High"/> or <see cref="RiskLevel.Medium"/> risk rule.</param>
    public static RuleExecutionResult MediumRisk(string reason) => new(Core.RiskLevel.Medium, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Low"/> risk level.</summary>
    public static RuleExecutionResult LowRisk() => new(Core.RiskLevel.Low);
}

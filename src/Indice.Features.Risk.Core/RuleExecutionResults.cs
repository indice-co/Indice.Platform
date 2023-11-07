using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;

namespace Indice.Features.Risk.Core;

/// <summary>Describes the result that was created after executing all the rules registered in the system.</summary>
public class AggregateRuleExecutionResult
{
    internal AggregateRuleExecutionResult(Guid transactionId, int numberOfRulesExecuted, IEnumerable<RuleExecutionResult>? results) {
        TransactionId = transactionId;
        NumberOfRulesExecuted = numberOfRulesExecuted;
        Results = results ?? new List<RuleExecutionResult>();
    }

    /// <summary>The id of the transaction.</summary>
    public Guid TransactionId { get; }
    /// <summary>The total number of rules executed.</summary>
    public int NumberOfRulesExecuted { get; }
    /// <summary>The result of each individual rule run by the engine.</summary>
    public IEnumerable<RuleExecutionResult> Results { get; } = new List<RuleExecutionResult>();

    /// <summary>Converts a <see cref="AggregateRuleExecutionResult"/> to a <see cref="RiskResult"/></summary>
    /// <param name="riskModel"></param>
    public RiskResult ToRiskResult(RiskModel riskModel) => new() {
        TransactionId = TransactionId,
        IpAddress = riskModel.IpAddress,
        SubjectId = riskModel.SubjectId,
        Name = riskModel.Name,
        Type = riskModel.Type,
        Data = riskModel.Data,
        NumberOfRulesExecuted = NumberOfRulesExecuted,
        Results = Results
    };
}

/// <summary>Models an event of a transaction.</summary>
public class TransactionEventModel
{
    /// <summary>The unique id of the event.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the event.</summary>
    public string Name { get; set; } = null!;
    /// <summary>Timestamp regarding transaction creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Describes the result that was calculated after executing an individual rule registered in the system.</summary>
public class RuleExecutionResult
{
    /// <summary>Creates a new instance of <see cref="RuleExecutionResult"/> class.</summary>
    /// <param name="riskLevel">The risk level that came up after a rule run by the engine.</param>
    /// <param name="riskScore">The risk score that came up after a rule run by the engine.</param>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    internal RuleExecutionResult(RiskLevel riskLevel, int? riskScore, string? reason) {
        if (riskScore.HasValue) {
            var expectedRiskLevel = RiskEngineOptions.RiskLevelRangeMappingInternal.GetRiskLevel(riskScore.Value);
            if (riskLevel != expectedRiskLevel) {
                throw new InvalidOperationException($"Risk score '{riskScore}' is not in the range of risk level '{riskLevel}'.");
            }
        } else {
            riskScore = RiskEngineOptions.RiskLevelRangeMappingInternal[riskLevel].UpperLimit;
        }
        RiskLevel = riskLevel;
        RiskScore = riskScore;
        Reason = reason;
    }

    /// <summary>The risk level that came up after a rule run by the engine.</summary>
    public RiskLevel RiskLevel { get; internal set; }
    /// <summary>The risk score that came up after a rule run by the engine.</summary>
    public int? RiskScore { get; internal set; }
    /// <summary>A reason accompanying the provided risk level.</summary>
    public string? Reason { get; }
    /// <summary>The name of the rule.</summary>
    public string RuleName { get; internal set; } = string.Empty;

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Low"/> risk level.</summary>
    /// <param name="exactRiskScore">The risk score that came up after a rule run by the engine.</param>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult LowRisk(int? exactRiskScore, string? reason) => new(RiskLevel.Low, exactRiskScore, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Low"/> risk level.</summary>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult LowRisk(string? reason) => LowRisk(null, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Low"/> risk level.</summary>
    public static RuleExecutionResult LowRisk() => LowRisk(null, null);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Medium"/> risk level.</summary>
    /// <param name="exactRiskScore">The risk score that came up after a rule run by the engine.</param>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult MediumRisk(int? exactRiskScore, string? reason) => new(RiskLevel.Medium, exactRiskScore, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Medium"/> risk level.</summary>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult MediumRisk(string? reason) => MediumRisk(null, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.Medium"/> risk level.</summary>
    public static RuleExecutionResult MediumRisk() => MediumRisk(null, null);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.High"/> risk level.</summary>
    /// <param name="exactRiskScore">The risk score that came up after a rule run by the engine.</param>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult HighRisk(int? exactRiskScore, string? reason) => new(RiskLevel.High, exactRiskScore, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.High"/> risk level.</summary>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult HighRisk(string? reason) => HighRisk(null, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.High"/> risk level.</summary>
    public static RuleExecutionResult HighRisk() => HighRisk(null, null);
}

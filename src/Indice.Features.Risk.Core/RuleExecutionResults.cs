using Indice.Features.Risk.Core.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Enums;
using Indice.Features.Risk.Core.Models;
using Indice.Features.Risk.Core.Types;

namespace Indice.Features.Risk.Core;

/// <summary>Describes the result that was created after executing all the rules registered in the system.</summary>
public class AggregateRuleExecutionResult
{
    /// <summary>The unique id of the result.</summary>
    public Guid Id { get; }
    /// <summary>The total number of rules executed.</summary>
    public int NumberOfRulesExecuted { get; }
    /// <summary>The result of each individual rule run by the engine.</summary>
    public IEnumerable<RuleExecutionResult> Results { get; } = new List<RuleExecutionResult>();
    /// <summary>The aggregate risk score of all rules run by the engine.</summary>
    public int RiskScore { get; }
    /// <summary>The aggregate risk level based on risk scores of all rules run by the engine.</summary>
    public RiskLevel RiskLevel { get; }

    internal AggregateRuleExecutionResult(
        Guid id,
        int numberOfRulesExecuted,
        IEnumerable<RuleExecutionResult>? results,
        RiskEngineOptions riskEngineOptions
    ) {
        ArgumentNullException.ThrowIfNull(riskEngineOptions, nameof(riskEngineOptions));

        Id = id;
        NumberOfRulesExecuted = numberOfRulesExecuted;
        Results = results ?? new List<RuleExecutionResult>();
        RiskScore = GetAggregateRiskScore(riskEngineOptions.RiskAggregateScoreResolution);
        RiskLevel = GetAggregateRiskLevel(riskEngineOptions.RiskLevelRangeMapping);
    }

    /// <summary>Converts a <see cref="AggregateRuleExecutionResult"/> to a <see cref="DbAggregateRuleExecutionResult"/></summary>
    /// <param name="riskModel"></param>
    public DbAggregateRuleExecutionResult ToDbAggregateExecutionRiskResult(RiskModel riskModel) => new() {
        Id = Id,
        CreatedAt = DateTimeOffset.UtcNow,
        Amount = riskModel.Amount,
        IpAddress = riskModel.IpAddress,
        SubjectId = riskModel.SubjectId,
        Name = riskModel.Name,
        Type = riskModel.Type,
        Data = riskModel.Data,
        NumberOfRulesExecuted = NumberOfRulesExecuted,
        Results = Results.Select(x => new DbRuleExecutionResult {
            RiskLevel = x.RiskLevel,
            RiskScore = x.RiskScore,
            Reason = x.Reason,
            RuleName = x.RuleName
        }),
        RiskScore = RiskScore,
        RiskLevel = RiskLevel.ToString()
    };

    /// <summary>
    /// Gets aggregate Risk Score based on configured <see cref="RiskAggregateScoreResolutionType"/>
    /// </summary>
    private int GetAggregateRiskScore(RiskAggregateScoreResolutionType? aggregateScoreResolutionType) {
        return aggregateScoreResolutionType switch {
            RiskAggregateScoreResolutionType.Maximum => Results
                                .Where(x => x.RiskScore.HasValue)
                                .OrderByDescending(x => x.RiskScore)
                                .FirstOrDefault()?
                                .RiskScore ?? 0,
            _ => Results.Select(x => x.RiskScore ?? 0).Sum()
        };
    }

    /// <summary>
    /// Gets aggregate Risk Level based on configured Risk Level Range Mapping
    /// </summary>
    private RiskLevel GetAggregateRiskLevel(RiskLevelRangeDictionary riskLevelRangeMapping) {
        return riskLevelRangeMapping.GetRiskLevel(RiskScore) ?? RiskLevel.None;
    }
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

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.None"/> risk level.</summary>
    /// <param name="reason">A reason accompanying the provided risk level.</param>
    public static RuleExecutionResult NoRisk(string? reason) => new(RiskLevel.None, riskScore: 0, reason);

    /// <summary>Creates an instance of <see cref="RuleExecutionResult"/> with a <see cref="RiskLevel.None"/> risk level.</summary>
    public static RuleExecutionResult NoRisk() => NoRisk(null);

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

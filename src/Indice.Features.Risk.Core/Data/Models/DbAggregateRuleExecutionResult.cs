namespace Indice.Features.Risk.Core.Data.Models;

/// <summary>Models a risk event that was calculated by the system.</summary>
public class DbAggregateRuleExecutionResult
{
    /// <summary>The unique id of the result.</summary>
    public Guid Id { get; set; }
    /// <summary>The id of the associated transaction.</summary>
    public Guid? TransactionId { get; set; }
    /// <summary>Timestamp regarding event result calculation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>An amount relative to the event.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The user IP address related to the event occurred.</summary>
    public string? IpAddress { get; set; }
    /// <summary>The unique identifier of the subject performed the event.</summary>
    public string SubjectId { get; set; } = string.Empty;
    /// <summary>The name of the event.</summary>
    public string? Name { get; set; }
    /// <summary>The type of the event.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The data of the event.</summary>
    public dynamic? Data { get; set; }
    /// <summary>The total number of rules executed.</summary>
    public int NumberOfRulesExecuted { get; set; }
    /// <summary>The result of each individual rule run by the engine.</summary>
    public IEnumerable<DbRuleExecutionResult>? Results { get; set; }
}

/// <summary>Describes the result that was calculated after executing an individual rule registered in the system.</summary>
public class DbRuleExecutionResult
{
    /// <summary>The risk level that came up after a rule run by the engine.</summary>
    public RiskLevel RiskLevel { get; internal set; }
    /// <summary>The risk score that came up after a rule run by the engine.</summary>
    public int? RiskScore { get; internal set; }
    /// <summary>A reason accompanying the provided risk level.</summary>
    public string? Reason { get; set; }
    /// <summary>The name of the rule.</summary>
    public string RuleName { get; internal set; } = string.Empty;
}
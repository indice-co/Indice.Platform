namespace Indice.Features.Risk.Core.Data.Models;

/// <summary>Models a risk event that was calculated by the system.</summary>
public class RiskResult
{
    /// <summary>The id of the transaction.</summary>
    public Guid TransactionId { get; set; }
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
    public dynamic? Results { get; set; }
}
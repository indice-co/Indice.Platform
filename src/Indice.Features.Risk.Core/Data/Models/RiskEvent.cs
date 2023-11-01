namespace Indice.Features.Risk.Core.Data.Models;

/// <summary>Models an event that was ingested in the system.</summary>
public class RiskEvent
{
    /// <summary>The unique id of the event.</summary>
    public Guid Id { get; internal set; }
    /// <summary>An amount relative to the event.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The user IP address related to the event occurred.</summary>
    public string? IpAddress { get; set; }
    /// <summary>The unique identifier of the subject performed the event.</summary>
    public string SubjectId { get; set; } = string.Empty;
    /// <summary>Timestamp regarding event creation.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>The name of the event.</summary>
    public string? Name { get; set; }
    /// <summary>The type of the event.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>The data of the event.</summary>
    public dynamic? Data { get; set; }
}

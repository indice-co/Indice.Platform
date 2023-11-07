﻿using Indice.Features.Risk.Core.Data.Models;

namespace Indice.Features.Risk.Core.Models;

/// <summary></summary>
public class RiskModel
{
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

    /// <summary></summary>
    public RiskEvent ToRiskEvent() => new() {
        Amount = Amount,
        CreatedAt = DateTimeOffset.UtcNow,
        Id = Guid.NewGuid(),
        IpAddress = IpAddress,
        Name = Name,
        SubjectId = SubjectId,
        Type = Type,
        Data = Data
    };
}
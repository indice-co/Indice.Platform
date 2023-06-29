namespace Indice.Features.Risk.Core;

/// <summary>Describes the risk level that was calculated after a rule one or more rules were run by the engine.</summary>
public enum RiskLevel : byte
{
    /// <summary>Zero risk</summary>
    None,
    /// <summary>Low risk</summary>
    Low,
    /// <summary>Medium risk</summary>
    Medium,
    /// <summary>High risk</summary>
    High
}

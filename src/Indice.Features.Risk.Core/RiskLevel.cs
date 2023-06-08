namespace Indice.Features.Risk.Core;

/// <summary>Describes the risk level that came up after a rule run by the engine.</summary>
public enum RiskLevel : byte
{
    /// <summary>Very low</summary>
    VeryLow,
    /// <summary>Low</summary>
    Low,
    /// <summary>Medium</summary>
    Medium,
    /// <summary>High</summary>
    High,
    /// <summary>Very high</summary>
    VeryHigh
}

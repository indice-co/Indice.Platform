namespace Indice.Features.Risk.Core.Enums;

/// <summary>Describes how the aggregate risk score will be calculated after one or more rules are run by the engine.</summary>
public enum RiskAggregateScoreResolutionType
{
    /// <summary>The aggregate risk score will be calculated by summing all individual risk scores.</summary>
    Sum,

    /// <summary>The aggregate risk score will be calculated by selecting the maximum individual risk score.</summary>
    Maximum
}
using Indice.Features.Risk.Core.Types;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Options used to configure the core risk engine.</summary>
public class RiskEngineOptions
{
    /// <summary></summary>
    public IDictionary<RiskLevel, IntegerRange> RiskLevelRangeMapping { get; set; } = new Dictionary<RiskLevel, IntegerRange> {
        { RiskLevel.VeryLow, new IntegerRange(0, 200) },
        { RiskLevel.Low, new IntegerRange(201, 400) },
        { RiskLevel.Medium, new IntegerRange(401, 600) },
        { RiskLevel.High, new IntegerRange(601, 800) },
        { RiskLevel.VeryHigh, new IntegerRange(801, 1000) }
    };
}

using System.ComponentModel.DataAnnotations;
using Indice.Features.Risk.Core.Types;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Options used to configure the core risk engine.</summary>
public class RiskEngineOptions
{
    internal static RiskLevelRangeDictionary RiskLevelRangeMappingInternal = new(new Dictionary<RiskLevel, IntegerRange> {
        [RiskLevel.None] = new IntegerRange(0, 1),
        [RiskLevel.Low] = new IntegerRange(2, 333),
        [RiskLevel.Medium] = new IntegerRange(334, 666),
        [RiskLevel.High] = new IntegerRange(667, 1000)
    });

    /// <summary>Contains the mapping between the risk level and the score.</summary>
    public RiskLevelRangeDictionary RiskLevelRangeMapping {
        get => RiskLevelRangeMappingInternal;
        set => RiskLevelRangeMappingInternal = value is not null && value.Any()
            ? new RiskLevelRangeDictionary(value.OrderBy(x => (int)x.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            : throw new ArgumentNullException($"{RiskLevelRangeMapping} options must be configured.");
    }

    /// <summary>Validates the current instance of <see cref="RiskEngineOptions"/>.</summary>
    internal ValidationResult? Validate() {
        if (RiskLevelRangeMapping.ContainsOverlappingRanges(out var errorMessage)) {
            return new ValidationResult(errorMessage);
        }
        return ValidationResult.Success;
    }
}

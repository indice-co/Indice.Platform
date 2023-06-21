using Indice.Features.Risk.Core.Types;

namespace Indice.Features.Risk.Core.Configuration;

/// <summary>Options used to configure the core risk engine.</summary>
public class RiskEngineOptions
{
    internal static IDictionary<RiskLevel, IntegerRange> RiskLevelRangeMappingInternal = new Dictionary<RiskLevel, IntegerRange> {
        { RiskLevel.VeryLow, new IntegerRange(0, 200) },
        { RiskLevel.Low, new IntegerRange(201, 400) },
        { RiskLevel.Medium, new IntegerRange(401, 600) },
        { RiskLevel.High, new IntegerRange(601, 800) },
        { RiskLevel.VeryHigh, new IntegerRange(801, 1000) }
    };

    /// <summary>Contains the mapping between the risk level and the score.</summary>
    public IDictionary<RiskLevel, IntegerRange> RiskLevelRangeMapping {
        get => RiskLevelRangeMappingInternal;
        set => RiskLevelRangeMappingInternal = value?.Any() == true ? value.OrderBy(x => (int)x.Key).ToDictionary(x => x.Key, y => y.Value) : throw new ArgumentNullException($"{RiskLevelRangeMapping} options must be configured.");
    }

    /// <summary></summary>
    public RiskEngineOptionsValidationResult Validate() {
        for (var i = 0; i < RiskLevelRangeMapping.Count; i++) {
            if (i + 1 < RiskLevelRangeMapping.Count) {
                var currentRange = RiskLevelRangeMapping.ElementAt(i);
                var nextRange = RiskLevelRangeMapping.ElementAt(i + 1);
                var isOverlapped = currentRange.Value.IsOverlapped(nextRange.Value);
                if (isOverlapped) {
                    return RiskEngineOptionsValidationResult.Failed($"Risk level '{currentRange}' is overlapped with '{nextRange}'.");
                }
            }
        }
        return RiskEngineOptionsValidationResult.Success;
    }
}

/// <summary>Models the result of validating a <see cref="RiskEngineOptions"/> instance.</summary>
public class RiskEngineOptionsValidationResult
{
    /// <summary>Indicates whether the validation result was successful or not.</summary>
    public bool Succeeded { get; private set; }
    /// <summary>The error message.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Creates a successful result.</summary>
    public static RiskEngineOptionsValidationResult Success => new() {
        Succeeded = true
    };

    /// <summary>Create a failure result.</summary>
    /// <param name="errorMessage">The error message.</param>
    public static RiskEngineOptionsValidationResult Failed(string? errorMessage) => new() {
        Succeeded = false,
        ErrorMessage = errorMessage
    };
}

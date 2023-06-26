namespace Indice.Features.Risk.Core.Types;

/// <summary>A type for defining the mapping between <see cref="RiskLevel"/> and it's associated <see cref="IntegerRange"/>.</summary>
public class RiskLevelRangeDictionary : Dictionary<RiskLevel, IntegerRange>
{
    /// <summary>Creates a new instance of <see cref="RiskLevelRangeDictionary"/>.</summary>
    /// <param name="source">The source used to create the <see cref="RiskLevelRangeDictionary"/> instance.</param>
    public RiskLevelRangeDictionary(IDictionary<RiskLevel, IntegerRange> source) {
        foreach (var item in source) {
            Add(item.Key, item.Value);
        }
    }

    /// <summary>Checks if the ranges in the dictionary contain overlapping values.</summary>
    /// <returns>Returns true if overlapping ranges are contained in the dictionary, otherwise false.</returns>
    public bool ContainsOverlappingRanges(out string? errorMessage) {
        errorMessage = null;
        for (var i = 0; i < Count; i++) {
            if (i + 1 < Count) {
                var currentRange = this.ElementAt(i);
                var nextRange = this.ElementAt(i + 1);
                var isOverlapped = currentRange.Value.IsOverlapped(nextRange.Value);
                if (isOverlapped) {
                    errorMessage = $"Risk level '{currentRange}' is overlapped with '{nextRange}'.";
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Gets the risk level that corresponds to a given value.</summary>
    /// <param name="value">The value to look for.</param>
    public RiskLevel? GetRiskLevel(int? value) {
        if (!value.HasValue) {
            return default;
        }
        if (value < this[RiskLevel.VeryLow].LowerLimit) {
            return RiskLevel.VeryLow;
        }
        if (value > this[RiskLevel.VeryHigh].UpperLimit) {
            return RiskLevel.VeryHigh;
        }
        return this.Where(x => x.Value.LowerLimit <= value && value <= x.Value.UpperLimit).First().Key;
    }
}

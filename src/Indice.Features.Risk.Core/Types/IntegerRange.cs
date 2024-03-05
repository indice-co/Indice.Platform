namespace Indice.Features.Risk.Core.Types;

/// <summary>Models a range of integer values.</summary>
public class IntegerRange
{
    private int _lowerLimit = int.MinValue;
    private int _upperLimit = int.MaxValue;

    /// <summary>Creates a new instance of <see cref="IntegerRange"/> with a lower and upper limit.</summary>
    /// <param name="lowerLimit">Lower limit of range. Defaults to <see cref="int.MinValue"/>.</param>
    /// <param name="upperLimit">Upper limit of range. Defaults to <see cref="int.MaxValue"/></param>
    public IntegerRange(int lowerLimit = int.MinValue, int upperLimit = int.MaxValue) {
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
    }

    /// <summary>Lower limit of range.</summary>
    public int LowerLimit {
        get => _lowerLimit;
        set => _lowerLimit = value;
    }

    /// <summary>Upper limit of range.</summary>
    public int UpperLimit {
        get => _upperLimit;
        set => _upperLimit = value >= _lowerLimit ? value : throw new InvalidOperationException($"Upper limit ({value}) of range cannot be lower than the lower limit ({_lowerLimit}).");
    }

    /// <summary>Check if the current range is overlapped by another given range.</summary>
    /// <param name="other">The range to compare to.</param>
    public bool IsOverlapped(IntegerRange other) =>
        LowerLimit.CompareTo(other.UpperLimit) < 0 && other.LowerLimit.CompareTo(UpperLimit) < 0;

    /// <inheritdoc />
    public override string ToString() => $"[{LowerLimit} - {UpperLimit}]";
}

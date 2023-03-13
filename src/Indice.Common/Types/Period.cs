namespace Indice.Types;

/// <summary>Represents a period in time, with a starting and ending <see cref="DateTimeOffset"/>.</summary>
public class Period
{
    /// <summary>Period from.</summary>
    public DateTimeOffset? From { get; set; }
    /// <summary>Period to.</summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>String representation of a <see cref="Period"/>.</summary>
    public override string ToString() => $"{From:d} - {To:d}";
}

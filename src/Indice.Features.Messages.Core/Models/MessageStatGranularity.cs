namespace Indice.Features.Messages.Core.Models;

/// <summary>The length of the period that a send statistics row covers.</summary>
public enum MessageStatGranularity : byte
{
    /// <summary>The row covers one UTC day.</summary>
    Day = 2,
    /// <summary>The row covers one UTC calendar month.</summary>
    Month = 3
}

namespace Indice.Features.Cases.Core.Models;

/// <summary>Filters the entries returned by the customer timeline.</summary>
public enum TimelineFilter
{
    /// <summary>Include comments and checkpoints.</summary>
    All,
    /// <summary>Include only comments.</summary>
    Comments,
    /// <summary>Include only checkpoints.</summary>
    Checkpoints
}

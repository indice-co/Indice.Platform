namespace Indice.Types;

/// <summary>Specifies a time frame for aggregating or filtering time-series data.</summary>
public enum SeriesTimeFrame
{
    /// <summary>filter last 24h.</summary>
    Last24Hours = 1,
    /// <summary>filter last week.</summary>
    Last7Days = 2,
    /// <summary>filter last month.</summary>
    Last30Days = 3,
    /// <summary>filter last Q.</summary>
    Last90Days = 4,
    /// <summary>filter last year.</summary>
    Last12Months = 5,
}

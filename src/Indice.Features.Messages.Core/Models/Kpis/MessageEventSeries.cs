using System.Diagnostics;
using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>A daily aggregate of message events.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class MessageEventSeries {
    /// <summary>The year of the aggregate.</summary>
    public int? Year { get; set; }
    /// <summary>The month of the aggregate.</summary>
    public int? Month { get; set; }
    /// <summary>The day of the aggregate.</summary>
    public int? Day { get; set; }
    /// <summary>The total number of events.</summary>
    public int Events { get; set; }

    /// <summary>Indicates whether the aggregate represents a total count without specific date breakdown.</summary>
    internal bool IsTotal => !Day.HasValue || !Month.HasValue || !Year.HasValue;
    /// <summary>Indicates whether the aggregate represents a total per month.</summary>
    internal bool IsMonthTotal => !Day.HasValue && Month.HasValue && Year.HasValue;
    /// <summary>Indicates whether the aggregate represents a total per month.</summary>
    internal bool IsYearTotal => !Day.HasValue && !Month.HasValue && Year.HasValue;
    /// <summary>
    /// Gets a value indicating whether this instance represents a grand total, with no specific day, month, or year
    /// assigned.
    /// </summary>
    /// <remarks>Use this property to determine if the object aggregates data across all dates, rather than
    /// for a specific time period.</remarks>
    internal bool IsGrandTotal => !Day.HasValue && !Month.HasValue && !Year.HasValue;

    /// <summary>Gets a label representing the date of the aggregate in "YYYY-MM-DD" format.</summary>
    public string Label => IsGrandTotal ? "Total" : 
                            IsYearTotal ? $"{Year:D4}" :
                            IsMonthTotal ? $"{Year:D4}-{Month:D2}" :
                            $"{Year:D4}-{Month:D2}-{Day:D2}";

    /// <inheritdoc/>
    public override string ToString() => $"{Label} ({Events})";
}

/// <summary>
/// A summary of message event series data, including the total number of events. 
/// </summary>
public class MessageEventSeriesSummary
{
    /// <summary>The total number of events.</summary>
    public int Total { get; set; }
}

/// <summary>A result set containing message event series data along with a summary.</summary>
public class MessageSeriesResultSet : ResultSet<MessageEventSeries, MessageEventSeriesSummary>
{
    /// <inheritdoc/>
    public MessageSeriesResultSet() : base() {
            
    }
    
    /// <inheritdoc/>
    public MessageSeriesResultSet(IEnumerable<MessageEventSeries> collection, int totalCount, MessageEventSeriesSummary summary) 
        : base(collection, totalCount, summary) {

    }
}

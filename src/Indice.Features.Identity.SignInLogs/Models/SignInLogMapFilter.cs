using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>
/// Map filter options
/// </summary>
public class SignInLogMapFilter
{
    /// <summary>
    /// Defines the time frame for which to retrieve sign-in log map data. This property allows you to specify a predefined period (e.g., last 24 hours, last 7 days, etc.) for which the sign-in log map should be generated. The selected time frame will determine the range of sign-in events included in the map, enabling you to visualize sign-in activity over that specific period. 
    /// </summary>
    public SeriesTimeFrame? TimeFrame { get; set; }
}

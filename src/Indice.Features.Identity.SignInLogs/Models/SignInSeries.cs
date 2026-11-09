using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Filter options for the daily sign in series.</summary>
public class SignInSeriesFilter
{
    /// <summary>The period to cover, ending today. Defaults to <see cref="SeriesTimeFrame.Last30Days"/>.</summary>
    public SeriesTimeFrame? TimeFrame { get; set; }
}

/// <summary>Sign in attempts of a single day (UTC).</summary>
/// <param name="Date">The day.</param>
/// <param name="Succeeded">Number of successful sign in events.</param>
/// <param name="Failed">Number of failed sign in events.</param>
/// <param name="UniqueUsers">Number of distinct users with at least one sign in event.</param>
public record SignInDailyCount(DateTime Date, int Succeeded, int Failed, int UniqueUsers);

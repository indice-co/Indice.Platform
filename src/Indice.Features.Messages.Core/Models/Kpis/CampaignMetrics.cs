using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>
/// Represents aggregate metrics for a set of campaigns, including total and active campaign counts.
/// </summary>
/// <remarks>Use this class to obtain summary statistics about campaigns, such as the total number created and the
/// number currently active. A campaign is considered active if it is published and has not expired; specifically, its
/// end date is either in the future or not set.</remarks>
public class CampaignMetrics
{
    /// <summary>Total campaigns created.</summary>
    [Description("Total campaigns created.")]
    public int Total { get; set; }
    /// <summary>Total campaigns that are active. This means campaigns that are published and not expired. (End date is in the future or not set)</summary>
    [Description("Total active campaigns.")]
    public int Active { get; set; }
    /// <summary>Gets or sets the total number of items processed today.</summary>
    [Description("Total campaigns created today.")]
    public int TotalToday { get; set; }
    /// <summary>Gets or sets the total value recorded for the previous day.</summary>
    [Description("Total campaigns created yesterday.")]
    public int TotalYesterday { get; set; }
    /// <summary>Trend of campaigns created today vs yesterday</summary>
    [Description("Trend of campaigns created today vs yesterday.")]
    public double Trend => TotalYesterday == 0 ? 0 : ((double)(TotalToday - TotalYesterday) / TotalYesterday);
}

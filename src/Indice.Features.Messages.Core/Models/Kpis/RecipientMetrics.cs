using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>Statistics about message recipients.</summary>
[Description("Statistics about message dalivery.")]
public class RecipientMetrics
{
    /// <summary>Total number of campaigns that have used this sender.</summary>
    [Description("Total number of recipients targeted by the message.")]
    public int TotalCampaigns { get; set; }
    /// <summary>Total number of messages sent in all channels combigned.</summary>
    [Description("Total number of messages targeted by the message.")]
    public int TotalMessages { get; set; }
    /// <summary>Total number of recipients targeted by the message.</summary>
    [Description("Total number of recipients targeted by the message.")]
    public int Total { get; set; }
    /// <summary>Number of recipients that have received the message.</summary>
    [Description("Number of recipients that have received the message.")]
    public int Reached { get; set; }
    /// <summary>Number of recipients that have opened the message at least once.</summary>
    [Description("Number of recipients that have opened the message at least once.")]
    public int Engaged { get; set; }
    /// <summary>Number of recipients that have opened the message at least once.</summary>
    [Description("Number of recipients that clicked the call to action link at least once.")]
    public int Acted { get; set; }
    /// <summary>
    /// Reachability Coverage Percentage: The percentage of recipients that have been successfully reached out of the total targeted recipients.
    /// </summary>
    [Description("Reachability Coverage Percentage: The percentage of recipients that have been successfully reached out of the total targeted recipients.")]
    public double Coverage => Total == 0 ? 0 : (double)Reached / Total;

    /// <summary>Total number of recipients targeted by the message.</summary>
    [Description("Average Total number of recipients targeted by the message.")]
    public int AvgTotal => TotalCampaigns > 0 ? (int)Math.Round(Total / (double)TotalCampaigns) : 0;
    /// <summary>Total number of recipients targeted by the message.</summary>
    [Description("Average Total number of recipients targeted by the message.")]
    public int AvgReached => TotalCampaigns > 0 ? (int)Math.Round(Reached / (double)TotalCampaigns) : 0;
    /// <summary>Total number of recipients targeted by the message.</summary>
    [Description("Average recipients that have opened the message at least once.")]
    public int AvgEngaged => TotalCampaigns > 0 ? (int)Math.Round(Engaged / (double)TotalCampaigns) : 0;
}

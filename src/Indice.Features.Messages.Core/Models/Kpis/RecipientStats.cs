using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>Statistics about message recipients.</summary>
public class RecipientStats
{
    /// <summary>Total number of recipients targeted by the message.</summary>
    [Description("Total number of recipients targeted by the message.")]
    public int Count { get; set; }
    /// <summary>Number of recipients that have received the message.</summary>
    [Description("Number of recipients that have received the message.")]
    public int Reached { get; set; }
    /// <summary>Number of recipients that have opened the message at least once.</summary>
    [Description("Number of recipients that have opened the message at least once.")]
    public int Engaged { get; set; }
    /// <summary>
    /// Reachability Coverage Percentage: The percentage of recipients that have been successfully reached out of the total targeted recipients.
    /// </summary>
    [Description("Reachability Coverage Percentage: The percentage of recipients that have been successfully reached out of the total targeted recipients.")]
    public double Coverage => Count == 0 ? 0 : (double)Reached / Count;
}

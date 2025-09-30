using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>Statistics about contacts in the system.</summary>
[Description("Statistics about contacts in the system.")]
public class ContactMetrics
{

    /// <summary>Indicates the number of contacts in the system.</summary>
    [Description("Indicates the number of contacts in the system.")]
    public int Total { get; set; }
    /// <summary>Indicates the number of known contacts.</summary>
    /// <remarks>Contacts that have an actual recipientId that has been successfully resolved from an external source (ie Identity System).</remarks>
    [Description("Contacts that have an actual recipientId that has been successfully resolved from an external source (ie Identity System).</")]
    public int Known { get; set; }
}

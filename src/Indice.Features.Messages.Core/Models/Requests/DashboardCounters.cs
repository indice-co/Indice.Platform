
namespace Indice.Features.Messages.Core.Models.Requests;
/// <summary>
/// Represents various counters for the dashboard, including campaign and message statistics.
/// </summary>
public class DashboardCounters
{
    /// <summary>Indicates the number of campaigns created.</summary>
    public int CampaignsCount { get; set; }
    /// <summary>Indicates the number of published campaigns.</summary>
    public int CampaignsPublishedCount { get; set; }
    /// <summary>Indicates the number messages send per message type.</summary>
    public Dictionary<string, int> CampaignsByType { get; set; } = new Dictionary<string, int>();
    /// <summary>Indicates the number of contacts in the system.</summary>
    public int ContactsTotal { get; set; }
    /// <summary>Indicates the number of known contacts (with email or phone).</summary>
    public int ContactsKnownTotal { get; set; }
}

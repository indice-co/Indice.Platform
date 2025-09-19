namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary></summary>
public class CampaignStatistics
{
    /// <summary></summary>
    public string? Title { get; set; }
    /// <summary></summary>
    public int ReadCount { get; set; }
    /// <summary></summary>
    public int? NotReadCount { get; set; }
    /// <summary></summary>
    public int DeletedCount { get; set; }
    /// <summary></summary>
    public int CallToActionCount { get; set; }
    /// <summary></summary>
    public DateTime LastUpdated { get; set; }


    /// <summary>Indicates the number of created messages per channel kind.</summary>
    public Dictionary<string, int> MessagesperChannel{ get; set; } = new Dictionary<string, int>();
    /// <summary></summary>
    public int RecipientsCount { get; set; }

}

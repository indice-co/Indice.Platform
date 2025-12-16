using Indice.Features.Messages.Core.Data.Models;

namespace Indice.Features.Messages.Core.Models;

/// <summary>Data transfer object representing campaign messages.</summary>
public class CampaignMessagesDto
{
    /// <summary>Represents the campaign associated with the messages.</summary>
    public DbCampaign? Campaign { get; set; }
    /// <summary>Represents the message associated with the campaign.</summary>
    public DbMessage? Message { get; set; }
}

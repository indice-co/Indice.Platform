using System.Text.Json.Serialization;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to create a new campaign.</summary>
public class SendRequest : CreateCampaignRequest
{
    /// <summary>Determines if a campaign is published.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public override bool Published => true;
}
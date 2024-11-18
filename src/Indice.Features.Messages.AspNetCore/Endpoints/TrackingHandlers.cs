#if NET7_0_OR_GREATER
#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Indice.Features.Messages.Core;
using Indice.Types;
using Microsoft.AspNetCore.Http.HttpResults;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.AspNetCore.Endpoints;
internal static class TrackingHandlers
{

    public static async Task<Results<RedirectHttpResult, NotFound>> Track(
        ICampaignService campaignService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Base64Id trackingCode) {
        var campaignId = trackingCode.Id;
        var campaign = await campaignService.GetById(campaignId);
        if (campaign is null) {
            return TypedResults.NotFound();
        }
        await campaignService.UpdateHit(campaignId);
        return TypedResults.Redirect(campaign.ActionLink.Href);
    }

    #region Descriptions
    public const string TRACK_DESCRIPTION = @"
    Tracks a campaign message click using a tracking code and redirects to the campaign's action link.

    Parameters:
    - trackingCode: The Base64-encoded tracking code for the campaign.
    ";
    #endregion
}

#nullable disable
#endif
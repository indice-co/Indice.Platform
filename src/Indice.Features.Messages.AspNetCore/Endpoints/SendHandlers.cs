using Indice.Features.Media.AspNetCore;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class SendHandlers
{
    public static async Task<Results<Ok<CreateCampaignResult>, ValidationProblem>> SendCampaign(
        NotificationsManager notificationsManager,
        MediaBaseHrefResolver baseHrefResolver,
        SendRequest request) {
        if (string.IsNullOrWhiteSpace(request.MediaBaseHref) ||
            Uri.TryCreate(request!.MediaBaseHref, UriKind.RelativeOrAbsolute, out var mediaBasePath) && !mediaBasePath.IsAbsoluteUri) {
            request.MediaBaseHref = (await baseHrefResolver.ResolveBaseHrefAsync()).ToString();
        }
        var result = await notificationsManager.CreateCampaignInternal(request.ToCreateCampaignRequest(), validateRules: false);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(ValidationErrors.AddErrors("Campaign Validation", result.Errors));
        }
        return TypedResults.Ok(result);
    }

    #region Descriptions

    public static readonly string CREATE_CAMPAIGN_DESCRIPTION = @"
Creates a new campaign.

Parameters:
- request: Contains information about the campaign to be created.
";
    #endregion
}
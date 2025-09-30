using System.Net.Mime;
using System.Threading.Channels;
using Indice.Extensions;
using Indice.Features.Media.AspNetCore;
using Indice.Features.Messages.AspNetCore.Extensions;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class SendHandlers
{
    public static async Task<Results<Ok<CreateCampaignResult>, ValidationProblem>> SendCampaign(
        NotificationsManager notificationsManager,
        IConfiguration configuration,
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
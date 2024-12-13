using System.Security.Claims;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Controllers;

/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.MessageInboxEndpoints, IgnoreApi = true)]
[Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme)]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[Route("_tracking")]
internal class TrackingController(
    ICampaignService campaignService,
    IOptions<MessageInboxOptions> campaignEndpointOptions
    ) : ControllerBase
{
    public ICampaignService CampaignService { get; } = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
    public MessageInboxOptions CampaignInboxOptions { get; } = campaignEndpointOptions?.Value ?? throw new ArgumentNullException(nameof(campaignEndpointOptions));
    public string? UserCode => User.FindFirstValue(CampaignInboxOptions.UserClaimType);

    [AllowAnonymous]
    [HttpGet("messages/cta/{trackingCode}")]
    public async Task<IActionResult> Track([FromRoute] Base64Id trackingCode) {
        var campaignId = trackingCode.Id;
        var campaign = await CampaignService.GetById(campaignId);
        if (campaign is null) {
            return NotFound();
        }
        await CampaignService.UpdateHit(trackingCode.Id);
        return Redirect(campaign.ActionLink!.Href!);
    }
}

using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "campaigns")]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/campaigns")]
    internal class CampaignsController : CampaignsControllerBase
    {
        public const string Name = "Campaigns";

        public CampaignsController(
            ICampaignService campaignService,
            Func<string, IFileService> getFileService,
            Func<string, IEventDispatcher> getEventDispatcher,
            IOptions<GeneralSettings> generalSettings,
            IPlatformEventService eventService
        ) : base(getFileService) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            EventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            EventDispatcher = getEventDispatcher(KeyedServiceNames.EventDispatcherAzureServiceKey) ?? throw new ArgumentNullException(nameof(getEventDispatcher));
        }

        public ICampaignService CampaignService { get; }
        public IPlatformEventService EventService { get; }
        public GeneralSettings GeneralSettings { get; }
        public IEventDispatcher EventDispatcher { get; }

        /// <summary>
        /// Gets the list of all campaigns using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<Campaign>))]
        public async Task<IActionResult> GetCampaigns([FromQuery] ListOptions<CampaignsFilter> options) {
            var campaigns = await CampaignService.GetList(options);
            return Ok(campaigns);
        }

        /// <summary>
        /// Gets a campaign with the specified id.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{campaignId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCampaignById([FromRoute] Guid campaignId) {
            var campaign = await CampaignService.GetById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            return Ok(campaign);
        }

        /// <summary>
        /// Publishes a campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpPost("{campaignId:guid}/publish")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PublishCampaign([FromRoute] Guid campaignId) {
            var updated = await CampaignService.Publish(campaignId);
            if (!updated) {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Gets the status of a campaign. 
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{campaignId:guid}/status")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCampaignStatus([FromRoute] Guid campaignId) {
            await Task.CompletedTask;
            return Ok();
        }

        /// <summary>
        /// Gets the statistics for a specified campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [CacheResourceFilter(Expiration = 5)]
        [HttpGet("{campaignId:guid}/statistics")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignStatistics))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCampaignStatistics([FromRoute] Guid campaignId) {
            var statistics = await CampaignService.GetStatistics(campaignId);
            if (statistics is null) {
                return NotFound();
            }
            return Ok(statistics);
        }

        /// <summary>
        /// Gets the statistics for a specified campaign in the form of an Excel file.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{campaignId:guid}/statistics/export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<CampaignStatistics>> ExportCampaignStatistics([FromRoute] Guid campaignId) {
            var statistics = await CampaignService.GetStatistics(campaignId);
            if (statistics == null) {
                return NotFound();
            }
            return statistics;
        }

        /// <summary>
        /// Creates a new campaign.
        /// </summary>
        /// <param name="request">Contains info about the campaign to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Campaign))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request) {
            var campaign = await CampaignService.Create(request);
            await EventDispatcher.RaiseEventAsync(
                payload: Mapper.ToCampaignCreatedEvent(campaign, request.SelectedUserCodes),
                configure: options => options.WrapInEnvelope(false).WithQueueName(QueueNames.CampaignCreated)
            );
            return CreatedAtAction(nameof(GetCampaignById), new { campaignId = campaign.Id }, campaign);
        }

        /// <summary>
        /// Updates an existing campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign to update.</param>
        /// <param name="request">Contains info about the campaign to update.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{campaignId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCampaign([FromRoute] Guid campaignId, [FromBody] UpdateCampaignRequest request) {
            var updated = await CampaignService.Update(campaignId, request);
            if (!updated) {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{campaignId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCampaign([FromRoute] Guid campaignId) {
            var deleted = await CampaignService.Delete(campaignId);
            if (!deleted) {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Uploads an attachment for the specified campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <param name="file">Contains the stream of the attachment to be uploaded.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [AllowedFileSize(6291456)] // 6 MegaBytes
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [HttpPost("{campaignId}/attachment")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentLink))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
        public async Task<IActionResult> UploadCampaignAttachment([FromRoute] Guid campaignId, [FromForm] IFormFile file) {
            if (file == null) {
                ModelState.AddModelError(nameof(file), "File is empty.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var campaign = await CampaignService.GetById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            var attachment = await CampaignService.CreateAttachment(file);
            await CampaignService.AssociateAttachment(campaignId, attachment.Id);
            return Ok(attachment);
        }

        /// <summary>
        /// Gets the attachment associated with a campaign.
        /// </summary>
        /// <param name="fileGuid">Contains the photo's Id.</param>
        /// <param name="format">Contains the format of the uploaded attachment extension.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("attachments/{fileGuid}.{format}")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        public async Task<IActionResult> GetCampaignAttachment([FromRoute] Base64Id fileGuid, [FromRoute] string format) => await GetFile("campaigns", fileGuid, format);
    }
}

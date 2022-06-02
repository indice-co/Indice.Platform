using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using HandlebarsDotNet;
using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Features.Messages.AspNetCore.Extensions;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Manager;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = ApiGroups.CampaignManagementEndpoints)]
    [Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme, Policy = MessagesApi.Policies.BeCampaignManager)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/campaigns")]
    internal class CampaignsController : CampaignsControllerBase
    {
        public const string Name = "Campaigns";

        public CampaignsController(
            ICampaignService campaignService,
            IDistributionListService distributionListService,
            IContactService contactService,
            Func<string, IFileService> getFileService,
            Func<string, IEventDispatcher> getEventDispatcher,
            IOptions<GeneralSettings> generalSettings,
            IPlatformEventService eventService,
            ICampaignAttachmentService campaignAttachmentService,
            NotificationsManager notificationsManager
        ) : base(getFileService) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            EventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            CampaignAttachmentService = campaignAttachmentService ?? throw new ArgumentNullException(nameof(campaignAttachmentService));
            NotificationsManager = notificationsManager ?? throw new ArgumentNullException(nameof(notificationsManager));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            EventDispatcher = getEventDispatcher(KeyedServiceNames.EventDispatcherServiceKey) ?? throw new ArgumentNullException(nameof(getEventDispatcher));
        }

        public ICampaignService CampaignService { get; }
        public IDistributionListService DistributionListService { get; }
        public IContactService ContactService { get; }
        public IPlatformEventService EventService { get; }
        public ICampaignAttachmentService CampaignAttachmentService { get; }
        public NotificationsManager NotificationsManager { get; }
        public GeneralSettings GeneralSettings { get; }
        public IEventDispatcher EventDispatcher { get; }

        /// <summary>Gets the list of all campaigns using the provided <see cref="ListOptions"/>.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ResultSet<Campaign>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCampaigns([FromQuery] ListOptions<CampaignListFilter> options) {
            var campaigns = await CampaignService.GetList(options);
            return Ok(campaigns);
        }

        /// <summary>Receives a list of </summary>
        /// <param name="items"></param>
        [ApiExplorerSettings(IgnoreApi = false)]
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPost("preview")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(IEnumerable<PreviewItemResult>), StatusCodes.Status200OK)]
        public IActionResult PreviewCampaign([FromBody] IEnumerable<PreviewItem> items) {
            var itemsResult = new List<PreviewItemResult>();
            var handlebars = Handlebars.Create();
            handlebars.Configuration.TextEncoder = new HtmlEncoder();
            foreach (var item in items) {
                var resultText = handlebars.Compile(item.Text)(item.Data);
                itemsResult.Add(new PreviewItemResult { 
                    Code = item.Code,
                    Text = resultText
                });
            }
            return Ok(itemsResult);
        }

        /// <summary>Gets a campaign with the specified id.</summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{campaignId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CampaignDetails), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCampaignById([FromRoute] Guid campaignId) {
            var campaign = await CampaignService.GetById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            return Ok(campaign);
        }

        /// <summary>Publishes a campaign.</summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPut("{campaignId:guid}/publish")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> PublishCampaign([FromRoute] Guid campaignId) {
            var publishedCampaign = await CampaignService.Publish(campaignId);
            // Dispatch event that the campaign was created.
            await EventDispatcher.RaiseEventAsync(
                payload: CampaignCreatedEvent.FromCampaign(publishedCampaign),
                configure: options => options.WrapInEnvelope(false).WithQueueName(EventNames.CampaignCreated)
            );
            return NoContent();
        }

        /// <summary>Gets the statistics for a specified campaign.</summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [CacheResourceFilter(Expiration = 5)]
        [HttpGet("{campaignId:guid}/statistics")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(CampaignStatistics), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCampaignStatistics([FromRoute] Guid campaignId) {
            var statistics = await CampaignService.GetStatistics(campaignId);
            if (statistics is null) {
                return NotFound();
            }
            return Ok(statistics);
        }

        /// <summary>Gets the statistics for a specified campaign in the form of an Excel file.</summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{campaignId:guid}/statistics/export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(IFormFile), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CampaignStatistics>> ExportCampaignStatistics([FromRoute] Guid campaignId) {
            var statistics = await CampaignService.GetStatistics(campaignId);
            if (statistics == null) {
                return NotFound();
            }
            return statistics;
        }

        /// <summary>Creates a new campaign.</summary>
        /// <param name="request">Contains info about the campaign to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Campaign), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request) {
            var result = await NotificationsManager.CreateCampaignInternal(request, validateRules: false);
            return CreatedAtAction(nameof(GetCampaignById), new { campaignId = result.CampaignId }, result.Campaign);
        }

        /// <summary>Updates an existing unpublished campaign.</summary>
        /// <param name="campaignId">The id of the campaign to update.</param>
        /// <param name="request">Contains info about the campaign to update.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [Consumes(MediaTypeNames.Application.Json)]
        [HttpPut("{campaignId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCampaign([FromRoute] Guid campaignId, [FromBody] UpdateCampaignRequest request) {
            await CampaignService.Update(campaignId, request);
            return NoContent();
        }

        /// <summary>Permanently deletes a campaign.</summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpDelete("{campaignId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteCampaign([FromRoute] Guid campaignId) {
            await CampaignService.Delete(campaignId);
            return NoContent();
        }

        /// <summary>Uploads an attachment for the specified campaign.</summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <param name="file">Contains the stream of the attachment to be uploaded.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [AllowedFileSize(6291456)] // 6 MegaBytes
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [HttpPost("{campaignId}/attachment")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(AttachmentLink), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCampaignAttachment([FromRoute] Guid campaignId, [FromForm] IFormFile file) {
            if (file == null) {
                ModelState.AddModelError(nameof(file), "File is empty.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var attachment = new FileAttachment(file.OpenReadStream).PopulateFrom(file);
            var attachmentLink = await CampaignAttachmentService.Create(attachment);
            await CampaignAttachmentService.Associate(campaignId, attachmentLink.Id);
            return Ok(attachmentLink);
        }

        /// <summary>Gets the attachment associated with a campaign.</summary>
        /// <param name="fileGuid">Contains the photo's Id.</param>
        /// <param name="format">Contains the format of the uploaded attachment extension.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("attachments/{fileGuid}.{format}")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(typeof(IFormFile), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        public async Task<IActionResult> GetCampaignAttachment([FromRoute] Base64Id fileGuid, [FromRoute] string format) => await GetFile("campaigns", fileGuid, format);
    }
}

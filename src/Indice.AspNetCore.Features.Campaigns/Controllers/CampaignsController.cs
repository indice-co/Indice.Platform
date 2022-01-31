using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Extensions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Features.Campaigns.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "campaigns")]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route("[campaignsApiPrefix]")]
    internal class CampaignsController : ControllerBase
    {
        public const string Name = "Campaigns";

        public CampaignsController(ICampaignService campaignService, IFileService fileService, IOptions<GeneralSettings> generalSettings) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            FileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            GeneralSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        public ICampaignService CampaignService { get; }
        public IFileService FileService { get; }
        public GeneralSettings GeneralSettings { get; }

        /// <summary>
        /// Gets the list of all campaigns using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("campaigns")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<Campaign>))]
        public async Task<IActionResult> GetCampaigns([FromQuery] ListOptions<GetCampaignsListFilter> options) {
            var campaigns = await CampaignService.GetCampaigns(options);
            return Ok(campaigns);
        }

        /// <summary>
        /// Gets a campaign with the specified id.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("campaigns/{campaignId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCampaignById([FromRoute] Guid campaignId) {
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            return Ok(campaign);
        }

        /// <summary>
        /// Gets the attachment associated with a campaign.
        /// </summary>
        /// <param name="fileGuid">Contains the photo's Id.</param>
        /// <param name="format">Contains the format of the uploaded attachment extension.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [HttpGet("campaigns/attachments/{fileGuid}.{format}")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        public async Task<IActionResult> GetCampaignAttachment([FromRoute] Base64Id fileGuid, [FromRoute] string format) => await GetFile("campaigns", fileGuid, format);

        /// <summary>
        /// Gets the statistics for a specified campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [CacheResourceFilter(Expiration = 5)]
        [HttpGet("campaigns/{campaignId:guid}/statistics")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignStatistics))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCampaignStatistics([FromRoute] Guid campaignId) {
            var statistics = await CampaignService.GetCampaignStatistics(campaignId);
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
        [HttpGet("campaigns/{campaignId:guid}/statistics/export")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<CampaignStatistics>> ExportCampaignStatistics([FromRoute] Guid campaignId) {
            var statistics = await CampaignService.GetCampaignStatistics(campaignId);
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
        [HttpPost("campaigns")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Campaign))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request) {
            var campaign = await CampaignService.CreateCampaign(request);

            return CreatedAtAction(nameof(GetCampaignById), new { campaignId = campaign.Id }, campaign);
        }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpGet("campaign-types")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CampaignType>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> GetCampaignTypes([FromQuery] ListOptions options) {
            var campaignTypes = await CampaignService.GetCampaignTypes(options);
            return Ok(campaignTypes);
        }

        /// <summary>
        /// Creates a new campaign type.
        /// </summary>
        /// <param name="request">Contains info about the campaign type to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpPost("campaign-types")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateCampaignType([FromBody] UpsertCampaignTypeRequest request) {
            var campaign = await CampaignService.CreateCampaignType(request);
            return Ok(campaign);
        }

        /// <summary>
        /// Updates an existing campaign type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the campaign type.</param>
        /// <param name="request">Contains info about the campaign type to update.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPut("campaign-types/{campaignTypeId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(CampaignType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCampaignType([FromRoute] Guid campaignTypeId, [FromBody] UpsertCampaignTypeRequest request) {
            var campaignType = await CampaignService.GetCampaignTypeById(campaignTypeId);
            if (campaignType is null) {
                return NotFound();
            }
            await CampaignService.UpdateCampaignType(campaignTypeId, request);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a campaign type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the campaign type.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("campaign-types/{campaignTypeId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCampaignType([FromRoute] Guid campaignTypeId) {
            var campaignType = await CampaignService.GetCampaignTypeById(campaignTypeId);
            if (campaignType == null) {
                return NotFound();
            }
            await CampaignService.DeleteCampaignType(campaignTypeId);
            return NoContent();
        }

        /// <summary>
        /// Updates an existing campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign to update.</param>
        /// <param name="request">Contains info about the campaign to update.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("campaigns/{campaignId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCampaign([FromRoute] Guid campaignId, [FromBody] UpdateCampaignRequest request) {
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            await CampaignService.UpdateCampaign(campaignId, request);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a campaign.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("campaigns/{campaignId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCampaign([FromRoute] Guid campaignId) {
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign == null) {
                return NotFound();
            }
            await CampaignService.DeleteCampaign(campaignId);
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
        [HttpPost("campaigns/{campaignId}/attachment")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AttachmentLink))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
        public async Task<IActionResult> UploadCampaignAttachment([FromRoute] Guid campaignId, [FromForm] IFormFile file) {
            if (file == null) {
                ModelState.AddModelError(nameof(file), "File is empty.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            var attachment = await CampaignService.CreateAttachment(file);
            await CampaignService.AssociateCampaignAttachment(campaignId, attachment.Id);
            return Ok(attachment);
        }

        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("campaigns/track/{trackingCode}")]
        public async Task<IActionResult> Track([FromRoute] Base64Id trackingCode) {
            var campaignId = trackingCode.Id;
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            await CampaignService.UpdateCampaignVisit(campaignId);
            return Redirect(campaign.ActionUrl);
        }

        private async Task<IActionResult> GetFile(string rootFolder, Guid fileGuid, string format) {
            if (format.StartsWith('.')) {
                format = format.TrimStart('.');
            }
            var path = $"{rootFolder}/{fileGuid.ToString("N").Substring(0, 2)}/{fileGuid:N}.{format}";
            var properties = await FileService.GetPropertiesAsync(path);
            if (properties is null) {
                return NotFound();
            }
            var data = await FileService.GetAsync(path);
            var contentType = properties.ContentType;
            if (contentType == MediaTypeNames.Application.Octet && !string.IsNullOrEmpty(format)) {
                contentType = FileExtensions.GetMimeType($".{format}");
            }
            return File(data, contentType, properties.LastModified, new EntityTagHeaderValue(properties.ETag, true));
        }
    }
}

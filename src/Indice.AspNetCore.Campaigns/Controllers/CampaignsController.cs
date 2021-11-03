using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Extensions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Features.Campaigns.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "campaigns")]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
    [Route("api/campaigns")]
    internal class CampaignsController : ControllerBase
    {
        public const string Name = "Campaigns";

        public CampaignsController(
            ICampaignService campaignService,
            IFileService fileService
        ) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            FileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        public ICampaignService CampaignService { get; }
        public IFileService FileService { get; }

        /// <summary>
        /// Gets the list of all campaigns using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<Campaign>))]
        public async Task<IActionResult> GetCampaigns([FromQuery] ListOptions options) {
            var campaigns = await CampaignService.GetCampaigns(options);
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
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            return Ok(campaign);
        }

        /// <summary>
        /// Gets the image associated with a campaign.
        /// </summary>
        /// <param name="fileGuid">Contains the photo's Id.</param>
        /// <param name="format">Contains the format of the uploaded image extension.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [AllowAnonymous]
        [HttpGet("images/{fileGuid}.{format}")]
        [Produces(MediaTypeNames.Application.Octet)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        public async Task<IActionResult> GetCampaignImage([FromRoute] Base64Id fileGuid, [FromRoute] string format) => await GetFile("campaigns", fileGuid, format);

        private async Task<IActionResult> GetFile(string rootFolder, Guid fileGuid, string format) {
            if (format.StartsWith(".")) {
                format = format.TrimStart('.');
            }
            var path = $"{rootFolder}/{fileGuid.ToString("N").Substring(0, 2)}/{fileGuid:N}.{format}";
            var info = await FileService.GetPropertiesAsync(path);
            if (info == null) {
                return NotFound();
            }
            var data = await FileService.GetAsync(path);
            var contentType = info.ContentType;
            if (contentType == MediaTypeNames.Application.Octet && !string.IsNullOrEmpty(format)) {
                contentType = FileExtensions.GetMimeType($".{format}");
            }
            return File(data, contentType, info.LastModified, new EntityTagHeaderValue(info.ETag, true));
        }
    }
}

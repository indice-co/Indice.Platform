using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Messages.AspNetCore.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "messages")]
    [Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme, Policy = MessagesApi.Policies.BeCampaignManager)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/templates")]
    internal class TemplatesController : ControllerBase
    {
        public TemplatesController(ITemplateService templateService) {
            TemplateService = templateService;
        }

        public ITemplateService TemplateService { get; }

        /// <summary>
        /// Creates a new template in the store.
        /// </summary>
        /// <param name="request">The request model used to create a new template.</param>
        /// <response code="200">OK</response>
        [HttpPost]
        [ProducesResponseType(typeof(Template), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request) {
            var createdTemplate = await TemplateService.Create(request);
            return Ok(createdTemplate);
        }
    }
}

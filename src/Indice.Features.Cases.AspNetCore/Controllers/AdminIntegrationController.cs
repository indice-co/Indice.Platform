using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Integration endpoints with 3rd party systems.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/integrations")]
public class AdminIntegrationController : ControllerBase
{
    private readonly IContactProvider _customerIntegrationService;

    /// <inheritdoc/>
    public AdminIntegrationController(IContactProvider customerIntegrationService) {
        _customerIntegrationService = customerIntegrationService ?? throw new ArgumentNullException(nameof(customerIntegrationService));
    }

    /// <summary>Fetch customers.</summary>
    /// <param name="criteria">The customers criteria.</param>
    /// <param name="options"></param>
    /// <returns></returns>
    [HttpGet("contacts")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<Contact>))]
    public async Task<IActionResult> GetCustomers([FromQuery] ContactFilter criteria, [FromQuery] ListOptions options) {
        return Ok(await _customerIntegrationService.GetListAsync(User, ListOptions.Create(options, criteria)));
    }

    /// <summary>Fetch customer data for a specific case type code.</summary>
    /// <param name="customerId">The Id of the customer to the integrator's system.</param>
    /// <param name="caseTypeCode">The case type code.</param>
    /// <returns></returns>
    [HttpGet("contacts/{reference}/data/{caseTypeCode}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JsonNode))]
    public async Task<IActionResult> GetCustomerData([FromRoute] string customerId, [FromRoute] string caseTypeCode) {
        return Ok(await _customerIntegrationService.GetByReferenceAsync(User, customerId, caseTypeCode));
    }
}

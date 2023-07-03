using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
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
    private readonly ICustomerIntegrationService _customerIntegrationService;

    /// <inheritdoc/>
    public AdminIntegrationController(ICustomerIntegrationService customerIntegrationService) {
        _customerIntegrationService = customerIntegrationService ?? throw new ArgumentNullException(nameof(customerIntegrationService));
    }

    /// <summary>Fetch customers.</summary>
    /// <param name="criteria">The customers criteria.</param>
    /// <returns></returns>
    [HttpGet("customers")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CustomerDetails>))]
    public async Task<IActionResult> GetCustomers([FromQuery] SearchCustomerCriteria criteria) {
        return Ok(await _customerIntegrationService.GetCustomers(criteria));
    }

    /// <summary>Fetch customer data for a specific case type code.</summary>
    /// <param name="customerId">The Id of the customer to the integrator's system.</param>
    /// <param name="caseTypeCode">The case type code.</param>
    /// <returns></returns>
    [HttpGet("customers/{customerId}/data/{caseTypeCode}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerData))]
    public async Task<IActionResult> GetCustomerData([FromRoute] string customerId, [FromRoute] string caseTypeCode) {
        return Ok(await _customerIntegrationService.GetCustomerData(customerId, caseTypeCode));
    }

    /// <summary>Fetch the candidate Assignee for current case.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns></returns>
    [HttpGet("assignees")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Assignee>))]
    public async Task<IActionResult> GetAssignees([FromQuery] string caseId) {
        return Ok(await _customerIntegrationService.GetAssignees(caseId));
    }
}

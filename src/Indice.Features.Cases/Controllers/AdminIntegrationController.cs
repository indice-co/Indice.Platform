using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/integrations")]
    public class AdminIntegrationController : ControllerBase
    {
        private readonly ICustomerIntegrationService _customerIntegrationService;

        public AdminIntegrationController(ICustomerIntegrationService customerIntegrationService) {
            _customerIntegrationService = customerIntegrationService ?? throw new ArgumentNullException(nameof(customerIntegrationService));
        }

        [HttpGet("customers")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CustomerDetails>))]
        public async Task<IActionResult> GetCustomers([FromQuery] SearchCustomerCriteria criteria) {
            return Ok(await _customerIntegrationService.GetCustomers(criteria));
        }
    }
}

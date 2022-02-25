using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Api.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "lookups")]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
    [Route("api/timezones")]
    public class TimeZonesController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        public IActionResult GetTimeZones() {
            var timeZones = new HashSet<string>(TimeZoneInfo.GetSystemTimeZones().Select(timeZone => timeZone.Id)).OrderBy(x => x);
            return Ok(timeZones);
        }
    }
}

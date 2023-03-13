using System.Net.Mime;
using Indice.Features.Messages.Core.Models;
using Indice.Types;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Api.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "lookups")]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
[ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
[Route("api")]
public class LookupsController : ControllerBase
{
    private static readonly IEnumerable<Contact> _contacts = new List<Contact> {
        new Contact { Id = Guid.NewGuid(), FirstName = "Constantinos", LastName = " Leftheris", FullName = "Constantinos Leftheris", Email = "c.leftheris@indice.gr", PhoneNumber = "698xxxxxxx", Salutation = "Mr." },
        new Contact { Id = Guid.NewGuid(), FirstName = "Georgios", LastName = " Manoltzas", FullName = "Georgios Manoltzas", Email = "g.manoltzas@indice.gr", PhoneNumber = "699xxxxxxx", Salutation = "Mr." },
        new Contact { Id = Guid.NewGuid(), FirstName = "John", LastName = " Tsenes", FullName = "John Tsenes", Email = "itsenes@indice.gr", PhoneNumber = "697xxxxxxx", Salutation = "Mr." },
        new Contact { Id = Guid.NewGuid(), FirstName = "Thanos", LastName = " Panousis", FullName = "Thanos Panousis", Email = "thanos.panousis@indice.gr", PhoneNumber = "699xxxxxxx", Salutation = "Mr." },
        new Contact { Id = Guid.NewGuid(), FirstName = "Anna", LastName = " Livitsanou", FullName = "Anna Livitsanou", Email = "a.livitsanou@indice.gr", PhoneNumber = "699xxxxxxx", Salutation = "Mrs." },
        new Contact { Id = Guid.NewGuid(), FirstName = "Makis", LastName = " Stavropoulos", FullName = "Makis Stavropoulos", Email = "makis.stavropoulos@indice.gr", PhoneNumber = "699xxxxxxx", Salutation = "Mr." }
    };

    [HttpGet("system-timezones")]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    public IActionResult GetTimeZones() {
        var timeZones = new HashSet<string>(TimeZoneInfo.GetSystemTimeZones().Select(timeZone => timeZone.Id)).OrderBy(x => x);
        return Ok(timeZones);
    }

    [HttpGet("sample-contacts")]
    [ProducesResponseType(typeof(ResultSet<Contact>), StatusCodes.Status200OK)]
    public IActionResult GetSampleContacts([FromQuery] ListOptions options) {
        IEnumerable<Contact> result = _contacts.ToList();
        var searchTerm = options.Search?.ToLower();
        if (!string.IsNullOrWhiteSpace(searchTerm)) {
            result = _contacts.Where(x => x.FirstName.ToLower().Contains(searchTerm) || x.LastName.ToLower().Contains(searchTerm));
        }
        return Ok(result.AsQueryable().ToResultSet(options));
    }
}

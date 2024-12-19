using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminIntegrationHandler
{
    public static async Task<Results<Ok<ResultSet<Contact>>, NotFound>> GetCustomers(ClaimsPrincipal currentUser, IContactProvider customerIntegrationService, [AsParameters] ContactFilter criteria, [AsParameters] ListOptions listOptions) {
        var contacts = await customerIntegrationService.GetListAsync(currentUser, ListOptions.Create(listOptions, criteria));
        return TypedResults.Ok(contacts);
    }

    public static async Task<Results<Ok<JsonNode>, NotFound>> GetCustomerData(ClaimsPrincipal currentUser, IContactProvider customerIntegrationService, string reference, string caseTypeCode) {
        var contactData = await customerIntegrationService.GetByReferenceAsync(currentUser, reference, caseTypeCode);
        if (contactData == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(contactData);
    }
}

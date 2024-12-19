using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminIntegrationHandler
{
    public static async Task<Results<Ok<List<Contact>>, NotFound>> GetCustomers(IContactProvider customerIntegrationService, [AsParameters] ContactFilter criteria) {
        var customers = await customerIntegrationService.SearchAsync(criteria);
        if (customers == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(customers);
    }

    public static async Task<Results<Ok<ContactData>, NotFound>> GetCustomerData(IContactProvider customerIntegrationService, string customerId, string caseTypeCode) {
        var customerData = await customerIntegrationService.GetByReferenceAsync(customerId, caseTypeCode);
        if (customerData == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(customerData);
    }
}

using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminIntegrationHandler
{
    public static async Task<Results<Ok<IEnumerable<CustomerDetails>>, NotFound>> GetCustomers(ICustomerIntegrationService customerIntegrationService, [AsParameters] SearchCustomerCriteria criteria) {
        var customers = await customerIntegrationService.GetCustomers(criteria);
        if (customers == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(customers);
    }

    public static async Task<Results<Ok<CustomerData>, NotFound>> GetCustomerData(ICustomerIntegrationService customerIntegrationService, string customerId, string caseTypeCode) {
        var customerData = await customerIntegrationService.GetCustomerData(customerId, caseTypeCode);
        if (customerData == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(customerData);
    }
}

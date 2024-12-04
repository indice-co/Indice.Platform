using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminIntegrationHandler
{
    public static async Task<Results<Ok<List<CustomerDetails>>, NotFound>> GetCustomers(ICustomerIntegrationService customerIntegrationService, [AsParameters] SearchCustomerCriteria criteria) {
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

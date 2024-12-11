using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminIntegrationHandler
{
    public static async Task<Ok<List<CustomerDetails>>> GetCustomers(ICustomerIntegrationService customerIntegrationService, [AsParameters] SearchCustomerCriteria criteria) =>
        TypedResults.Ok(await customerIntegrationService.GetCustomers(criteria));

    public static async Task<Ok<CustomerData>> GetCustomerData(ICustomerIntegrationService customerIntegrationService, string customerId, string caseTypeCode) =>
        TypedResults.Ok(await customerIntegrationService.GetCustomerData(customerId, caseTypeCode));
}

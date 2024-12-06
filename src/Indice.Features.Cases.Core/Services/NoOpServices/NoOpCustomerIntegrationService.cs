using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services.NoOpServices;

internal class NoOpCustomerIntegrationService : ICustomerIntegrationService
{
    public Task<List<CustomerDetails>> GetCustomers(SearchCustomerCriteria criteria) =>
        throw new NotImplementedException("Implement this interface with your own data sources.");

    public Task<CustomerData> GetCustomerData(string customerId, string caseTypeCode) =>
        throw new NotImplementedException("Implement this interface with your own data sources.");
}

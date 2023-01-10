using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Services.NoOpServices
{
    internal class NoOpCustomerIntegrationService : ICustomerIntegrationService
    {
        public Task<IEnumerable<CustomerDetails>> GetCustomers(SearchCustomerCriteria criteria) =>
            throw new NotImplementedException("Implement this interface with your own data sources.");

        public Task<CustomerData> GetCustomerData(string customerId, string caseTypeCode) =>
            throw new NotImplementedException("Implement this interface with your own data sources.");
    }
}

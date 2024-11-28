using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Interfaces;

/// <summary>The customer service that will be integrated from the consumer for customer queries from its own storage/apis.</summary>
public interface ICustomerIntegrationService
{
    /// <summary>Get a list of customers.</summary>
    /// <param name="criteria">The criteria to search for.</param>
    /// <returns></returns>
    Task<IEnumerable<CustomerDetails>> GetCustomers(SearchCustomerCriteria criteria);
    /// <summary>Get Customer Data for a specific case type.</summary>
    /// <param name="customerId">The Id of the customer.</param>
    /// <param name="caseTypeCode">The case type code.</param>
    /// <returns></returns>
    Task<CustomerData> GetCustomerData(string customerId, string caseTypeCode);
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The customer service that will be integrated from the consumer for customer queries from its own storage/apis.
    /// </summary>
    public interface ICustomerIntegrationService
    {
        /// <summary>
        /// Get a list of customers.
        /// </summary>
        /// <param name="criteria">The criteria to search for.</param>
        /// <returns></returns>
        Task<IEnumerable<CustomerDetails>> GetCustomers(SearchCustomerCriteria criteria);
    }
}

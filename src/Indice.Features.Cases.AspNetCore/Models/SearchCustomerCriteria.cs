namespace Indice.Features.Cases.Models
{
    /// <summary>
    /// The criteria to search for a customer to the consumer/integration service.
    /// </summary>
    public class SearchCustomerCriteria
    {
        /// <summary>
        /// The Id of the customer as provided by the consumer/integrator.
        /// </summary>
        public string? CustomerId { get; set; }
        public string? CaseTypeCode { get; set; }
    }
}
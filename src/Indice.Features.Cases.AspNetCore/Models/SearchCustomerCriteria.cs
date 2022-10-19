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

        /// <summary>
        /// The tax identification of the customer.
        /// </summary>
        public string? TaxId { get; set; }
        
        /// <summary>
        /// Use to return legal customers
        /// </summary>
        public bool IncludeLegal { get; set; } = false;
    }
}
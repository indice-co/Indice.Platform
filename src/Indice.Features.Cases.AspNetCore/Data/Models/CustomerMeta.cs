namespace Indice.Features.Cases.Data.Models
{
    /// <summary>
    /// Customer metadata related to the Customer the case is created for.
    /// The customer may create the case himself or the case could be 
    /// created on this customers behalf
    /// </summary>
    public class CustomerMeta
    {
        public string? UserId { get; set; }
        public string? CustomerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}"; // this is an unmapped EFCore property, do not use for querying
    }
}
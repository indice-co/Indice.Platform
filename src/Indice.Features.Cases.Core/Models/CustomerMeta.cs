namespace Indice.Features.Cases.Core.Models;

/// <summary>
/// Customer metadata related to the Customer the case is created for.
/// The customer may create the case himself or the case could be 
/// created on this customers behalf
/// </summary>
public class CustomerMeta
{
    /// <summary>The related user id</summary>
    public string? UserId { get; set; }
    /// <summary>The customer id</summary>
    public string? CustomerId { get; set; }
    /// <summary>The first name</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name</summary>
    public string? LastName { get; set; }
    /// <summary>full name</summary>
    public string? FullName => $"{FirstName} {LastName}"; // this is an unmapped EFCore property, do not use for querying
}

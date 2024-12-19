namespace Indice.Features.Cases.Core.Models;

/// <summary>
/// Customer metadata related to the Customer the case is created for.
/// The customer may create the case himself or the case could be 
/// created on this customers behalf
/// </summary>
public class ContactMeta
{
    /// <summary>The related user id</summary>
    public string? UserId { get; set; }
    /// <summary>Can be the customer id or something related to an external system correlation id</summary>
    /// <remarks>In case of non external system this should be the same as the userid</remarks>
    public string? Reference { get; set; }
    /// <summary>The first name</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name</summary>
    public string? LastName { get; set; }
    /// <summary>full name</summary>
    public string? FullName => $"{FirstName} {LastName}"; // this is an unmapped EFCore property, do not use for querying
}


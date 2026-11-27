using System.Text.Json.Serialization;

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
    /// <summary>The Tax identification number</summary>
    public string? Tin { get; set; }
    /// <summary>The permission level of the contact</summary>
    public ContactPermission Permission { get; set; } = ContactPermission.Reader;
    /// <summary>full name</summary>
    public string FullName => $"{FirstName} {LastName}"; // this is an unmapped EFCore property, do not use for querying
}

/// <summary>
/// The permission level of the contact regarding the case.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContactPermission
{
    /// <summary>
    /// Reader permission allows the contact to view the case and its details, but not make any modifications.
    /// </summary>
    Reader = 0,
    /// <summary>
    /// Represents a contributor. A contributor can view and make modifications to the case.
    /// </summary>
    Contributor = 1,
    /// <summary>
    /// Represents the owner role. An owner can do everything a contributor can, and can also discard the case while it is a draft.
    /// Co-owners and permissions are managed by the back-office only.
    /// </summary>
    Owner = 2
}
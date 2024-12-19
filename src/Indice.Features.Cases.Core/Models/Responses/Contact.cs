namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>
/// The customer response object that contains information from the integration system. Properties that have no direct mapping to this model
/// can be added to <see cref="Metadata"/> dictionary.
/// </summary>
public class Contact
{
    /// <summary>The Id of the customer as created to our Identity provider.</summary>
    public string? UserId { get; set; }

    /// <summary>The external system correlation key. Can be the Id of the customer as provided by the consumer/integrator.</summary>
    public string? Reference { get; set; }

    /// <summary>The first name of the customer.</summary>
    public string? FirstName { get; set; }

    /// <summary>The last name of the customer.</summary>
    public string? LastName { get; set; }

    /// <summary>The Id of the group the customer belongs.</summary>
    public string? GroupId { get; set; }

    /// <summary>Any extra metadata with consumer/integrator business logic.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
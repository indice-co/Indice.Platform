using Indice.Security;

namespace Indice.Features.Messages.Core;

/// <summary>Options for configuring <see cref="ICustomerIntegrationService"/>.</summary>
public class CustomerIdentityResolverOptions
{
    /// <summary>The base address of the identity system.</summary>
    public Uri? BaseAddress { get; set; }
    /// <summary>The client id used to communicate with Identity Server.</summary>
    public string? ClientId { get; set; }
    /// <summary>The client secret used to communicate with Identity Server.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The claim type used to identify the user. Defaults to <i>sub</i>.</summary>
    public string UserClaimType { get; set; } = BasicClaimTypes.Subject;
    /// <summary>Indicates that the recipient id is not based on the default claim type used to identify the contact.</summary>
    public bool HasCustomRecipientId => UserClaimType != BasicClaimTypes.Subject;
}

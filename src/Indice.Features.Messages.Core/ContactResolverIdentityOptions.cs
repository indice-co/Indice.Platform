using Indice.Features.Messages.Core.Services;

namespace Indice.Features.Messages.Core;

/// <summary>Options for configuring <see cref="ContactResolverIdentity"/>.</summary>
public class ContactResolverIdentityOptions
{
    /// <summary>The base address of the identity system.</summary>
    public Uri BaseAddress { get; set; }
    /// <summary>The client id used to communicate with Identity Server.</summary>
    public string ClientId { get; set; }
    /// <summary>The client secret used to communicate with Identity Server.</summary>
    public string ClientSecret { get; set; }
}

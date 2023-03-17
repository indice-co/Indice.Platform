namespace Indice.Features.Multitenancy.Core;

/// <summary>Resolves the host to a tenant identifier.</summary>
public interface ITenantResolutionStrategy
{
    /// <summary>Gets the tenant identifier.</summary>
    Task<string> GetTenantIdentifierAsync();
}

namespace Indice.Features.Multitenancy.Core
{
    /// <summary>Provides access to the resolved tenant (cached) for the current operation (request).</summary>
    /// <typeparam name="TTenant">The type of tenant.</typeparam>
    public interface ITenantAccessor<TTenant> where TTenant : Tenant
    {
        /// <summary>The current tenant.</summary>
        TTenant Tenant { get; }
    }
}

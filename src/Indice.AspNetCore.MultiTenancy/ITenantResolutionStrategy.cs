using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indice.AspNetCore.MultiTenancy
{
    /// <summary>
    /// Resolve the host to a tenant identifier
    /// </summary>
    public interface ITenantResolutionStrategy
    {
        /// <summary>
        /// Get the tenant identifier
        /// </summary>
        /// <returns></returns>
        Task<string> GetTenantIdentifierAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.AspNetCore.MultiTenancy
{
    /// <summary>
    /// Constants
    /// </summary>
    internal class Constants
    {
        public const string HttpContextTenantKey = "TENANT_KEY";
        public const string HttpRequestHeaderName = "X-Tenant-Id";
        public const string RouteParameterName = "tenantId";
    }

    /// <summary>
    /// The Jwt wellknown claim types 
    /// </summary>
    internal class JwtClaimTypesInternal
    {
        public const string Subject= "sub";
        public const string ClientId = "client_id";

        /// <summary>
        /// Identifies a machine (worker) principal as a trusted system account with administrative priviledges.
        /// </summary>
        public const string System = "system";
    }
}

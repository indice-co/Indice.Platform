namespace Indice.Features.Multitenancy.AspNetCore
{
    /// <summary>Constants.</summary>
    internal class Constants
    {
        public const string HttpContextTenantKey = "TENANT_KEY";
        public const string HttpRequestHeaderName = "X-Tenant-Id";
        public const string RouteParameterName = "tenantId";
    }

    /// <summary>The JWT well-known claim types.</summary>
    internal class JwtClaimTypesInternal
    {
        /// <summary>User id.</summary>
        public const string Subject= "sub";
        /// <summary>Client application id.</summary>
        public const string ClientId = "client_id";
        /// <summary>Identifies a machine (worker) principal as a trusted system account with administrative privileges.</summary>
        public const string System = "system";
    }
}

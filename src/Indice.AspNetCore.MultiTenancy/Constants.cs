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
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Security
{
    /// <summary>
    /// Common Claim types used in all Indice applications.
    /// </summary>
    public static class BasicClaimTypes
    {
        /// <summary>
        /// Basic Claim types prefix.
        /// </summary>
        public const string Prefix = "indice_";

        /// <summary>
        /// Identiyfies a physical user principal as a trusted account with administrative priviledges.
        /// </summary>
        public const string Admin = "admin";

        /// <summary>
        /// Identiyfies a machine (worker) principal as a trusted system account with administrative priviledges.
        /// </summary>
        public const string System = "system";

        /// <summary>
        /// Identifier for the current tenant.
        /// </summary>
        public const string TenantId = "tenantId";

        /// <summary>
        /// Alternate key for the current tenant
        /// </summary>
        public const string TenantAlias = "tenantAlias";

        /// <summary>
        /// All possible user related claims
        /// </summary>
        public static readonly string[] UserClaims = {
            "sub",
            "name",
            "email",
            "phone",
            "phone_verified",
            "email_verified",
            "family_name",
            "given_name",
            "role",
            Admin
        };
    }
}

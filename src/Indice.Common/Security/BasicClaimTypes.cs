using System;

namespace Indice.Security
{
    /// <summary>
    /// Common claim types used in all Indice applications.
    /// </summary>
    public static class BasicClaimTypes
    {
        /// <summary>
        /// Basic Claim types prefix.
        /// </summary>
        public const string Prefix = "indice_";
        /// <summary>
        /// Identifies a physical user principal as a trusted account with administrative priviledges.
        /// </summary>
        public const string Admin = "admin";
        /// <summary>
        /// Identifies a machine (worker) principal as a trusted system account with administrative priviledges.
        /// </summary>
        public const string System = "system";
        /// <summary>
        /// Identifier for the current tenant.
        /// </summary>
        public const string TenantId = "tenantId";
        /// <summary>
        /// Alternate key for the current tenant.
        /// </summary>
        public const string TenantAlias = "tenantAlias";
        /// <summary>
        /// User id.
        /// </summary>
        public const string Subject = "sub";
        /// <summary>
        /// User email.
        /// </summary>
        public const string Email = "email";
        /// <summary>
        /// User last name.
        /// </summary>
        public const string FamilyName = "family_name";
        /// <summary>
        /// User first name.
        /// </summary>
        public const string GivenName = "given_name";
        /// <summary>
        /// Username.
        /// </summary>
        public const string Name = "name";
        /// <summary>
        /// Full name.
        /// </summary>
        public const string FullName = "full_name";
        /// <summary>
        /// String from the time zone database (http://www.twinsun.com/tz/tz-link.htm) representing
        /// the End-User's time zone. For example, Europe/Paris or America/Los_Angeles.
        /// </summary>
        public const string ZoneInfo = "zoneinfo";
        /// <summary>
        /// Client Id (calling application)
        /// </summary>
        public const string ClientId = "client_id";
        /// <summary>
        /// The <see cref="DateTime"/> when the user password will expire.
        /// </summary>
        public const string PasswordExpirationDate = "password_expiration_date";
        /// <summary>
        /// Defines the period in which a password expires.
        /// </summary>
        public const string PasswordExpirationPolicy = "password_expiration_policy";
        /// <summary>
        /// Defines a standard OTP code used for bypassing OTP verification for developement environment.
        /// </summary>
        public const string DeveloperTotp = "developer_totp";
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
            Admin,
            PasswordExpirationDate
        };
    }
}

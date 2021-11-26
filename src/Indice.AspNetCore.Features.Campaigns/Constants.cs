using System.Reflection;
using Indice.Security;

namespace Indice.AspNetCore.Features.Campaigns
{
    /// <summary>
    /// Contant values for Campaigns API.
    /// </summary>
    public static class CampaignsApi
    {
        /// <summary>
        /// The assembly name.
        /// </summary>
        public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        /// <summary>
        /// Authentication scheme name used by Campaigns API.
        /// </summary>
        public const string AuthenticationScheme = "Bearer";
        /// <summary>
        /// Campaigns API scope.
        /// </summary>
        public const string Scope = "campaigns";
        /// <summary>
        /// Default database schema.
        /// </summary>
        public const string DatabaseSchema = "campaign";

        /// <summary>
        /// Campaigns API policies.
        /// </summary>
        public static class Policies 
        {
            /// <summary>
            /// A user must have the <i>Admin</i> flag or own the <see cref="BasicRoleNames.AdminUICampaignsManager"/> role.
            /// </summary>
            public const string BeCampaignsManager = nameof(BeCampaignsManager);
        }
    }
}

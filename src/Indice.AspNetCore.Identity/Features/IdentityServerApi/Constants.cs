using System.Reflection;
using Indice.Security;

namespace Indice.AspNetCore.Identity.Api.Security
{
    /// <summary>Constants for IdentityServer API feature.</summary>
    public static class IdentityServerApi
    {
        /// <summary>The assembly name.</summary>
        public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        /// <summary>Authentication scheme name used by IdentityServer local API.</summary>
        public const string AuthenticationScheme = "IdentityServerApiAccessToken";
        /// <summary>Identity API scope.</summary>
        public const string Scope = "identity";

        /// <summary>Contains constants for property keys.</summary>
        public static class PropertyKeys 
        {
            /// <summary>Key used in properties table for keeping translations for an object.</summary>
            public const string Translation = "translations";
            /// <summary>Key used in properties table for keeping translations for an object.</summary>
            public const string UiConfig = "ui-config";
        }
        
        /// <summary>Identity API sub-scopes.</summary>
        public static class SubScopes
        {
            /// <summary>A scope that allows managing clients on IdentityServer.</summary>
            public const string Clients = "identity:clients";
            /// <summary>A scope that allows managing users on IdentityServer.</summary>
            public const string Users = "identity:users";
        }

        /// <summary>Identity API policies.</summary>
        public static class Policies
        {
            /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/> or <see cref="BasicRoleNames.AdminUIAdministrator"/> roles.</summary>
            public const string BeAdmin = nameof(BeAdmin);
            /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/> or <see cref="BasicRoleNames.AdminUIUsersReader"/> roles.</summary>
            public const string BeUsersReader = nameof(BeUsersReader);
            /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/> or <see cref="BasicRoleNames.AdminUIUsersWriter"/> roles.</summary>
            public const string BeUsersWriter = nameof(BeUsersWriter);
            /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/> or <see cref="BasicRoleNames.AdminUIClientsReader"/> roles.</summary>
            public const string BeClientsReader = nameof(BeClientsReader);
            /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/> or <see cref="BasicRoleNames.AdminUIClientsWriter"/> roles.</summary>
            public const string BeClientsWriter = nameof(BeClientsWriter);
            /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/>, <see cref="BasicRoleNames.AdminUIUsersReader"/> or <see cref="BasicRoleNames.AdminUIClientsReader"/> roles.</summary>
            public const string BeUsersOrClientsReader = nameof(BeUsersOrClientsReader);
        }
    }
}

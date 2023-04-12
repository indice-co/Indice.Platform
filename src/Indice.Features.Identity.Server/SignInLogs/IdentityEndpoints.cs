using Indice.Security;

namespace Indice.Features.Identity.Server;

/// <summary>Constants for IdentityServer API feature.</summary>
public static partial class IdentityEndpoints
{
    /// <summary>Identity API sub-scopes.</summary>
    public static partial class SubScopes
    {
        /// <summary>A scope that allows managing clients on IdentityServer.</summary>
        public const string Logs = "identity:logs";
    }

    /// <summary>Identity API policies.</summary>
    public static partial class Policies
    {
        /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/> or <see cref="BasicRoleNames.AdminUIUsersReader"/> roles.</summary>
        public const string BeLogsReader = nameof(BeLogsReader);
        /// <summary>A user must have the 'Admin' flag or own one of the <see cref="BasicRoleNames.Administrator"/>, <see cref="BasicRoleNames.AdminUIAdministrator"/> or <see cref="BasicRoleNames.AdminUIUsersWriter"/> roles.</summary>
        public const string BeLogsWriter = nameof(BeLogsWriter);
    }

    /// <summary>Feature flags for Identity Server API.</summary>
    public static partial class Features
    {
    }
}

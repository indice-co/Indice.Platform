using System.Reflection;
using Indice.Features.Identity.Core;
using Indice.Security;

namespace Indice.Features.Identity.Server;

/// <summary>Constants for IdentityServer API feature.</summary>
public static partial class IdentityEndpoints
{
    /// <summary>The assembly name.</summary>
    public static readonly string? AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
    /// <summary>Authentication scheme name used by IdentityServer local API.</summary>
    public const string AuthenticationScheme = ExtendedIdentityConstants.ApiAuthenticationScheme;
    /// <summary>Identity API scope.</summary>
    public const string Scope = "identity";

    /// <summary>Identity API sub-scopes.</summary>
    public static partial class SubScopes
    {
        /// <summary>A scope that allows managing clients on IdentityServer.</summary>
        public const string Clients = "identity:clients";
        /// <summary>A scope that allows managing users on IdentityServer.</summary>
        public const string Users = "identity:users";
    }

    /// <summary>Identity API policies.</summary>
    public static partial class Policies
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

    /// <summary>Feature flags for Identity Server API.</summary>
    public static partial class Features
    {
        /// <summary>Enables API for public registration API.</summary>
        public const string PublicRegistration = nameof(PublicRegistration);
        /// <summary>Enables API for public registration API.</summary>
        public const string DashboardMetrics = nameof(DashboardMetrics);
        /// <summary>Enables API for public registration API.</summary>
        public const string RssFeed = nameof(RssFeed);
    }

    /// <summary>Rate limiting config for Identity Server API.</summary>
    internal static partial class RateLimiter 
    {
        public static IReadOnlyList<string> Endpoints { get; } = new List<string> {
            "account/forgot-password",
            "account/forgot-password/confirmation",
            "account/password-options",
            "account/username-exists",
            "account/validate-password",
            "totp",
            "account/calling-codes",
            "my/account/picture"
        };

        public static class Policies
        {
            public static readonly string ForgotPassword = Endpoints[0];
            public static readonly string ForgotPasswordConfirmation = Endpoints[1];
            public static readonly string PasswordOptions = Endpoints[2];
            public static readonly string UserNameExists = Endpoints[3];
            public static readonly string ValidatePassword = Endpoints[4];
            public static readonly string Totp = Endpoints[5];
            public static readonly string CallingCodes = Endpoints[6];
            public static readonly string UploadPicture = Endpoints[7];
            
        }
    }
}

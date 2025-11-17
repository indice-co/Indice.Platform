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
        /// <summary>A scope that allows using the totp endpoints on IdentityServer.</summary>
        public const string Totp = "identity:totp";
        /// <summary>A scope that allows reading the secret for a user device.</summary>
        public const string UserDeviceSecret= "identity:users.devices.secret.read";
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
        /// <summary>A user must have the 'Admin' flag or own the scope <see cref="IdentityEndpoints.SubScopes.UserDeviceSecret"/> or has the <see cref="BeUsersReader"/> policy.</summary>
        public const string BeUserDeviceSecretReader = nameof(BeUserDeviceSecretReader);
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
            "my/account/picture",
            "my/account/email",
            "my/account/phone-number",
            "my/account/email/change",
            "my/account/phone-number/change",
            "my/account/email/confirmation",
            "my/account/phone-number/confirmation",
            "my/account/email/change-confirmation",
            "my/account/phone-number/change-confirmation",
            "secure-page", // this is the generic policy name for any public Razor Pages that require throttling
            "register",
            "forgot-password",
            "login"
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
            public static readonly string UpdateEmail = Endpoints[8];
            public static readonly string UpdatePhoneNumber = Endpoints[9];
            public static readonly string ChangeEmail = Endpoints[10];
            public static readonly string ChangePhoneNumber = Endpoints[11];
            public static readonly string EmailConfirmation = Endpoints[12];
            public static readonly string PhoneNumberConfirmation = Endpoints[13];
            public static readonly string EmailChangeConfirmation = Endpoints[14];
            public static readonly string ChangePhoneNumberConfirmation = Endpoints[15];
            public static readonly string PublicPage = Endpoints[16];
        }
    }
}

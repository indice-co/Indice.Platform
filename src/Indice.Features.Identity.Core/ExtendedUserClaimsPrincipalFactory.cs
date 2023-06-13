using System.Security.Claims;
using IdentityModel;
using Indice.AspNetCore.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core;

/// <summary>Generate the claims for a user. Extends the default principal created by the IdentityServer with custom claims.</summary>
public class ExtendedUserClaimsPrincipalFactory : ExtendedUserClaimsPrincipalFactory<User, IdentityRole>
{
    /// <summary>Constructor for the extender user claims principal factory.</summary>
    /// <param name="userManager">The <see cref="ExtendedUserManager{TUser}"/> to retrieve user information from.</param>
    /// <param name="roleManager">The <see cref="RoleManager{TRole}"/> to retrieve a user's roles from.</param>
    /// <param name="optionsAccessor">The configured <see cref="IdentityOptions"/>.</param>
    /// <param name="deviceIdResolver">An abstraction used to specify the way to resolve the device identifier using various ways.</param>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    public ExtendedUserClaimsPrincipalFactory(
        ExtendedUserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDeviceIdResolver deviceIdResolver,
        IHttpContextAccessor httpContextAccessor
    ) : base(userManager, roleManager, optionsAccessor, deviceIdResolver, httpContextAccessor) { }

    /// <summary>Generates the claims for a user.</summary>
    /// <param name="user">The user to create a <see cref="ClaimsIdentity"/> from.</param>
    /// <returns>The claim-based identity of the user.</returns>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user) => await base.GenerateClaimsAsync(user);
}

/// <summary>Generate the claims for a user. Extends the default principal created by the IdentityServer with custom claims.</summary>
public class ExtendedUserClaimsPrincipalFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser, TRole>
    where TUser : User
    where TRole : IdentityRole
{
    const string ISOFORMAT = "yyyy-MM-dd\\THH:mm:ss.fffK"; //ISO-8601 used by JavaScript (ALWAYS UTC)
    private readonly IDeviceIdResolver _deviceIdResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Constructor for the extender user claims principal factory.</summary>
    /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve user information from.</param>
    /// <param name="roleManager">The <see cref="RoleManager{TRole}"/> to retrieve a user's roles from.</param>
    /// <param name="optionsAccessor">The configured <see cref="IdentityOptions"/>.</param>
    /// <param name="deviceIdResolver">An abstraction used to specify the way to resolve the device identifier using various ways.</param>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    public ExtendedUserClaimsPrincipalFactory(
        ExtendedUserManager<TUser> userManager,
        RoleManager<TRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDeviceIdResolver deviceIdResolver,
        IHttpContextAccessor httpContextAccessor
    ) : base(userManager, roleManager, optionsAccessor) {
        _deviceIdResolver = deviceIdResolver ?? throw new ArgumentNullException(nameof(deviceIdResolver));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>Generates the claims for a user.</summary>
    /// <param name="user">The user to create a <see cref="ClaimsIdentity"/> from.</param>
    /// <returns>The claim-based identity of the user.</returns>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user) {
        // https://github.com/aspnet/AspNetCore/blob/master/src/Identity/Extensions.Core/src/UserClaimsPrincipalFactory.cs#L135
        var identity = await base.GenerateClaimsAsync(user);
        var additionalClaims = new List<Claim>();
        if (!identity.HasClaim(x => x.Type == BasicClaimTypes.Admin)) {
            var isAdmin = user.Admin;
            if (!isAdmin) {
                if (identity.HasClaim(x => x.Type == JwtClaimTypes.Role)) {
                    isAdmin = identity.HasClaim(JwtClaimTypes.Role, BasicRoleNames.Administrator);
                } else {
                    var roles = (await UserManager.GetRolesAsync(user)).Select(role => new Claim(JwtClaimTypes.Role, role));
                    isAdmin = roles.Where(x => x.Value == BasicRoleNames.Administrator).Any();
                }
            }
            additionalClaims.Add(new Claim(BasicClaimTypes.Admin, isAdmin.ToString().ToLower(), ClaimValueTypes.Boolean));
        }
        if (!identity.HasClaim(x => x.Type == BasicClaimTypes.PasswordExpired) && user.HasExpiredPassword()) {
            additionalClaims.Add(new Claim(BasicClaimTypes.PasswordExpired, "true", ClaimValueTypes.Boolean));
        }
        if (!identity.HasClaim(x => x.Type == BasicClaimTypes.PasswordExpirationDate) && user.PasswordExpirationDate.HasValue) {
            additionalClaims.Add(new Claim(BasicClaimTypes.PasswordExpirationDate, ToISOString(user.PasswordExpirationDate.Value.UtcDateTime), ClaimValueTypes.DateTime));
        }
        if (!identity.HasClaim(x => x.Type == BasicClaimTypes.PasswordExpirationPolicy) && user.PasswordExpirationPolicy.HasValue) {
            additionalClaims.Add(new Claim(BasicClaimTypes.PasswordExpirationPolicy, user.PasswordExpirationPolicy.ToString()));
        }
        var deviceId = await _deviceIdResolver.Resolve();
        if (!string.IsNullOrWhiteSpace(deviceId.Value)) {
            additionalClaims.Add(new Claim(BasicClaimTypes.DeviceId, deviceId.Value));
        }
        var ipAddress = _httpContextAccessor.HttpContext.GetClientIpAddress()?.ToString();
        if (!string.IsNullOrWhiteSpace(ipAddress)) {
            additionalClaims.Add(new Claim(BasicClaimTypes.IPAddress, ipAddress));
        }
        identity.AddClaims(additionalClaims);
        return identity;
    }

    private static string ToISOString(DateTime dateTime, bool useLocal = false) {
        if (!useLocal && dateTime.Kind == DateTimeKind.Local) {
            // If d is LT or you don't want LocalTime -> convert to UTC and always add K format always add 'Z' postfix.
            return dateTime.ToUniversalTime().ToString(ISOFORMAT);
        } else {
            //If d is already UTC K format add 'Z' postfix, if d is LT K format add +/-TIMEOFFSET.
            return dateTime.ToString(ISOFORMAT);
        }
    }
}

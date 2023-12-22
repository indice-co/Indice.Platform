using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Security;

namespace Indice.Features.Identity.Core;

/// <summary>Extends the implementation of <see cref="IProfileService"/> and validates the user based on ASP.NET Identity and custom rules.</summary>
/// <typeparam name="TInner">The type is decorated.</typeparam>
/// <remarks>Creates a new instance of <see cref="ExtendedProfileService{TUser}"/>.</remarks>
/// <param name="profileService"> This interface allows IdentityServer to connect to your user and profile store.</param>
/// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
public class ExtendedProfileService<TInner>(
    TInner profileService, 
    ExtendedUserManager<User> userManager) : IProfileService where TInner : IProfileService
{
    private readonly IProfileService _inner = profileService ?? throw new ArgumentNullException(nameof(profileService));
    private readonly ExtendedUserManager<User> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    /// <inheritdoc />
    public async Task GetProfileDataAsync(ProfileDataRequestContext context) { 
        await _inner.GetProfileDataAsync(context);
        // TODO: We could also configure it from outside of the service.
        var claimsToIssue = new List<string>([
            BasicClaimTypes.IPAddress, 
            BasicClaimTypes.DeviceId
        ]);
        foreach (var claim in claimsToIssue) {
            var userClaim = context.Subject.Claims.FirstOrDefault(x => x.Type == claim);
            var isAlreadyIssued = context.IssuedClaims.FirstOrDefault(x => x.Type == claim) is not null;
            if (userClaim is not null && !isAlreadyIssued) {
                context.IssuedClaims.Add(userClaim);
            }
        }
    }

    /// <inheritdoc />
    public async Task IsActiveAsync(IsActiveContext context) {
        await _inner.IsActiveAsync(context);
        if (!context.IsActive) {
            return;
        }
        // If missing subject is missing do not check user specifics.
        var subject = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(subject)) {
            context.IsActive = true;
            return;
        }
        var user = await _userManager.FindByIdAsync(subject);
        var isActive = user != null
                    && !user.IsLockedOut() // This forces existing tokens to fail upon refresh or introspection. Not sure if we should.
                    && !user.Blocked;
        context.IsActive = isActive;
    }
}

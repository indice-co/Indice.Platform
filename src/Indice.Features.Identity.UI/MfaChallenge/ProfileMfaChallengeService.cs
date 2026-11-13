using System.Security.Claims;
#if NET9_0_OR_GREATER
using Duende.IdentityModel;
using Duende.IdentityServer.Services;
#else
using IdentityModel;
using IdentityServer4.Services;
#endif
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI;

/// <summary>Default implementation of <see cref="IProfileMfaChallengeService"/>, based on the code methods (SMS, Viber, email, authenticator app) the user has activated.</summary>
public class ProfileMfaChallengeService : IProfileMfaChallengeService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ExtendedUserManager<User> _userManager;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly IAuthenticationMethodProvider _authenticationMethodProvider;
    private readonly TotpServiceFactory _totpServiceFactory;
    private readonly IUserSession _userSession;
    private readonly TimeProvider _timeProvider;
    private readonly IdentityUIOptions _options;
    private readonly ILogger<ProfileMfaChallengeService> _logger;

    /// <summary>Creates a new instance of <see cref="ProfileMfaChallengeService"/>.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="authenticationMethodProvider">Abstracts interaction with system's various authentication methods.</param>
    /// <param name="totpServiceFactory">A factory service that contains methods to create various TOTP services.</param>
    /// <param name="userSession">The IdentityServer user session, used to bind the MFA challenge to the current login session.</param>
    /// <param name="timeProvider">Provides the current time.</param>
    /// <param name="options">Configuration options for Identity UI.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public ProfileMfaChallengeService(
        IHttpContextAccessor httpContextAccessor,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IAuthenticationMethodProvider authenticationMethodProvider,
        TotpServiceFactory totpServiceFactory,
        IUserSession userSession,
        TimeProvider timeProvider,
        IOptions<IdentityUIOptions> options,
        ILogger<ProfileMfaChallengeService> logger
    ) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _authenticationMethodProvider = authenticationMethodProvider ?? throw new ArgumentNullException(nameof(authenticationMethodProvider));
        _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

    /// <inheritdoc />
    public bool AllowMethodSelection => _authenticationMethodProvider.AllowMfaChannelDowngrade;

    /// <inheritdoc />
    public virtual async Task<ProfileMfaChallengeStatus> GetStatusAsync(User user) {
        ArgumentNullException.ThrowIfNull(user);
        if (!await _signInManager.IsTwoFactorEnabledAsync(user)) {
            return ProfileMfaChallengeStatus.NotRequired;
        }
        var sessionId = await _userSession.GetSessionIdAsync();
        if (string.IsNullOrWhiteSpace(sessionId)) {
            _logger.LogWarning("Profile MFA challenge blocked for user {UserId}: no login session id is available.", user.Id);
            return ProfileMfaChallengeStatus.Blocked;
        }
        if (await IsSatisfiedAsync(user, sessionId)) {
            return ProfileMfaChallengeStatus.Satisfied;
        }
        if ((await GetMethodsAsync(user)).Length == 0) {
            _logger.LogWarning("Profile MFA challenge blocked for user {UserId}: MFA is enabled but no code based method is available.", user.Id);
            return ProfileMfaChallengeStatus.Blocked;
        }
        return ProfileMfaChallengeStatus.Required;
    }

    /// <inheritdoc />
    public virtual async Task<AuthenticationMethod[]> GetMethodsAsync(User user) {
        ArgumentNullException.ThrowIfNull(user);
        var methods = await _authenticationMethodProvider.GetAllMethodsForUserAsync(user);
        return methods.Where(IsCodeBased).ToArray();
    }

    /// <inheritdoc />
    public virtual async Task<AuthenticationMethod?> FindMethodAsync(User user, string? code) {
        var methods = await GetMethodsAsync(user);
        if (!string.IsNullOrWhiteSpace(code) && AllowMethodSelection) {
            var selected = methods.FirstOrDefault(method => string.Equals(method.Code, code, StringComparison.OrdinalIgnoreCase));
            if (selected is not null) {
                return selected;
            }
        }
        return methods.FirstOrDefault();
    }

    /// <inheritdoc />
    public virtual async Task<TotpResult> SendCodeAsync(User user, AuthenticationMethod method) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(method);
        if (!await IsAvailableForUserAsync(user, method)) {
            return TotpResult.ErrorResult(_userManager.MessageDescriber.MfaValidationError);
        }
        if (!method.SupportsDeliveryChannel()) {
            return TotpResult.SuccessResult;
        }
        var totpService = _totpServiceFactory.Create<User>();
        var tokenProvider = method.GetTokenProvider()!;
        var purpose = TotpConstants.TokenGenerationPurpose.ProfileMfaChallenge;
        var channel = method.GetDeliveryChannel();
        var result = channel == TotpDeliveryChannel.Email
            ? await totpService.SendAsync(message => message.ToUser(user)
                                                            .WithMessage(_userManager.MessageDescriber.MfaEmailBody)
                                                            .UsingEmail("EmailMfaOtpCode")
                                                            .UsingTokenProvider(tokenProvider)
                                                            .WithSubject(_userManager.MessageDescriber.MfaEmailSubject)
                                                            .WithPurpose(purpose))
            : await totpService.SendAsync(message => message.ToUser(user)
                                                            .WithMessage(_userManager.MessageDescriber.MfaSmsBody)
                                                            .UsingDeliveryChannel(channel)
                                                            .UsingTokenProvider(tokenProvider)
                                                            .WithSubject(_userManager.MessageDescriber.MfaSmsSubject)
                                                            .WithPurpose(purpose));
        if (result.Success) {
            _logger.LogInformation("Profile MFA challenge code sent to user {UserId} using method {Method}.", user.Id, method.Code);
        }
        return result;
    }

    /// <inheritdoc />
    public virtual async Task<ProfileMfaChallengeVerifyResult> VerifyCodeAsync(User user, AuthenticationMethod method, string code) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(method);
        if (_userManager.SupportsUserLockout && await _userManager.IsLockedOutAsync(user)) {
            return ProfileMfaChallengeVerifyResult.LockedOut;
        }
        var sanitizedCode = code?.Replace(" ", string.Empty).Trim();
        var sessionId = await _userSession.GetSessionIdAsync();
        if (string.IsNullOrEmpty(sanitizedCode) || string.IsNullOrWhiteSpace(sessionId) || !await IsAvailableForUserAsync(user, method)) {
            return ProfileMfaChallengeVerifyResult.Failed;
        }
        // Verification does not update the security stamp (same as the MFA login page), so an MFA challenge that is not followed by a change keeps the other sessions of the user signed in.
        var verified = await _userManager.VerifyUserTokenAsync(user, method.GetTokenProvider()!, TotpConstants.TokenGenerationPurpose.ProfileMfaChallenge, sanitizedCode);
        if (!verified) {
            if (_userManager.SupportsUserLockout) {
                await _userManager.AccessFailedAsync(user);
                if (await _userManager.IsLockedOutAsync(user)) {
                    _logger.LogWarning("Profile MFA challenge failed for user {UserId} using method {Method}. The account is locked out.", user.Id, method.Code);
                    return ProfileMfaChallengeVerifyResult.LockedOut;
                }
            }
            _logger.LogWarning("Profile MFA challenge failed for user {UserId} using method {Method}.", user.Id, method.Code);
            return ProfileMfaChallengeVerifyResult.Failed;
        }
        if (_userManager.SupportsUserLockout) {
            await _userManager.ResetAccessFailedCountAsync(user);
        }
        var identity = new ClaimsIdentity(ExtendedIdentityConstants.ProfileMfaChallengeScheme, JwtClaimTypes.Subject, JwtClaimTypes.Role);
        identity.AddClaim(new Claim(JwtClaimTypes.Subject, user.Id));
        identity.AddClaim(new Claim(JwtClaimTypes.SessionId, sessionId));
        identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, method.Code));
        var issuedUtc = _timeProvider.GetUtcNow();
        await HttpContext.SignInAsync(ExtendedIdentityConstants.ProfileMfaChallengeScheme, new ClaimsPrincipal(identity), new AuthenticationProperties {
            IssuedUtc = issuedUtc,
            ExpiresUtc = issuedUtc.Add(_options.ProfileMfaChallengeLifetime),
            IsPersistent = false,
            AllowRefresh = false
        });
        _logger.LogInformation("Profile MFA challenge succeeded for user {UserId} using method {Method}.", user.Id, method.Code);
        return ProfileMfaChallengeVerifyResult.Succeeded;
    }

    /// <inheritdoc />
    public virtual Task ClearAsync() => HttpContext.SignOutAsync(ExtendedIdentityConstants.ProfileMfaChallengeScheme);

    /// <summary>Determines whether the method can be used for the MFA challenge. Push (trusted device), FIDO2 and recovery codes are not supported.</summary>
    /// <param name="method">The authentication method.</param>
    protected virtual bool IsCodeBased(AuthenticationMethod method) =>
        method.Type is AuthenticationMethodType.PhoneNumber or AuthenticationMethodType.Email or AuthenticationMethodType.AuthenticatorApp &&
        !string.IsNullOrWhiteSpace(method.GetTokenProvider());

    private async Task<bool> IsAvailableForUserAsync(User user, AuthenticationMethod method) =>
        (await GetMethodsAsync(user)).Any(available => string.Equals(available.Code, method.Code, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> IsSatisfiedAsync(User user, string sessionId) {
        var result = await HttpContext.AuthenticateAsync(ExtendedIdentityConstants.ProfileMfaChallengeScheme);
        if (!result.Succeeded || result.Principal is null || result.Properties?.IssuedUtc is not DateTimeOffset issuedUtc) {
            return false;
        }
        var subject = result.Principal.FindFirstValue(JwtClaimTypes.Subject);
        var mfaChallengeSessionId = result.Principal.FindFirstValue(JwtClaimTypes.SessionId);
        return string.Equals(subject, user.Id, StringComparison.Ordinal) &&
               string.Equals(mfaChallengeSessionId, sessionId, StringComparison.Ordinal) &&
               issuedUtc.Add(_options.ProfileMfaChallengeLifetime) > _timeProvider.GetUtcNow();
    }
}

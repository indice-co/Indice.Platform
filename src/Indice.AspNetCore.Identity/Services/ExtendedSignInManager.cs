using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Authorization;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Provides the APIs for user sign in.
    /// </summary>
    /// <typeparam name="TUser">The type encapsulating a user.</typeparam>
    public class ExtendedSignInManager<TUser> : SignInManager<TUser> where TUser : User
    {
        private const string LoginProviderKey = "LoginProvider";
        private const string XsrfKey = "XsrfId";
        private readonly IAuthenticationSchemeProvider _authenticationSchemeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="SignInManager{TUser}" />
        /// </summary>
        /// <param name="userManager">An instance of <see cref="UserManager{TUser}"/> used to retrieve users from and persist users.</param>
        /// <param name="contextAccessor">The accessor used to access the <see cref="HttpContext"/>.</param>
        /// <param name="claimsFactory">The factory to use to create claims principals for a user.</param>
        /// <param name="optionsAccessor">The accessor used to access the <see cref="AppSetting"/>.</param>
        /// <param name="logger">The logger used to log messages, warnings and errors.</param>
        /// <param name="schemes">The scheme provider that is used enumerate the authentication schemes.</param>
        /// <param name="confirmation">The <see cref="IUserConfirmation{TUser}"/> used check whether a user account is confirmed.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        /// <param name="authenticationSchemeProvider">Responsible for managing what authenticationSchemes are supported.</param>
        public ExtendedSignInManager(
            UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptionsSnapshot<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<TUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<TUser> confirmation,
            IConfiguration configuration,
            IAuthenticationSchemeProvider authenticationSchemeProvider
        ) : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation) {
            RequirePostSignInConfirmedEmail = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.SignIn)}").GetValue<bool?>(nameof(RequirePostSignInConfirmedEmail)) == true ||
                                              configuration.GetSection(nameof(SignInOptions)).GetValue<bool?>(nameof(RequirePostSignInConfirmedEmail)) == true;
            RequirePostSignInConfirmedPhoneNumber = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.SignIn)}").GetValue<bool?>(nameof(RequirePostSignInConfirmedPhoneNumber)) == true ||
                                                    configuration.GetSection(nameof(SignInOptions)).GetValue<bool?>(nameof(RequirePostSignInConfirmedPhoneNumber)) == true;
            ExternalScheme = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.SignIn)}").GetValue<string>(nameof(ExternalScheme)) ?? IdentityConstants.ExternalScheme;
            _authenticationSchemeProvider = authenticationSchemeProvider ?? throw new ArgumentNullException(nameof(authenticationSchemeProvider));
        }

        /// <summary>
        /// Enables the feature post login email confirmation.
        /// </summary>
        public bool RequirePostSignInConfirmedEmail { get; }
        /// <summary>
        /// Enables the feature post login phone number confirmation.
        /// </summary>
        public bool RequirePostSignInConfirmedPhoneNumber { get; }
        /// <summary>
        /// The scheme used to identify external authentication cookies.
        /// </summary>
        public string ExternalScheme { get; }

        /// <summary>
        /// Gets the external login information for the current login, as an asynchronous operation.
        /// </summary>
        /// <param name="expectedXsrf">Flag indication whether a Cross Site Request Forgery token was expected in the current request.</param>
        /// <returns>The task object representing the asynchronous operation containing the <see name="ExternalLoginInfo"/> for the sign-in attempt.</returns>
        public override async Task<ExternalLoginInfo> GetExternalLoginInfoAsync(string expectedXsrf = null) {
            var auth = await Context.AuthenticateAsync(ExternalScheme);
            var items = auth?.Properties?.Items;
            if (auth?.Principal == null || items == null || !items.ContainsKey(LoginProviderKey)) {
                return null;
            }
            if (expectedXsrf != null) {
                if (!items.ContainsKey(XsrfKey)) {
                    return null;
                }
                var userId = items[XsrfKey];
                if (userId != expectedXsrf) {
                    return null;
                }
            }
            var providerKey = auth.Principal.FindFirstValue(Options.ClaimsIdentity.UserIdClaimType);
            if (providerKey == null || !(items[LoginProviderKey] is string provider)) {
                return null;
            }
            var providerDisplayName = (await GetExternalAuthenticationSchemesAsync()).FirstOrDefault(p => p.Name == provider)?.DisplayName ?? provider;
            return new ExternalLoginInfo(auth.Principal, provider, providerKey, providerDisplayName) {
                AuthenticationTokens = auth.Properties.GetTokens()
            };
        }

        /// <summary>
        /// Signs in the specified user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="authenticationProperties"></param>
        /// <param name="authenticationMethod"></param>
        /// <returns></returns>
        public override async Task SignInAsync(TUser user, AuthenticationProperties authenticationProperties, string authenticationMethod = null) {
            await base.SignInAsync(user, authenticationProperties, authenticationMethod);
            if (user is User) {
                user.LastSignInDate = DateTimeOffset.UtcNow;
                await UserManager.UpdateAsync(user);
            }
        }

        /// <summary>
        /// Returns a flag indicating whether the specified user can sign in.
        /// </summary>
        /// <param name="user">The user whose sign-in status should be returned.</param>
        /// <returns>The task object representing the asynchronous operation, containing a flag that is true if the specified user can sign-in, otherwise false.</returns>
        public override async Task<bool> CanSignInAsync(TUser user) {
            if (user is User && user.Blocked) {
                Logger.LogWarning(0, "User {userId} cannot sign in. User is blocked by the administrator.", await UserManager.GetUserIdAsync(user));
                return false;
            }
            return await base.CanSignInAsync(user);
        }

        /// <summary>
        /// Signs in the specified <paramref name="user"/> if <paramref name="bypassTwoFactor"/> is set to false.
        /// Otherwise stores the <paramref name="user"/> for use after a two factor check.
        /// </summary>
        /// <param name="user">The user to sign in.</param>
        /// <param name="isPersistent">Flag indicating whether the sign-in cookie should persist after the browser is closed.</param>
        /// <param name="loginProvider">The login provider to use. Default is null</param>
        /// <param name="bypassTwoFactor">Flag indicating whether to bypass two factor authentication. Default is false.</param>
        /// <returns>Returns a <see cref="SignInResult"/>.</returns>
        protected override async Task<SignInResult> SignInOrTwoFactorAsync(TUser user, bool isPersistent, string loginProvider = null, bool bypassTwoFactor = false) {
            var isEmailConfirmed = await UserManager.IsEmailConfirmedAsync(user);
            var isPhoneConfirmed = await UserManager.IsPhoneNumberConfirmedAsync(user);
            var userClaims = await UserManager.GetClaimsAsync(user);
            var firstName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value; 
            var lastName = userClaims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
            var isPasswordExpired = user.HasExpiredPassword();
            var doPartialSignIn = (!isEmailConfirmed && RequirePostSignInConfirmedEmail)
                               || (!isPhoneConfirmed && RequirePostSignInConfirmedPhoneNumber)
                               || isPasswordExpired;
            if (doPartialSignIn) {
                // Store the userId for use after two factor check.
                var userId = await UserManager.GetUserIdAsync(user);
                var returnUrl = Context.Request.Query["ReturnUrl"];
                await Context.SignInAsync(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, StoreValidationInfo(userId, isEmailConfirmed, isPhoneConfirmed, isPasswordExpired, firstName, lastName), new AuthenticationProperties {
                    RedirectUri = returnUrl,
                    IsPersistent = isPersistent
                });
                var requiresEmailValidation = !isEmailConfirmed && RequirePostSignInConfirmedEmail;
                var requiresPhoneNumberValidation = !isPhoneConfirmed && RequirePostSignInConfirmedPhoneNumber;
                var requiresPasswordChange = isPasswordExpired;
                return new ExtendedSigninResult(requiresEmailValidation, requiresPhoneNumberValidation, requiresPasswordChange);
            }
            var signInResult = await base.SignInOrTwoFactorAsync(user, isPersistent, loginProvider, bypassTwoFactor);
            if (signInResult.Succeeded && (user is User)) {
                try {
                    user.LastSignInDate = DateTimeOffset.UtcNow;
                    await UserManager.UpdateAsync(user);
                } catch {; }
            }
            return signInResult;
        }

        /// <summary>
        /// Signs the current user out of the application.
        /// </summary>
        public async override Task SignOutAsync() {
            var schemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
            // Check if authentication scheme is registered before trying to signout out, to avoid errors.
            if (schemes.Any(x => x.Name == ExtendedIdentityConstants.ExtendedValidationUserIdScheme)) {
                await Context.SignOutAsync(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
            }
            if (schemes.Any(x => x.Name == ExternalScheme)) {
                await Context.SignOutAsync(ExternalScheme);
            }
            await base.SignOutAsync();
        }

        /// <summary>
        /// Creates a claims principal for the specified 2fa information.
        /// </summary>
        /// <param name="userId">The user whose is logging in via 2fa.</param>
        /// <param name="loginProvider">The 2fa provider.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing the user 2fa information.</returns>
        internal ClaimsPrincipal StoreTwoFactorInfo(string userId, string loginProvider) {
            var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, userId));
            if (loginProvider != null) {
                identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
            }
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Creates a claims principal for the specified validation information.
        /// </summary>
        /// <param name="userId">The user whose is logging in.</param>
        /// <param name="isEmailConfirmed">Flag indicating whether the user has confirmed his email address.</param>
        /// <param name="isPhoneConfirmed">Flag indicating whether the user has confirmed his phone number.</param>
        /// <param name="isPasswordExpired">Flag indicating whether the user has an expired password.</param>
        /// <param name="firstName">User's first name.</param>
        /// <param name="lastName">User's last name.</param>
        /// <returns>A <see cref="ClaimsPrincipal"/> containing the user 2fa information.</returns>
        internal ClaimsPrincipal StoreValidationInfo(string userId, bool isEmailConfirmed, bool isPhoneConfirmed, bool isPasswordExpired, string firstName, string lastName) {
            var identity = new ClaimsIdentity(ExtendedIdentityConstants.ExtendedValidationUserIdScheme);
            identity.AddClaim(new Claim(JwtClaimTypes.Subject, userId));
            identity.AddClaim(new Claim(JwtClaimTypes.EmailVerified, isEmailConfirmed.ToString().ToLower()));
            identity.AddClaim(new Claim(JwtClaimTypes.PhoneNumberVerified, isPhoneConfirmed.ToString().ToLower()));
            identity.AddClaim(new Claim(ExtendedIdentityConstants.PasswordExpiredClaimType, isPasswordExpired.ToString().ToLower()));
            if (!string.IsNullOrWhiteSpace(firstName)) {
                identity.AddClaim(new Claim(JwtClaimTypes.GivenName, firstName));
            }
            if (!string.IsNullOrWhiteSpace(lastName)) {
                identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, lastName));
            }
            return new ClaimsPrincipal(identity);
        }
    }

    /// <summary>
    /// Extends the <see cref="SignInResult"/> type.
    /// </summary>
    public class ExtendedSigninResult : SignInResult
    {
        /// <summary>
        /// Construct an instance of <see cref="ExtendedSigninResult"/>.
        /// </summary>
        public ExtendedSigninResult(bool requiresEmailValidation, bool requiresPhoneNumberValidation, bool requiresPasswordChange) {
            RequiresEmailValidation = requiresEmailValidation;
            RequiresPhoneNumberValidation = requiresPhoneNumberValidation;
            RequiresPasswordChange = requiresPasswordChange;
        }

        /// <summary>
        /// Returns a flag indication whether the user attempting to sign-in requires phone number confirmation.
        /// </summary>
        /// <value>True if the user attempting to sign-in requires phone number confirmation, otherwise false.</value>
        public bool RequiresPhoneNumberValidation { get; }
        /// <summary>
        /// Returns a flag indication whether the user attempting to sign-in requires email confirmation.
        /// </summary>
        /// <value>True if the user attempting to sign-in requires email confirmation, otherwise false.</value>
        public bool RequiresEmailValidation { get; }
        /// <summary>
        /// Returns a flag indication whether the user attempting to sign-in requires an immediate password change due to expiration.
        /// </summary>
        /// <value>True if the user attempting to sign-in requires a password change, otherwise false.</value>
        public bool RequiresPasswordChange { get; }
    }

    /// <summary>
    /// Extensions on <see cref="SignInResult"/> type.
    /// </summary>
    public static class ExtendedSignInManagerExtensions
    {
        /// <summary>
        ///  Returns a flag indication whether the user attempting to sign-in requires phone number confirmation.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool RequiresPhoneNumberConfirmation(this SignInResult result) => (result as ExtendedSigninResult)?.RequiresPhoneNumberValidation == true;
        /// <summary>
        /// Returns a flag indication whether the user attempting to sign-in requires email confirmation .
        /// </summary>
        public static bool RequiresEmailConfirmation(this SignInResult result) => (result as ExtendedSigninResult)?.RequiresEmailValidation == true;
        /// <summary>
        /// Returns a flag indication whether the user attempting to sign-in requires email confirmation .
        /// </summary>
        public static bool RequiresPasswordChange(this SignInResult result) => (result as ExtendedSigninResult)?.RequiresPasswordChange == true;
    }
}

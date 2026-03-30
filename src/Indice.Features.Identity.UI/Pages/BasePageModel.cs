#if NET9_0_OR_GREATER
using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Stores;
#else
using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
#endif
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Base model class for pages containing some common utility methods.</summary>
public abstract class BasePageModel : PageModel
{
    private IdentityUIOptions? _uiOptions;
    private RequestCulture? _requestCulture;
    private IIdentityServerInteractionService? _interactionService;
    private IUserRequirementProvider<User>? _userActivityProvider;

    /// <summary>Will propagate to body class</summary>
    [ViewData]
    public string BodyCssClass { get; set; } = "identity-page";
    /// <summary>Defines a mechanism for retrieving a service object.</summary>
    protected IServiceProvider ServiceProvider => HttpContext.RequestServices;
    /// <summary>UI Options</summary>
    public IdentityUIOptions UiOptions => _uiOptions ??= ServiceProvider.GetRequiredService<IOptions<IdentityUIOptions>>().Value;
    /// <summary>Request Culture</summary>
    public RequestCulture RequestCulture => _requestCulture ??= Request.HttpContext.Features.Get<IRequestCultureFeature>()!.RequestCulture;
    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    public IIdentityServerInteractionService InteractionService => _interactionService ??= ServiceProvider.GetRequiredService<IIdentityServerInteractionService>();
    /// <summary>The <see cref="IUserRequirementProvider{User}"/> used to retrieve the next validation activity according to the user state.</summary>
    public IUserRequirementProvider<User> UserActivityProvider => _userActivityProvider ??= ServiceProvider.GetRequiredService<IUserRequirementProvider<User>>();

    /// <summary>Checks if the given return URL is safe for redirection.</summary>
    /// <param name="returnUrl">The URL to validate.</param>
    public bool IsValidReturnUrl(string? returnUrl) {
        if (string.IsNullOrWhiteSpace(returnUrl)) {
            return false;
        }
        return InteractionService.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl) || UiOptions.IsValidReturnUrl(returnUrl);
    }

    /// <summary>Adds errors contained in <see cref="IdentityResult"/> to the <see cref="ModelStateDictionary"/>.</summary>
    /// <param name="result">Represents the result of a sign-in operation.</param>
    public virtual void AddModelErrors(IdentityResult result) {
        if (result.Succeeded) {
            return;
        }
        if (result.Errors?.Count() > 0) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }

    /// <summary>Generates a registration email confirmation link and sends it to the email of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task SendRegistrationEmail(User user, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        if (!string.IsNullOrEmpty(returnUrl) && InteractionService.IsValidReturnUrl(returnUrl)) {
            // if this is a login url use it to extract the client_id param but remove it from the email link
            // If a return url such as a login url is baked into the confirmation chanses are that it will
            // 1. either become invild by the time the user clicks his email
            // 2. it will make the confirmation link so big it will fail on multiple browsers.
            // so remove the thing.
            returnUrl = null;
        }
        var callbackUrl = Url.PageLink("/ConfirmEmail", values: new { userId = user.Id, token, returnUrl, client_id = HttpContext.GetClientIdFromReturnUrl() }, protocol: HttpContext.Request.Scheme ?? null);
        var emailService = ServiceProvider.GetRequiredService<IEmailService>();
        var identityMessageDescriber = ServiceProvider.GetRequiredService<IdentityMessageDescriber>();
        await emailService.SendAsync(message =>
            message.To(user.Email!)
                   .WithSubject(identityMessageDescriber.RegisterEmailSubject(configuration.GetApplicationName()!))
                   .UsingTemplate("EmailRegister")
                   .WithData(new {
                       user.UserName,
                       Subject = identityMessageDescriber.RegisterEmailSubject(configuration.GetApplicationName()!),
                       Url = callbackUrl
                   })
        );
        var logger = ServiceProvider.GetRequiredService<ILogger<BasePageModel>>();
        var maskedEmail = user.Email!.Substring(0, 2) + "****" + user.Email.Substring(user.Email.IndexOf('@'));
        logger.LogInformation("Sending a welcome email to {Email} with callback URL: {CallbackUrl}.", maskedEmail, callbackUrl);
    }

    /// <summary>Generates a registration email confirmation link and sends it to the email of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task SendConfirmationEmail(User user, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        if (!string.IsNullOrEmpty(returnUrl) && InteractionService.IsValidReturnUrl(returnUrl)) {
            // if this is a login url use it to extract the client_id param but remove it from the email link
            // If a return url such as a login url is baked into the confirmation chanses are that it will
            // 1. either become invild by the time the user clicks his email
            // 2. it will make the confirmation link so big it will fail on multiple browsers.
            // so remove the thing.
            returnUrl = null;
        }
        var callbackUrl = Url.PageLink("/ConfirmEmail", values: new { userId = user.Id, token, returnUrl, client_id = HttpContext.GetClientIdFromReturnUrl() }, protocol: HttpContext.Request.Scheme ?? null);
        var emailService = ServiceProvider.GetRequiredService<IEmailService>();
        var identityMessageDescriber = ServiceProvider.GetRequiredService<IdentityMessageDescriber>();
        await emailService.SendAsync(message =>
            message.To(user.Email!)
                   .WithSubject(identityMessageDescriber.ConfirmationEmailSubject)
                   .UsingTemplate("EmailConfirmYourEmail")
                   .WithData(new {
                       user.UserName,
                       Url = callbackUrl
                   })
        );
        var logger = ServiceProvider.GetRequiredService<ILogger<BasePageModel>>();
        var maskedEmail = user.Email!.Substring(0, 2) + "****" + user.Email.Substring(user.Email.IndexOf('@'));
        logger.LogInformation("Sending a confirmation email to {Email} with callback URL: {CallbackUrl}.", maskedEmail, callbackUrl);
    }

    /// <summary>Generates a change email confirmation link and sends it to the email of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="newEmail">The new email of the user.</param>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task SendChangeEmailConfirmationEmail(User user, string newEmail, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        if (!string.IsNullOrEmpty(returnUrl) && InteractionService.IsValidReturnUrl(returnUrl)) {
            // if this is a login url use it to extract the client_id param but remove it from the email link
            // If a return url such as a login url is baked into the confirmation chanses are that it will
            // 1. either become invild by the time the user clicks his email
            // 2. it will make the confirmation link so big it will fail on multiple browsers.
            // so remove the thing.
            returnUrl = null;
        }
        var callbackUrl = Url.PageLink("/ConfirmEmailChange", values: new { userId = user.Id, token, email = newEmail, returnUrl, client_id = HttpContext.GetClientIdFromReturnUrl() });
        var claims = await userManager.GetClaimsAsync(user);
        var emailService = ServiceProvider.GetRequiredService<IEmailService>();
        var identityMessageDescriber = ServiceProvider.GetRequiredService<IdentityMessageDescriber>();
        await emailService.SendAsync(message =>
            message.To(newEmail!)
                   .WithSubject(identityMessageDescriber.ConfirmationEmailChangeSubject)
                   .UsingTemplate("EmailConfirmEmailChange")
                   .WithData(new {
                       UserName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value ?? user.UserName,
                       NewEmail = newEmail,
                       Url = callbackUrl
                   })
        );
    }

    /// <summary>Generates a TOTP code and sends it to the phone number of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="phoneNumber">The phone number.</param>
    public virtual async Task SendVerificationSmsAsync(User user, string phoneNumber) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var code = await userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
        var smsService = ServiceProvider.GetRequiredService<ISmsService>();
        var identityMessageDescriber = ServiceProvider.GetRequiredService<IdentityMessageDescriber>();
        await smsService.SendAsync(phoneNumber, identityMessageDescriber.PhoneVerificationSmsSubject, identityMessageDescriber.PhoneVerificationSmsBody(code));
    }

    /// <summary>Generates a TOTP code and sends it to the email address of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    public virtual async Task SendVerificationEmailAsync(User user) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        var emailService = ServiceProvider.GetRequiredService<IEmailService>();
        await emailService.SendAsync(message => {
            message
                .To(user.Email!)
                .UsingTemplate("EmailMfaOnboarding")
                .WithSubject(userManager.MessageDescriber.UpdateEmailMessageSubject)
                .WithData(new {
                    Username = user.UserName,
                    Code = code
                });
        });
    }

    /// <summary>
    /// Attempts to complete the login process and returns an appropriate action result based on the sign-in outcome and
    /// authentication context. 
    /// </summary>
    /// <remarks>If two-factor authentication is required, the user is redirected to the multi-factor
    /// authentication page. If email validation is required, the user is redirected to the email addition page. For
    /// native clients in an OpenID Connect context, a loading page is returned to improve user experience.</remarks>
    /// <param name="signInResult">The result of the sign-in attempt, indicating the status of the user's authentication and any required
    /// additional steps.</param>
    /// <param name="user">The logged in user</param>
    /// <param name="returnUrl">The URL to redirect the user to after a successful login or required authentication step. Must not be null or
    /// empty.</param>
    /// <returns>An <see cref="IActionResult"/> that redirects the user to the next step in the authentication flow, such as
    /// multi-factor authentication, email validation, or the specified return URL.</returns>
    protected async Task<IActionResult> TryLogin(Microsoft.AspNetCore.Identity.SignInResult signInResult, User user, string returnUrl) {
        if (string.IsNullOrEmpty(returnUrl)) {
            returnUrl = "/";
        }
        if (signInResult.RequiresTwoFactor) {
            var redirectUrl = Url.PageLink("/Mfa", values: new { returnUrl });
            return Redirect(redirectUrl!);
        }

        if (signInResult.RequiresValidation()) {
            var userStateProvider = HttpContext.RequestServices.GetRequiredService<IUserRequirementProvider<User>>();
            var requirement = await userStateProvider.GetNextAsync(HttpContext, user);
            var redirectUrl = GetRedirectUrl(requirement, returnUrl);
            return Redirect(redirectUrl!);
        }
        // Check if external login is in the context of an OIDC request.
        var context = await InteractionService.GetAuthorizationContextAsync(returnUrl);
        if (context is not null) {
            if (context.IsNativeClient()) {
                // The client is native, so this change in how to return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }
            // We can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null.
            return Redirect(returnUrl);
        }
        return IsValidReturnUrl(returnUrl) ? Redirect(returnUrl) : Redirect("/");
    }

    /// <summary>Redirects the user to the error page with the specified error details.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="error">The error code.</param>
    /// <param name="errorDescription">The error description.</param>
    /// <param name="authorizationRequest">The authorization request.</param>
    [NonAction]
    protected async Task<IActionResult> RedirectToErrorPageAsync(HttpContext context, string error, string? errorDescription, AuthorizationRequest? authorizationRequest = null) {

        var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
        var errorMessageStore = context.RequestServices.GetRequiredService<IMessageStore<ErrorMessage>>();

        var errorModel = new ErrorMessage() {
#if NET9_0_OR_GREATER
            ActivityId = System.Diagnostics.Activity.Current?.Id,
#endif
            RequestId = context.TraceIdentifier,
            Error = error,
            ErrorDescription = errorDescription,
            UiLocales = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
            //DisplayMode = response.Request?.DisplayMode,
            ClientId = authorizationRequest?.Client.ClientId
        };


        var message = new Message<ErrorMessage>(errorModel, DateTime.UtcNow);
        var id = await errorMessageStore.WriteAsync(message);

        string errorUrl = options.UserInteraction.ErrorUrl ?? "/error";
        var url = errorUrl + "?" + options.UserInteraction.ErrorIdParameter + "=" + UrlEncoder.Default.Encode(id);

        return Redirect(url);
    }


    /// <summary>Gets the page to redirect based on the <see cref="UserValidationRequirement"/>.</summary>
    /// <param name="requirement">The current user validation requirement.</param>
    /// <param name="returnUrl">The return URL.</param>
    protected string? GetRedirectUrl(UserValidationRequirement requirement, string? returnUrl = null) => requirement.Kind switch {
        UserActivityRequirementKind.None => IsValidReturnUrl(returnUrl) ? returnUrl : "/",
        _ => Url.PageLink(requirement.PageName, values: new { returnUrl })
    };
}

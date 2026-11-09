#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Services;
#endif
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.UI.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA login screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
[IdentityUI(typeof(MfaModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseMfaModel"/> class.</summary>
    /// <param name="logger">The logger instance for this page.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="totpServiceFactory">A factory service that contains methods to create various TOTP services, based on <see cref="TotpServiceBase"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="authenticationMethodProvider">Abstracts interaction with system's various authentication methods.</param>
    /// <param name="totpOptions">Options for configuring Time-based One-Time Password (TOTP) settings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaModel(
        ILogger<BaseMfaModel> logger,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider,
        IOptions<TotpOptions> totpOptions
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        TotpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        AuthenticationMethodProvider = authenticationMethodProvider ?? throw new ArgumentNullException(nameof(authenticationMethodProvider));
        AuthenticatorDigits = totpOptions?.Value.CodeLength ?? AuthenticatorDigits;
    }

    /// <summary>The logger instance for this page.</summary>
    public ILogger<BaseMfaModel> Logger { get; }
    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>A factory service that contains methods to create various TOTP services, based on <see cref="TotpServiceBase"/>.</summary>
    protected TotpServiceFactory TotpServiceFactory { get; }
    /// <summary>Represents a set of key/value application configuration properties.</summary>
    protected IConfiguration Configuration { get; }
    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }
    /// <summary>Abstracts interaction with system's various authentication methods.</summary>
    protected IAuthenticationMethodProvider AuthenticationMethodProvider { get; }

    /// <summary>Number of digits for the authenticator code.</summary>
    protected readonly int AuthenticatorDigits = 6;
    /// <summary>Login view model.</summary>
    public MfaLoginViewModel View { get; set; } = new MfaLoginViewModel();

    /// <summary>The input model that backs the add email page.</summary>
    [BindProperty]
    public MfaLoginInputModel Input { get; set; } = new MfaLoginInputModel();

    /// <summary>MFA page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        Input = View = await BuildMfaLoginViewModelAsync(returnUrl);
        if (View.HasError) {
            ModelState.AddModelError(string.Empty, View.Error!);
            return Page();
        }

        await SendOtpAsync();
        return Page();
    }

    /// <summary>MFA page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        View = await BuildMfaLoginViewModelAsync(Input);
        if (View.HasError) {
            ModelState.AddModelError(string.Empty, View.Error!);
            return Page();
        }
        if (Input.ResendOtp) {
            var otpResult = await SendOtpAsync();
            switch (otpResult) {
                case { Success: false, IsRateLimited: true }:
                    ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.MfaTokenNotExpired);
                    break;
                case { Success: false }:
                    ModelState.AddModelError(string.Empty, otpResult.Error!);
                    break;
                default:
                    break;
            }
            return Page();
        }
        var rememberMfaClient = View.IsExistingBrowser || Input.RememberClient;
        var signInResult = await SignInManager.TwoFactorSignInAsync(View.AuthenticationMethod?.GetTokenProvider()!, Input.OtpCode!, Input.RememberMe, rememberMfaClient);
        if (signInResult.Succeeded) {
            if (string.IsNullOrEmpty(Input.ReturnUrl)) {
                return Redirect("/");
            } else if (IsValidReturnUrl(Input.ReturnUrl)) {
                return Redirect(Input.ReturnUrl);
            } else {
                Logger.LogError("Invalid return URL while federating to external provider.");
                return await RedirectToErrorPageAsync(HttpContext, "Invalid return URL.", "Invalid return URL while federating to external provider");
            }
        }
        if (signInResult.RequiresValidation()) {
            return RedirectToPage("/AddEmail", new { returnUrl });

        }
        ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.MfaValidationError);
        return Page();
    }

    /// <summary>MFA page POST handler for recovery code authentication.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnPostRecoveryCodeAsync([FromQuery] string? returnUrl) {
        View = await BuildMfaLoginViewModelAsync(Input);
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) {
            throw new InvalidOperationException("Unable to load two-factor authentication user.");
        }
        var recoveryCode = Input.RecoveryCode?.Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(recoveryCode)) {
            ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.MfaValidationError);
            return Page();
        }
        var result = await SignInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        if (result.Succeeded) {
            Logger.LogInformation("User logged in with a recovery code.");
            if (string.IsNullOrEmpty(returnUrl)) {
                return Redirect("/");
            } else if (IsValidReturnUrl(returnUrl)) {
                return Redirect(returnUrl);
            } else {
                Logger.LogError("Invalid return URL while signing in with recovery code.");
                return await RedirectToErrorPageAsync(HttpContext, "Invalid return URL.", "Invalid return URL while signing in with recovery code.");
            }
        }
        if (result.IsLockedOut) {
            Logger.LogWarning("User account locked out.");
            return RedirectToPage("/Lockout");
        }
        Logger.LogWarning("Invalid recovery code entered.");
        ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.MfaInvalidRecoveryCode);
        return Page();
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(MfaLoginInputModel model) {
        var viewModel = await BuildMfaLoginViewModelAsync(model.ReturnUrl, model.SelectedAuthenticationMethodCode);
        viewModel.SelectedAuthenticationMethodCode = model.SelectedAuthenticationMethodCode;
        viewModel.OtpCode = null;
        viewModel.RememberClient = model.RememberClient;
        viewModel.RememberMe = model.RememberMe;
        return viewModel;
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string? returnUrl, string? selectedMethodCode = null) {
        var user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("User cannot be null");
        var authenticationMethod = await AuthenticationMethodProvider.FindMethodForUserOrDefaultAsync(user, selectedMethodCode);
        var deviceIdentifier = await SignInManager.GetMfaDeviceIdentifierAsync(user);
        UserDevice? browserDevice = null;
        if (!string.IsNullOrWhiteSpace(deviceIdentifier.Value)) {
            browserDevice = await UserManager.GetDeviceByIdAsync(user, deviceIdentifier.Value);
        }
        if (authenticationMethod is null) {
            Logger.LogError("MFA must be applied but no suitable authentication method was found.");
        }
        var hasError = authenticationMethod == null;
        return new MfaLoginViewModel {
            AuthenticationMethod = authenticationMethod,
            AvailableAuthenticationMethods = await AuthenticationMethodProvider.GetAllMethodsForUserAsync(user),
            AllowDowngradeAuthenticationMethod = AuthenticationMethodProvider.AllowMfaChannelDowngrade,
            ReturnUrl = returnUrl,
            User = user,
            IsExistingBrowser = browserDevice?.MfaSessionActive() ?? false,
            Error = hasError ? "MFA is enabled but there is no active two factor authentication method configured. Please contact your administrator." : null,
            ResendEnabled = !hasError &&
                authenticationMethod!.Type != AuthenticationMethodType.AuthenticatorApp &&
                (authenticationMethod.GetDeliveryChannel() == TotpDeliveryChannel.Sms ||
                 authenticationMethod.GetDeliveryChannel() == TotpDeliveryChannel.PushNotification ||
                 authenticationMethod.GetDeliveryChannel() == TotpDeliveryChannel.Email),
            HubConnectionUrl = Configuration.GetSection("General").GetValue<string>("HubConnectionUrl"),
            AuthenticatorDigits = AuthenticatorDigits
        };
    }

    private async Task<TotpResult> SendOtpAsync() {
        if (View.AuthenticationMethod is null) {
            return TotpResult.ErrorResult("MFA is enabled but there is no active two factor authentication method configured. Please contact your administrator.");
        }
        var totpService = TotpServiceFactory.Create<User>();
        if (View.AuthenticationMethod.SupportsDeliveryChannel()) {
            if (View.AuthenticationMethodDeliveryChannel == TotpDeliveryChannel.Email) {
                return await totpService.SendAsync(message =>
                message.ToUser(View.User)
                       .WithMessage(UserManager.MessageDescriber.MfaEmailBody)
                       .UsingEmail("EmailMfaOtpCode")
                       .UsingTokenProvider(View.AuthenticationMethod?.GetTokenProvider()!)
                       .WithSubject(UserManager.MessageDescriber.MfaEmailSubject)
                       .WithPurpose("TwoFactor"));
            }
            return await totpService.SendAsync(message =>
                message.ToUser(View.User)
                   .WithMessage(UserManager.MessageDescriber.MfaSmsBody)
                   .UsingDeliveryChannel(View.AuthenticationMethodDeliveryChannel!.Value)
                   .UsingTokenProvider(View.AuthenticationMethod?.GetTokenProvider()!)
                   .WithSubject(UserManager.MessageDescriber.MfaSmsSubject)
                   .WithPurpose("TwoFactor"));
        }
        return TotpResult.SuccessResult;
    }
}

internal class MfaModel : BaseMfaModel
{
    public MfaModel(
        ILogger<BaseMfaModel> logger,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider,
        IOptions<TotpOptions> totpOptions
    ) : base(logger, userManager, signInManager, totpServiceFactory, configuration, interaction, authenticationMethodProvider, totpOptions) { }
}

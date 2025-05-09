using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.UI.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA login screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
[IdentityUI(typeof(MfaModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseMfaModel : BasePageModel
{
    private readonly IStringLocalizer<BaseMfaModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseMfaModel"/> class.</summary>
    /// <param name="logger">The logger instance for this page.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseMfaModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="totpServiceFactory">A factory service that contains methods to create various TOTP services, based on <see cref="TotpServiceBase"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="authenticationMethodProvider">Abstracts interaction with system's various authentication methods.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaModel(
        ILogger<BaseMfaModel> logger,
        IStringLocalizer<BaseMfaModel> localizer,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        TotpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        AuthenticationMethodProvider = authenticationMethodProvider ?? throw new ArgumentNullException(nameof(authenticationMethodProvider));
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

    /// <summary>Login view model.</summary>
    public MfaLoginViewModel View { get; set; } = new MfaLoginViewModel();

    /// <summary>The input model that backs the add email page.</summary>
    [BindProperty]
    public MfaLoginInputModel Input { get; set; } = new MfaLoginInputModel();

    /// <summary>MFA page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    /// <param name="downgradeChannel">Allows the user to select a channel with lower security.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl, [FromQuery(Name = "dc")] bool? downgradeChannel) {
        Input = View = await BuildMfaLoginViewModelAsync(returnUrl, downgradeChannel);
        if (View.HasError) {
            ModelState.AddModelError(string.Empty, _localizer[View.Error!]);
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
            ModelState.AddModelError(string.Empty, _localizer[View.Error!]);
            return Page();
        }
        if (Input.ResendOtp) {
            var otpResult = await SendOtpAsync();
            if(!otpResult.Success) {
                ModelState.AddModelError(string.Empty, otpResult.Error!);
            }
            return Page();
        }
        var signInResult = await SignInManager.TwoFactorSignInAsync(View.AuthenticationMethod?.GetTokenProvider()!, Input.OtpCode!, Input.RememberMe, Input.RememberClient);
        if (signInResult.Succeeded) {
            if (string.IsNullOrEmpty(Input.ReturnUrl)) {
                return Redirect("/");
            } else if (IsValidReturnUrl(Input.ReturnUrl)) {
                return Redirect(Input.ReturnUrl);
            } else {
                throw new Exception("Invalid return URL.");
            }
        }
        if (signInResult.RequiresValidation()) { 
            return RedirectToPage("/AddEmail", new { returnUrl });
            
        }
        ModelState.AddModelError(string.Empty, _localizer["The OTP code is not valid."]);
        return Page();
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(MfaLoginInputModel model) {
        var viewModel = await BuildMfaLoginViewModelAsync(model.ReturnUrl);
        viewModel.OtpCode = null;
        viewModel.RememberClient = model.RememberClient;
        viewModel.RememberMe = model.RememberMe;
        return viewModel;
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string? returnUrl, bool? tryDowngradeAuthenticationMethod = false) {
        var user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("User cannot be null");
        var authenticationMethod = await AuthenticationMethodProvider.GetRequiredAuthenticationMethod(user, tryDowngradeAuthenticationMethod);
        var allowDowngradeAuthenticationMethod = Configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "AllowDowngradeAuthenticationMethod") ?? false;
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
            AllowDowngradeAuthenticationMethod = allowDowngradeAuthenticationMethod,
            ReturnUrl = returnUrl,
            User = user,
            IsExistingBrowser = browserDevice?.MfaSessionActive() ?? false,
            Error = hasError ? "MFA is enabled but there is no active two factor authentication method configured. Please contact your administrator." : null,
            ResendEnabled = !hasError && (authenticationMethod?.GetDeliveryChannel() == TotpDeliveryChannel.Sms || authenticationMethod?.GetDeliveryChannel() == TotpDeliveryChannel.PushNotification),
            HubConnectionUrl = Configuration.GetSection("General").GetValue<string>("HubConnectionUrl")
        };
    }

    private async Task<TotpResult> SendOtpAsync() {
        var totpService = TotpServiceFactory.Create<User>();
        if (View.AuthenticationMethodDeliveryChannel == TotpDeliveryChannel.Sms || View.AuthenticationMethodDeliveryChannel == TotpDeliveryChannel.PushNotification) {
            return await totpService.SendAsync(message =>
                message.ToUser(View.User)
                       .WithMessage(_localizer["Your OTP code for login is: {0}"])
                       .UsingDeliveryChannel(View.AuthenticationMethodDeliveryChannel.Value)
                       .UsingTokenProvider(View.AuthenticationMethod?.GetTokenProvider()!)
                       .WithSubject(_localizer["OTP login"])
                       .WithPurpose("TwoFactor")
            );
        }
        return TotpResult.SuccessResult;
    }
}

internal class MfaModel : BaseMfaModel
{
    public MfaModel(
        ILogger<BaseMfaModel> logger,
        IStringLocalizer<MfaModel> localizer,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider
    ) : base(logger, localizer, userManager, signInManager, totpServiceFactory, configuration, interaction, authenticationMethodProvider) { }
}

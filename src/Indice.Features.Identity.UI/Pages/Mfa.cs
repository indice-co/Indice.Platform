#if NET9_0_OR_GREATER
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Services;
#endif
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.UI.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaModel(
        ILogger<BaseMfaModel> logger,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider
    ) {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (!otpResult.Success) {
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
        ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.MfaValidationError);
        return Page();
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(MfaLoginInputModel model) {
        var viewModel = await BuildMfaLoginViewModelAsync(model.ReturnUrl, model.SelectedDeliveryChannel);
        viewModel.SelectedDeliveryChannel = model.SelectedDeliveryChannel;
        viewModel.OtpCode = null;
        viewModel.RememberClient = model.RememberClient;
        viewModel.RememberMe = model.RememberMe;
        return viewModel;
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string? returnUrl, TotpDeliveryChannel? selectedTotpChannel = null) {
        var user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("User cannot be null");
        var authenticationMethod = await AuthenticationMethodProvider.FindMethodForUserOrDefaultAsync(user, selectedTotpChannel);
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
            ResendEnabled = !hasError && (authenticationMethod?.GetDeliveryChannel() == TotpDeliveryChannel.Sms || authenticationMethod?.GetDeliveryChannel() == TotpDeliveryChannel.PushNotification || authenticationMethod?.GetDeliveryChannel() == TotpDeliveryChannel.Email),
            HubConnectionUrl = Configuration.GetSection("General").GetValue<string>("HubConnectionUrl")
        };
    }

    private async Task<TotpResult> SendOtpAsync() {
        if (View.AuthenticationMethod is null) {
            return TotpResult.ErrorResult("MFA is enabled but there is no active two factor authentication method configured. Please contact your administrator.");
        }
        var totpService = TotpServiceFactory.Create<User>();
        if (View.AuthenticationMethod.SupportsDeliveryChannel()) {
            return await totpService.SendAsync(message =>
                message.ToUser(View.User)
                       .WithMessage(UserManager.MessageDescriber.MfaSmsBody)
                       .UsingDeliveryChannel(View.AuthenticationMethodDeliveryChannel!.Value)
                       .UsingTokenProvider(View.AuthenticationMethod?.GetTokenProvider()!)
                       .WithSubject(UserManager.MessageDescriber.MfaSmsSubject)
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
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider
    ) : base(logger, userManager, signInManager, totpServiceFactory, configuration, interaction, authenticationMethodProvider) { }
}

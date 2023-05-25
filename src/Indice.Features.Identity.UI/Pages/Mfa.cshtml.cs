using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Core.Totp;
using Indice.Features.Identity.UI.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the MFA login screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
[IdentityUI(typeof(MfaModel))]
[SecurityHeaders]
public abstract class BaseMfaModel : BasePageModel
{
    private readonly IStringLocalizer<BaseMfaModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseMfaModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseMfaModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="totpServiceFactory">A factory service that contains methods to create various TOTP services, based on <see cref="TotpServiceBase"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="authenticationMethodProvider">Abstracts interaction with system's various authentication methods.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaModel(
        IStringLocalizer<BaseMfaModel> localizer,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        TotpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        AuthenticationMethodProvider = authenticationMethodProvider ?? throw new ArgumentNullException(nameof(authenticationMethodProvider));
    }

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
        var totpService = TotpServiceFactory.Create<User>();
        if (View.DeliveryChannel == TotpDeliveryChannel.Sms) {
            await totpService.SendAsync(message =>
                message.ToUser(View.User)
                       .WithMessage(_localizer["Your OTP code for login is: {0}"])
                       .UsingSms()
                       .WithSubject(_localizer["OTP login"])
                       .WithPurpose("TwoFactor")
            );
        }
        return Page();
    }

    /// <summary>MFA page POST handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    [ValidateAntiForgeryToken]
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        var totpService = TotpServiceFactory.Create<User>();
        var signInResult = await SignInManager.TwoFactorSignInAsync(totpService.TokenProvider, Input.OtpCode, Input.RememberMe, Input.RememberClient);
        if (signInResult.Succeeded) {
            if (string.IsNullOrEmpty(Input.ReturnUrl)) {
                return Redirect("/");
            } else if (Interaction.IsValidReturnUrl(Input.ReturnUrl) || Url.IsLocalUrl(Input.ReturnUrl)) {
                return Redirect(Input.ReturnUrl);
            } else {
                throw new Exception("Invalid return URL.");
            }
        }
        var redirectUrl = GetRedirectUrl(signInResult, Input.ReturnUrl);
        if (!string.IsNullOrWhiteSpace(redirectUrl)) {
            return Redirect(redirectUrl);
        }
        ModelState.AddModelError(string.Empty, _localizer["The OTP code is not valid."]);
        View = await BuildMfaLoginViewModelAsync(Input);
        return Page();
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(MfaLoginInputModel model) {
        var viewModel = await BuildMfaLoginViewModelAsync(model.ReturnUrl);
        viewModel.OtpCode = null;
        viewModel.RememberClient = model.RememberClient;
        viewModel.RememberMe = model.RememberMe;
        return viewModel;
    }

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string? returnUrl, bool? tryDowngradeAuthenticationMethod = false, TotpDeliveryChannel? mfaChannel = null) {
        var user = await SignInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("User cannot be null");
        var authenticationMethod = await AuthenticationMethodProvider.GetRequiredAuthenticationMethod(user, tryDowngradeAuthenticationMethod) ?? throw new InvalidOperationException("MFA must be applied but no suitable authentication method was found.");
        var allowDowngradeAuthenticationMethod = Configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "AllowDowngradeAuthenticationMethod") ?? false;
        return new MfaLoginViewModel {
            AllowDowngradeAuthenticationMethod = allowDowngradeAuthenticationMethod,
            DeliveryChannel = authenticationMethod.SupportsDeliveryChannel() ? ((IAuthenticationMethodWithChannel)authenticationMethod).DeliveryChannel  : null,
            DeviceNames = authenticationMethod.SupportsDevices() ? ((IAuthenticationMethodWithDevices)authenticationMethod).Devices.Select(x => x.Name) : Array.Empty<string>(),
            ReturnUrl = returnUrl,
            User = user
        };
    }
}

internal class MfaModel : BaseMfaModel
{
    public MfaModel(
        IStringLocalizer<BaseMfaModel> localizer,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction,
        IAuthenticationMethodProvider authenticationMethodProvider
    ) : base(localizer, userManager, signInManager, totpServiceFactory, configuration, interaction, authenticationMethodProvider) { }
}

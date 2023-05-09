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
    private readonly ExtendedUserManager<User> _userManager;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly TotpServiceFactory _totpServiceFactory;
    private readonly IConfiguration _configuration;
    private readonly IIdentityServerInteractionService _interaction;

    /// <summary>Creates a new instance of <see cref="BaseMfaModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseMfaModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager"></param>
    /// <param name="totpServiceFactory">A factory service that contains methods to create various TOTP services, based on <see cref="TotpServiceBase"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseMfaModel(
        IStringLocalizer<BaseMfaModel> localizer,
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        TotpServiceFactory totpServiceFactory,
        IConfiguration configuration,
        IIdentityServerInteractionService interaction
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

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
        var totpService = _totpServiceFactory.Create<User>();
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
        var totpService = _totpServiceFactory.Create<User>();
        var signInResult = await _signInManager.TwoFactorSignInAsync(totpService.TokenProvider, Input.OtpCode, Input.RememberMe, Input.RememberClient);
        if (signInResult.Succeeded) {
            if (string.IsNullOrEmpty(Input.ReturnUrl)) {
                return Redirect("/");
            } else if (_interaction.IsValidReturnUrl(Input.ReturnUrl) || Url.IsLocalUrl(Input.ReturnUrl)) {
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

    private async Task<MfaLoginViewModel> BuildMfaLoginViewModelAsync(string? returnUrl, bool? downgradeMfaChannel = false, TotpDeliveryChannel? mfaChannel = null) {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync() ?? throw new InvalidOperationException("User cannot be null");
        var allowMfaChannelDowngrade = _configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "AllowChannelDowngrade") ?? false;
        if ((downgradeMfaChannel ??= false) && allowMfaChannelDowngrade) {
            return new MfaLoginViewModel {
                DeliveryChannel = mfaChannel ?? TotpDeliveryChannel.Sms,
                ReturnUrl = returnUrl,
                User = user,
                AllowMfaChannelDowngrade = true
            };
        }
        var trustedDevices = await _userManager.GetDevicesAsync(user, UserDeviceListFilter.TrustedNativeDevices());
        var deliveryChannel = TotpDeliveryChannel.None;
        if (trustedDevices.Count > 0) {
            deliveryChannel = TotpDeliveryChannel.PushNotification;
        } else {
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var phoneNumberConfirmed = !string.IsNullOrWhiteSpace(phoneNumber) && await _userManager.IsPhoneNumberConfirmedAsync(user);
            if (phoneNumberConfirmed) {
                deliveryChannel = mfaChannel ?? TotpDeliveryChannel.Sms;
            }
        }
        return new MfaLoginViewModel {
            AllowMfaChannelDowngrade = allowMfaChannelDowngrade,
            DeliveryChannel = deliveryChannel,
            DeviceNames = trustedDevices.Select(x => x.Name),
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
        IIdentityServerInteractionService interaction
    ) : base(localizer, userManager, signInManager, totpServiceFactory, configuration, interaction) { }
}

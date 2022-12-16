using System;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Indice.Identity.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(AuthenticationSchemes = ExtendedIdentityConstants.TwoFactorUserIdScheme)]
    [Route("login")]
    [SecurityHeaders]
    public class MfaController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly TotpServiceFactory _totpServiceFactory;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly IStringLocalizer<MfaController> _localizer;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly ExtendedSignInManager<User> _signInManager;
        public const string Name = "Mfa";

        public MfaController(
            IAccountService accountService,
            TotpServiceFactory totpServiceFactory,
            ExtendedUserManager<User> userManager,
            IStringLocalizer<MfaController> localizer,
            IIdentityServerInteractionService interaction,
            ExtendedSignInManager<User> signInManager
        ) {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        [HttpGet("mfa")]
        public async Task<IActionResult> Index([FromQuery] string returnUrl) {
            var viewModel = await _accountService.BuildMfaLoginViewModelAsync(returnUrl);
            if (viewModel is null) {
                throw new ArgumentNullException(nameof(viewModel));
            }
            var totpService = _totpServiceFactory.Create<User>();
            if (viewModel.DeliveryChannel == TotpDeliveryChannel.Sms) {
                await totpService.SendToSmsAsync(viewModel.User, _localizer["Your OTP code for login is: {0}"], _localizer["OTP login"], TotpConstants.TokenGenerationPurpose.MultiFactorAuthentication);
            } else if (viewModel.DeliveryChannel == TotpDeliveryChannel.PushNotification) {
                // TODO: Send push notification.
            }
            return View(viewModel);
        }

        [HttpPost("mfa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([FromForm] MfaLoginInputModel form) {
            var totpService = _totpServiceFactory.Create<User>();
            var signInResult = await _signInManager.TwoFactorSignInAsync(totpService.TokenProvider, form.OtpCode, form.RememberMe, form.RememberClient);
            if (!signInResult.Succeeded) {
                ModelState.AddModelError(string.Empty, _localizer["The OTP code is not valid."]);
                var viewModel = await _accountService.BuildMfaLoginViewModelAsync(form);
                return View(viewModel);
            }
            if (string.IsNullOrEmpty(form.ReturnUrl)) {
                return Redirect("~/");
            } else if (_interaction.IsValidReturnUrl(form.ReturnUrl) || Url.IsLocalUrl(form.ReturnUrl)) {
                return Redirect(form.ReturnUrl);
            } else {
                throw new Exception("Invalid return URL.");
            }
        }
    }
}

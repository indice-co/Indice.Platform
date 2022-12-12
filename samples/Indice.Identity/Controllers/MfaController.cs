using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public const string Name = "Mfa";

        public MfaController(
            IAccountService accountService,
            TotpServiceFactory totpServiceFactory,
            ExtendedUserManager<User> userManager
        ) {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        [HttpGet("mfa")]
        public async Task<IActionResult> Index([FromQuery] string returnUrl) {
            var viewModel = await _accountService.BuildMfaLoginViewModelAsync(returnUrl);
            var user = await _userManager.GetUserAsync(User);
            if (user is null) { 
                
            }
            var totpService = _totpServiceFactory.Create<User>();
            if (viewModel.DeliveryChannel == TotpDeliveryChannel.Sms) {
                //await totpService.SendToSmsAsync()
            }
            return View(viewModel);
        }
    }
}

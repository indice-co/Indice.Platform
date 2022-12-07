using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity;
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
        public const string Name = "Mfa";

        public MfaController(
            IAccountService accountService
        ) {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        [HttpGet("mfa")]
        public async Task<IActionResult> Index([FromQuery] string returnUrl) {
            var viewModel = await _accountService.BuildMfaLoginViewModelAsync(returnUrl);
            if (viewModel == null) {

            }
            return View(viewModel);
        }
    }
}

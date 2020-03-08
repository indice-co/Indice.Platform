using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResourceOwnerPasswordFlow.Services;

namespace ResourceOwnerPasswordFlow.Controllers
{
    [Authorize]
    [Route("summary")]
    public class SummaryController : Controller
    {
        private readonly IdentityApiService _identityApiService;
        public const string Name = "Summary";

        public SummaryController(IdentityApiService identityApiService) {
            _identityApiService = identityApiService ?? throw new ArgumentNullException(nameof(identityApiService));
        }

        [HttpGet("info")]
        public async Task<ViewResult> Info() {
            var summary = await _identityApiService.GetSystemSummary();
            return View(summary);
        }
    }
}

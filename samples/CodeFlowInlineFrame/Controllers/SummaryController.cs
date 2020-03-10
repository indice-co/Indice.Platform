using System;
using System.Threading.Tasks;
using CodeFlowInlineFrame.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeFlowInlineFrame.Controllers
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

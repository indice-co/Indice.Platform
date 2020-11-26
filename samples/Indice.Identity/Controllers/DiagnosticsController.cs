using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Identity.Controllers
{
    /// <summary>
    /// Diagnostics controller.
    /// </summary>
    [SecurityHeaders]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DiagnosticsController : Controller
    {
        /// <summary>
        /// Displays the diagnostics page.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index() {
            var localAddresses = new string[] { "127.0.0.1", "::1", HttpContext.Connection.LocalIpAddress.ToString() };
            if (!localAddresses.Contains(HttpContext.Connection.RemoteIpAddress.ToString())) {
                return NotFound();
            }
            var model = new DiagnosticsViewModel(await HttpContext.AuthenticateAsync());
            return View(model);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Indice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Indice.Identity.Controllers
{
    /// <summary>
    /// Controller that serves the home and error pages.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IConfiguration _configuration;
        private readonly ISmsServiceFactory _smsProviders;

        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Home";

        /// <summary>
        /// Creates a new instance of <see cref="HomeController"/>.
        /// </summary>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public HomeController(IIdentityServerInteractionService interaction, IConfiguration configuration) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Displays the applicaion's home page.
        /// </summary>
        /// <returns></returns>
        [HttpGet("home")]
        public IActionResult Index() {
            var siteUrl = _configuration[$"{GeneralSettings.Name}:Site"];
            if (!string.IsNullOrWhiteSpace(siteUrl)) {
                return Redirect(siteUrl);
            }
            return View();
        }

        /// <summary>
        /// Displays the error page.
        /// </summary>
        /// <param name="errorId">The id of the error.</param>
        [Route("error")]
        [HttpGet]
        public async Task<IActionResult> Error(string errorId) {
            var viewModel = new ErrorViewModel();
            // Retrieve error details from IdentityServer.
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null) {
                viewModel.Error = message;
            }
            return View(nameof(Error), viewModel);
        }
    }
}

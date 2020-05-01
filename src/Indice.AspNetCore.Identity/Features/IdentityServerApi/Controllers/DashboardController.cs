using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// A controller that provides useful information for the users.
    /// </summary>
    /// <response code="500">Internal Server Error</response>
    [Route("api/dashboard")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProblemDetailsExceptionFilter]
    internal class DashboardController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly ExtendedConfigurationDbContext _configurationDbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Dashboard";

        /// <summary>
        /// Creates a new instance of <see cref="DashboardController"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        public DashboardController(ExtendedUserManager<User> userManager, ExtendedConfigurationDbContext configurationDbContext) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        }

        /// <summary>
        /// Displays blog posts from the official IdentityServer blog.
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet("news")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<BlogItemInfo>))]
        [ResponseCache(VaryByQueryKeys = new[] { "page", "size" }, Duration = 3600/* 1 hour */, Location = ResponseCacheLocation.Client)]
        public IActionResult GetNews([FromQuery]int page = 1, [FromQuery]int size = 100) {
            const string url = "https://www.identityserver.com/rss";
            var feedItems = new List<BlogItemInfo>();
            using (var reader = XmlReader.Create(url)) {
                var feed = SyndicationFeed.Load(reader);
                feedItems.AddRange(
                    feed.Items.Select(post => new BlogItemInfo {
                        Title = post.Title?.Text,
                        Link = post.Links[0].Uri.AbsoluteUri,
                        PublishDate = post.PublishDate.DateTime,
                        Description = post.Summary?.Text
                    })
                );
            }
            var response = feedItems.Skip((page - 1) * size).Take(size).ToArray();
            return Ok(new ResultSet<BlogItemInfo>(response, feedItems.Count));
        }

        /// <summary>
        /// Gets some useful information as a summary of the system.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme)]
        [HttpGet("summary")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SummaryInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> GetSystemSummary() {
            var getUsersNumberTask = _userManager.Users.CountAsync();
            var getClientsNumberTask = _configurationDbContext.Clients.CountAsync();
            var results = await Task.WhenAll(getUsersNumberTask, getClientsNumberTask);
            return Ok(new SummaryInfo {
                NumberOfUsers = results[0],
                NumberOfClients = results[1]
            });
        }
    }
}

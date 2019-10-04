using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using IdentityServer4.EntityFramework.Interfaces;
using Indice.AspNetCore.Identity.Models;
using Indice.Identity.Configuration;
using Indice.Identity.Infrastructure.Extensions;
using Indice.Identity.Models;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Identity.Controllers.Api
{
    /// <summary>
    /// A controller that provides useful information for the users.
    /// </summary>
    [Route("api/dashboard")]
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly UserManager<User> _userManager;
        private readonly IConfigurationDbContext _configurationDbContext;

        /// <summary>
        /// Creates a new instance of <see cref="DashboardController"/>.
        /// </summary>
        /// <param name="cache">Represents a distributed cache of serialized values.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        public DashboardController(IDistributedCache cache, UserManager<User> userManager, IConfigurationDbContext configurationDbContext) {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        }

        /// <summary>
        /// Displays blog posts from the official IdentityServer blog.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("news")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<BlogItemInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ResponseCache(VaryByQueryKeys = new[] { "page", "size" }, Duration = 3600/* 1 hour */, Location = ResponseCacheLocation.Client)]
        public ActionResult<ResultSet<BlogItemInfo>> GetNews([FromQuery]int page = 1, [FromQuery]int size = 100) {
            List<BlogItemInfo> GetFeed() {
                const string url = "https://www.identityserver.com/rss";
                var blogItems = new List<BlogItemInfo>();
                using (var reader = XmlReader.Create(url)) {
                    var feed = SyndicationFeed.Load(reader);
                    blogItems.AddRange(
                        feed.Items.Select(post => new BlogItemInfo {
                            Title = post.Title?.Text,
                            Link = post.Links[0].Uri.AbsoluteUri,
                            PublishDate = post.PublishDate.DateTime,
                            Description = post.Summary?.Text
                        })
                    );
                }
                return blogItems;
            }
            var feedItems = _cache.TryGetOrSet(CacheKeys.News, GetFeed, TimeSpan.FromHours(24));
            var response = feedItems.Skip((page - 1) * size).Take(size).ToArray();
            return Ok(new ResultSet<BlogItemInfo>(response, feedItems.Count));
        }

        /// <summary>
        /// Gets some useful information as a summary of the system.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SummaryInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<SummaryInfo>> GetSystemSummary() {
            async Task<SummaryInfo> GetSummary() {
                var getUsersNumberTask = _userManager.Users.CountAsync();
                var getClientsNumberTask = _configurationDbContext.Clients.CountAsync();
                var results = await Task.WhenAll(getUsersNumberTask, getClientsNumberTask);
                return new SummaryInfo {
                    NumberOfUsers = results[0],
                    NumberOfClients = results[1]
                };
            }
            var summary = await _cache.TryGetOrSetAsync(CacheKeys.Summary, GetSummary, TimeSpan.FromHours(6));
            return Ok(summary);
        }
    }
}

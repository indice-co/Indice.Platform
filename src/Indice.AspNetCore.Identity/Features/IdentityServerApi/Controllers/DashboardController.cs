using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using IdentityModel;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Indice.AspNetCore.Identity.Api.Controllers
{
    /// <summary>
    /// A controller that provides useful information for the users.
    /// </summary>
    /// <response code="500">Internal Server Error</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProblemDetailsExceptionFilter]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/dashboard")]
    internal class DashboardController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly ExtendedConfigurationDbContext _configurationDbContext;
        private readonly RoleManager<Role> _roleManager;
        private readonly IFeatureManager _featureManager;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Dashboard";

        /// <summary>
        /// Creates a new instance of <see cref="DashboardController"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="configurationDbContext">Abstraction for the configuration context.</param>
        /// <param name="roleManager">Provides the APIs for managing roles in a persistence store.</param>
        /// <param name="featureManager">Used to evaluate whether a feature is enabled or disabled.</param>
        public DashboardController(
            ExtendedUserManager<User> userManager,
            ExtendedConfigurationDbContext configurationDbContext,
            RoleManager<Role> roleManager,
            IFeatureManager featureManager
        ) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
        }

        /// <summary>
        /// Displays blog posts from the official IdentityServer blog.
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet("news")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<BlogItemInfo>))]
        [ResponseCache(VaryByQueryKeys = new[] { "page", "size" }, Duration = 3600/* 1 hour */, Location = ResponseCacheLocation.Client)]
        public IActionResult GetNews([FromQuery] int page = 1, [FromQuery] int size = 100) {
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
        [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersOrClientsReader)]
        [CacheResourceFilter(Expiration = 5, VaryByClaimType = new string[] { JwtClaimTypes.Subject })]
        [HttpGet("summary")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SummaryInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetSystemSummary() {
            // Get total number of users in the system.
            var numberOfUsers = User.CanReadUsers() ? await _userManager.Users.CountAsync() : 0;
            // Get total number of roles in the system.
            var numberOfRoles = User.CanReadUsers() ? await _roleManager.Roles.CountAsync() : 0;
            // Get total number of clients in the system.
            var numberOfClients = User.CanReadClients() ? await _configurationDbContext.Clients.CountAsync() : 0;
            var metrics = new SummaryInfo {
                LastUpdatedAt = DateTime.UtcNow,
                TotalUsers = numberOfUsers,
                TotalRoles = numberOfRoles,
                TotalClients = numberOfClients
            };
            var metricsFeatureEnabled = await _featureManager.IsEnabledAsync(IdentityServerApiFeatures.DashboardMetrics);
            if (!metricsFeatureEnabled || !User.CanReadUsers()) {
                return Ok(metrics);
            }
            // Get percentage of active users (users that have logged into the system) on a daily/weekly/monthly basis.
            var dailyActiveUsers = await _userManager.Users.CountAsync(x => x.LastSignInDate >= DateTime.UtcNow.Date);
            var weeklyActiveUsers = await _userManager.Users.CountAsync(x => x.LastSignInDate >= DateTime.UtcNow.Date.AddDays(-7));
            var monthlyActiveUsers = await _userManager.Users.CountAsync(x => x.LastSignInDate >= DateTime.UtcNow.Date.AddDays(-30));
            metrics.Activity = new UsersActivityInfo {
                Day = new SummaryStatistic(count: dailyActiveUsers, percent: Math.Round(dailyActiveUsers / (double)numberOfUsers * 100, 2)),
                Week = new SummaryStatistic(count: weeklyActiveUsers, percent: Math.Round(weeklyActiveUsers / (double)numberOfUsers * 100, 2)),
                Month = new SummaryStatistic(count: monthlyActiveUsers, percent: Math.Round(monthlyActiveUsers / (double)numberOfUsers * 100, 2))
            };
            var userWithVerifiedEmail = await _userManager.Users.CountAsync(x => x.EmailConfirmed);
            var userWithVerifiedPhoneNumber = await _userManager.Users.CountAsync(x => x.PhoneNumberConfirmed);
            metrics.Stats = new UsersStatisticsInfo {
                EmailsVerified = new SummaryStatistic(count: userWithVerifiedEmail, percent: Math.Round(userWithVerifiedEmail / (double)numberOfUsers * 100, 2)),
                PhoneNumbersVerified = new SummaryStatistic(count: userWithVerifiedPhoneNumber, percent: Math.Round(userWithVerifiedPhoneNumber / (double)numberOfUsers * 100, 2))
            };
            return Ok(metrics);
        }
    }
}

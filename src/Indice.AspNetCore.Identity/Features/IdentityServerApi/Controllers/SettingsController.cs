using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Entities;
using Indice.AspNetCore.Identity.EntityFrameworkCore;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;

namespace Indice.AspNetCore.Identity.Api.Controllers
{
    /// <summary>
    /// Contains operations for managing application settings in the database.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/app-settings")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: 400, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
    [ProblemDetailsExceptionFilter]
    internal class SettingsController : ControllerBase
    {
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Settings";

        /// <summary>
        /// Creates an instance of <see cref="SettingsController"/>.
        /// </summary>
        /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
        /// <param name="webHostEnvironment">Provides information about the web hosting environment an application is running in.</param>
        public SettingsController(ExtendedIdentityDbContext<User, Role> dbContext, IWebHostEnvironment webHostEnvironment) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        /// <summary>
        /// Returns a list of <see cref="AppSettingInfo"/> objects containing the total number of app settings in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(statusCode: 200, type: typeof(ResultSet<AppSettingInfo>))]
        public async Task<IActionResult> GetSettings([FromQuery] ListOptions options) {
            var query = _dbContext.AppSettings.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Key.ToLower().Contains(searchTerm));
            }
            var settings = await query.Select(x => new AppSettingInfo {
                Key = x.Key,
                Value = x.Value
            })
            .ToResultSetAsync(options);
            return Ok(settings);
        }

        /// <summary>
        /// Loads the appsettings.json file and saves the configuration in the database.
        /// </summary>
        /// <param name="hardRefresh"></param>
        /// <response code="200">OK</response>
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpPost("load")]
        [ProducesResponseType(statusCode: 200, type: typeof(void))]
        public async Task<IActionResult> LoadFromAppSettings([FromQuery] bool hardRefresh = false) {
            var fileInfo = _webHostEnvironment.ContentRootFileProvider.GetFileInfo("appsettings.json");
            var settingsExist = await _dbContext.AppSettings.AnyAsync();
            if (settingsExist && !hardRefresh) {
                return BadRequest(new ValidationProblemDetails {
                    Detail = "App settings are already loaded in the database."
                });
            }
            IDictionary<string, string> settings;
            using (var stream = fileInfo.CreateReadStream()) {
                settings = JsonConfigurationFileParser.Parse(stream);
            }
            if (settingsExist) {
                await _dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE [{AppSetting.TableSchema}].[{nameof(AppSetting)}];");
            }
            await _dbContext.AppSettings.AddRangeAsync(settings.Select(x => new AppSetting {
                Key = x.Key,
                Value = x.Value
            }));
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Gets an application setting by it's key.
        /// </summary>
        /// <param name="key">The key of the setting.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(AppSettingInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("{key}")]
        public async Task<IActionResult> GetSetting([FromRoute] string key) {
            var setting = await _dbContext.AppSettings.AsNoTracking().Select(x => new AppSettingInfo {
                Key = x.Key,
                Value = x.Value
            })
            .SingleOrDefaultAsync(x => x.Key == key);
            if (setting == null) {
                return NotFound();
            }
            return Ok(setting);
        }

        /// <summary>
        /// Creates a new application setting.
        /// </summary>
        /// <param name="request">Contains info about the application setting to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(AppSettingInfo))]
        public async Task<IActionResult> CreateSetting([FromBody] CreateAppSettingRequest request) {
            var setting = new AppSetting {
                Key = request.Key,
                Value = request.Value
            };
            _dbContext.AppSettings.Add(setting);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSetting), Name, new { key = setting.Key }, new AppSettingInfo {
                Key = setting.Key,
                Value = setting.Value
            });
        }

        /// <summary>
        /// Updates an existing application setting.
        /// </summary>
        /// <param name="key">The key of the setting to update.</param>
        /// <param name="request">Contains info about the application setting to update.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{key}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(AppSettingInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateSetting([FromRoute] string key, [FromBody] UpdateAppSettingRequest request) {
            var setting = await _dbContext.AppSettings.SingleOrDefaultAsync(x => x.Key == key);
            if (setting == null) {
                return NotFound();
            }
            setting.Value = request.Value;
            // Commit changes to database.
            await _dbContext.SaveChangesAsync();
            // Send the response.
            return Ok(new AppSettingInfo {
                Key = setting.Key,
                Value = setting.Value
            });
        }

        /// <summary>
        /// Permanently deletes an application setting.
        /// </summary>
        /// <param name="key">The key of the setting.</param>
        /// <response code="200">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{key}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteSetting([FromRoute] string key) {
            var setting = await _dbContext.AppSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == key);
            if (setting == null) {
                return NotFound();
            }
            _dbContext.AppSettings.Remove(setting);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}

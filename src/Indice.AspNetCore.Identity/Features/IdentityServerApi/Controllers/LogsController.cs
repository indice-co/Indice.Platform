using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for retrieving the event log.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/logs")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status500InternalServerError, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
    [ProblemDetailsExceptionFilter]
    internal class LogsController : Controller
    {
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Logs";

        /// <summary>
        /// Creates an instance of <see cref="LogsController"/>.
        /// </summary>
        /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
        public LogsController(ExtendedIdentityDbContext<User, Role> dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Returns a list of <see cref="LogInfo"/> objects containing the total number of logs in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<LogInfo>))]
        public async Task<ActionResult<ResultSet<LogInfo>>> GetLogs([FromQuery]ListOptions options) {
            using var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            var query = "SELECT COUNT(*) " +
                        "FROM [log].[Logs];" +
                        "SELECT * " +
                        "FROM [log].[Logs] " +
                        "ORDER BY [TimeStamp] DESC " +
                        "OFFSET @Size * (@Page - 1) ROWS " +
                        "FETCH NEXT @Size ROWS ONLY;";
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            command.Parameters.Add(new SqlParameter(nameof(ListOptions.Page), options.Page));
            command.Parameters.Add(new SqlParameter(nameof(ListOptions.Size), options.Size));
            var reader = await command.ExecuteReaderAsync();
            var totalCount = 0;
            var logs = new List<LogInfo>();
            while (await reader.ReadAsync()) {
                totalCount = reader.GetInt32(0);
            }
            await reader.NextResultAsync();
            while (await reader.ReadAsync()) {
                logs.Add(new LogInfo {
                    Id = reader.GetInt32(0),
                    Message = !await reader.IsDBNullAsync(1) ? reader.GetString(1) : default,
                    Level = !await reader.IsDBNullAsync(2) ? reader.GetString(2) : default,
                    Timestamp = !await reader.IsDBNullAsync(3) ? reader.GetDateTime(3) : default(DateTime?),
                    Exception = !await reader.IsDBNullAsync(4) ? reader.GetString(4) : default,
                    UserName = !await reader.IsDBNullAsync(5) ? reader.GetString(5) : default,
                    MachineName = !await reader.IsDBNullAsync(6) ? reader.GetString(6) : default,
                    RequestUrl = !await reader.IsDBNullAsync(7) ? reader.GetString(7) : default,
                    IpAddress = !await reader.IsDBNullAsync(8) ? reader.GetString(8) : default,
                    RequestMethod = !await reader.IsDBNullAsync(9) ? reader.GetString(9) : default
                });
            }
            return Ok(new ResultSet<LogInfo>(logs, totalCount));
        }
    }
}

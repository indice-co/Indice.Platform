using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for device push notifications
    /// </summary>
    [Route("api/my/devices")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme)]
    [ProblemDetailsExceptionFilter]
    internal class DevicesController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Devices";

        public DevicesController(ExtendedUserManager<User> userManager, IPushNotificationService pushNotificationService, ExtendedIdentityDbContext<User, Role> dbContext) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Returns user devices
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        /// <response code="405">Method Not Allowed</response>
        /// <response code="406">Not Acceptable</response>
        /// <response code="408">Request Timeout</response>
        /// <response code="409">Conflict</response>
        /// <response code="415">Unsupported Media Type</response>
        /// <response code="429">Too Many Requests</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="503">Service Unavailable</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(IEnumerable<DeviceInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetDevices() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var devices = await _dbContext.UserDevices.Where(x => x.UserId == user.Id).Select(x => new DeviceInfo {
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                DevicePlatform = x.DevicePlatform,
                IsPushNotificationsEnabled = x.IsPushNotificationsEnabled
            })
            .ToArrayAsync();
            return Ok(devices);
        }

        /// <summary>
        /// Enables push notifications for device
        /// </summary>
        /// <param name="request"><see cref="RegisterDeviceRequest"/></param>
        /// <response code="201">Created</response>
        /// <response code="204">No Content</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        /// <response code="405">Method Not Allowed</response>
        /// <response code="406">Not Acceptable</response>
        /// <response code="408">Request Timeout</response>
        /// <response code="409">Conflict</response>
        /// <response code="415">Unsupported Media Type</response>
        /// <response code="429">Too Many Requests</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="503">Service Unavailable</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(DeviceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == request.DeviceId);
            await _pushNotificationService.Register(request.DeviceId.ToString(), request.PnsHandle, request.DevicePlatform, user.Id,  request.Tags?.ToArray());
            var deviceId = default(Guid);
            if (device != null) {
                device.IsPushNotificationsEnabled = true;
                device.DeviceName = request.DeviceName;
                deviceId = device.Id;
            } else {
                var deviceToAdd = new UserDevice {
                    DeviceId = request.DeviceId,
                    DeviceName = request.DeviceName,
                    DevicePlatform = request.DevicePlatform,
                    IsPushNotificationsEnabled = true,
                    UserId = user.Id,
                    DateCreated = DateTimeOffset.Now
                };
                _dbContext.UserDevices.Add(deviceToAdd);
                deviceId = deviceToAdd.Id;
            }
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDevices), Name, new { deviceId });
        }

        /// <summary>
        /// Disable push noitications for this device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <response code="204">No Content</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not found</response>
        /// <response code="405">Method Not Allowed</response>
        /// <response code="406">Not Acceptable</response>
        /// <response code="408">Request Timeout</response>
        /// <response code="409">Conflict</response>
        /// <response code="415">Unsupported Media Type</response>
        /// <response code="429">Too Many Requests</response>
        /// <response code="500">Internal Server Error</response>
        /// <response code="503">Service Unavailable</response>
        [HttpDelete("{deviceId}/unregister")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UnRegisterDevice([FromRoute] Guid deviceId) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var device = _dbContext.UserDevices.SingleOrDefault(x => x.UserId == user.Id && x.DeviceId == deviceId);
            if (device == null) {
                return NotFound();
            }
            await _pushNotificationService.UnRegister(deviceId.ToString());
            device.IsPushNotificationsEnabled = false;
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}

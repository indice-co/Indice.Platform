using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Api
{
    /// <summary>
    /// Contains operations for device push notifications.
    /// </summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="405">Method Not Allowed</response>
    /// <response code="406">Not Acceptable</response>
    /// <response code="408">Request Timeout</response>
    /// <response code="409">Conflict</response>
    /// <response code="415">Unsupported Media Type</response>
    /// <response code="429">Too Many Requests</response>
    /// <response code="500">Internal Server Error</response>
    /// <response code="503">Service Unavailable</response>
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
        private readonly IEventService _eventService;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Devices";

        public DevicesController(ExtendedUserManager<User> userManager, IPushNotificationService pushNotificationService, ExtendedIdentityDbContext<User, Role> dbContext, IEventService eventService) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        /// <summary>
        /// Returns a list of registered user devices.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<DeviceInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetDevices([FromQuery] ListOptions<UserDeviceFilter> options = null) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var devices = await _dbContext.UserDevices.Where(UserDevicePredicate(user.Id, options)).Select(x => DeviceInfo.FromUserDevice(x)).ToResultSetAsync(options);
            return Ok(devices);
        }

        /// <summary>
        /// Gets a device by it's unique id.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{deviceId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(DeviceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetDeviceById([FromRoute] string deviceId) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId);
            if (device == null) {
                return NotFound();
            }
            return Ok(DeviceInfo.FromUserDevice(device));
        }

        /// <summary>
        /// Creates a new device and optionally registers for push notifications.
        /// </summary>
        /// <param name="request">Contains information about the device to register.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(DeviceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> CreateDevice([FromBody] RegisterDeviceRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == request.DeviceId);
            if (device != null) {
                ModelState.AddModelError(nameof(request.DeviceId), $"A device with id {request.DeviceId} already exists.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var isPushNotificationsEnabled = !string.IsNullOrWhiteSpace(request.PnsHandle);
            device = new UserDevice {
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                DevicePlatform = request.DevicePlatform,
                IsPushNotificationsEnabled = isPushNotificationsEnabled,
                UserId = user.Id,
                DateCreated = DateTimeOffset.UtcNow
            };
            _dbContext.UserDevices.Add(device);
            await _dbContext.SaveChangesAsync();
            if (isPushNotificationsEnabled) {
                await _pushNotificationService.Register(request.DeviceId, request.PnsHandle, request.DevicePlatform, user.Id, request.Tags?.ToArray());
            }
            var response = DeviceInfo.FromUserDevice(device);
            var @event = new DeviceCreatedEvent(response, SingleUserInfo.FromUser(user));
            await _eventService.Raise(@event);
            return CreatedAtAction(nameof(GetDeviceById), new { deviceId = device.DeviceId }, response);
        }

        /// <summary>
        /// Updates a device.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="request">Contains information about the device to register.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{deviceId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateDevice([FromRoute] string deviceId, [FromBody] UpdateDeviceRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.UserId == user.Id && x.DeviceId == deviceId);
            if (device == null) {
                return NotFound();
            }
            var shouldRegisterDevice = !device.IsPushNotificationsEnabled && request.IsPushNotificationsEnabled;
            if (shouldRegisterDevice && string.IsNullOrWhiteSpace(request.PnsHandle)) {
                ModelState.AddModelError(nameof(request.PnsHandle), $"Pns handle is required in order to enable push notifications.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var shouldUnRegisterDevice = device.IsPushNotificationsEnabled && !request.IsPushNotificationsEnabled;
            device.IsPushNotificationsEnabled = request.IsPushNotificationsEnabled;
            device.DeviceName = request.DeviceName;
            await _dbContext.SaveChangesAsync();
            if (shouldUnRegisterDevice) {
                await _pushNotificationService.UnRegister(deviceId);
            }
            if (shouldRegisterDevice) {
                await _pushNotificationService.Register(device.DeviceId, request.PnsHandle, device.DevicePlatform, user.Id, request.Tags?.ToArray());
            }
            var @event = new DeviceUpdatedEvent(DeviceInfo.FromUserDevice(device), SingleUserInfo.FromUser(user));
            await _eventService.Raise(@event);
            return NoContent();
        }

        /// <summary>
        /// Deletes the device.
        /// </summary>
        /// <param name="deviceId">The id of the device.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{deviceId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteDevice([FromRoute] string deviceId) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var device = _dbContext.UserDevices.SingleOrDefault(x => x.UserId == user.Id && x.DeviceId == deviceId);
            if (device == null) {
                return NotFound();
            }
            await _pushNotificationService.UnRegister(deviceId);
            _dbContext.UserDevices.Remove(device);
            await _dbContext.SaveChangesAsync();
            var @event = new DeviceDeletedEvent(DeviceInfo.FromUserDevice(device), SingleUserInfo.FromUser(user));
            await _eventService.Raise(@event);
            return NoContent();
        }

        private Expression<Func<UserDevice, bool>> UserDevicePredicate(string userId, ListOptions<UserDeviceFilter> options) =>
            options?.Filter.IsPushNotificationEnabled == null
                ? x => x.UserId == userId
                : x => x.UserId == userId && x.IsPushNotificationsEnabled == options.Filter.IsPushNotificationEnabled.Value;
    }
}

using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Api
{
    /// <summary>Contains operations for device push notifications.</summary>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/my/devices")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status500InternalServerError, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme)]
    internal class DevicesController : ControllerBase
    {
        /// <summary>The name of the controller.</summary>
        public const string Name = "Devices";

        public DevicesController(
            ExtendedUserManager<User> userManager,
            IPushNotificationService pushNotificationService,
            ILogger<DevicesController> logger
        ) {
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            PushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ExtendedUserManager<User> UserManager { get; }
        public IPushNotificationService PushNotificationService { get; }
        public ILogger<DevicesController> Logger { get; }

        /// <summary>Returns a list of registered user devices.</summary>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<DeviceInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetDevices([FromQuery] ListOptions<UserDeviceFilter> options = null) {
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var query = UserManager.UserDevices.Where(device => device.UserId == user.Id);
            if (options.Filter is not null) {
                var filter = options.Filter;
                query = query.Where(device =>
                    (filter.IsPushNotificationEnabled == null || device.IsPushNotificationsEnabled == filter.IsPushNotificationEnabled) &&
                    (filter.IsTrusted == null || device.IsTrusted == filter.IsTrusted)
                );
            }
            var devices = await query.Select(DeviceInfoExtensions.ToDeviceInfo).ToResultSetAsync(options);
            return Ok(devices);
        }

        /// <summary>Gets a device by it's unique id.</summary>
        /// <param name="deviceId">The device id.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{deviceId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(DeviceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> GetDeviceById([FromRoute] string deviceId) {
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var device = await UserManager.GetDeviceByIdAsync(user, deviceId);
            if (device == null) {
                return NotFound();
            }
            return Ok(DeviceInfoExtensions.ToDeviceInfo.Compile()(device));
        }

        /// <summary>Creates a new device and optionally registers for push notifications.</summary>
        /// <param name="request">Contains information about the device to register.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Not Found</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(DeviceInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> CreateDevice([FromBody] RegisterDeviceRequest request) {
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var device = await UserManager.GetDeviceByIdAsync(user, request.DeviceId);
            if (device is not null) {
                ModelState.AddModelError(nameof(request.DeviceId), $"A device with id {request.DeviceId} already exists.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            var id = Guid.NewGuid();
            var shouldEnablePushNotifications = !string.IsNullOrWhiteSpace(request.PnsHandle);
            if (shouldEnablePushNotifications) {
                try {
                    await PushNotificationService.Register(id.ToString(), request.PnsHandle, request.Platform, user.Id, request.Tags?.ToArray());
                } catch (Exception exception) {
                    Logger.LogError("An exception occurred when connection to Azure Notification Hubs. Exception is '{Exception}'. Inner Exception is '{InnerException}'.", exception.Message, exception.InnerException?.Message ?? "N/A");
                    throw;
                }
            }
            device = new UserDevice(id) {
                Data = request.Data,
                DateCreated = DateTimeOffset.UtcNow,
                DeviceId = request.DeviceId,
                IsPushNotificationsEnabled = shouldEnablePushNotifications,
                Model = request.Model,
                Name = request.Name,
                OsVersion = request.OsVersion,
                Platform = request.Platform,
                PnsHandle = request.PnsHandle,
                Tags = request.Tags?.ToArray()
            };
            var result = await UserManager.CreateDeviceAsync(user, device);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            var response = DeviceInfoExtensions.ToDeviceInfo.Compile()(device);
            return CreatedAtAction(nameof(GetDeviceById), new { deviceId = device.DeviceId }, response);
        }

        /// <summary>Updates a device.</summary>
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
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var device = await UserManager.GetDeviceByIdAsync(user, deviceId);
            if (device is null) {
                return NotFound();
            }
            var shouldEnablePushNotifications = !string.IsNullOrWhiteSpace(request.PnsHandle);
            var shouldUnRegisterDevice = device.IsPushNotificationsEnabled && !shouldEnablePushNotifications;
            var shouldRegisterDevice = (!device.IsPushNotificationsEnabled && shouldEnablePushNotifications) || device.PnsHandle != request.PnsHandle;
            try {
                if (shouldUnRegisterDevice) {
                    await PushNotificationService.UnRegister(device.Id.ToString());
                }
                if (shouldRegisterDevice) {
                    await PushNotificationService.Register(device.Id.ToString(), request.PnsHandle, device.Platform, user.Id, request.Tags?.ToArray());
                }
            } catch (Exception exception) {
                Logger.LogError("An exception occurred when connection to Azure Notification Hubs. Exception is '{Exception}'. Inner Exception is '{InnerException}'.", exception.Message, exception.InnerException?.Message ?? "N/A");
                throw;
            }
            device.IsPushNotificationsEnabled = shouldEnablePushNotifications;
            device.Name = request.Name;
            device.Model = request.Model;
            device.OsVersion = request.OsVersion;
            device.Data = request.Data;
            device.PnsHandle = request.PnsHandle;
            device.Tags = request.Tags?.ToArray();
            await UserManager.UpdateDeviceAsync(user, device);
            return NoContent();
        }

        /// <summary>Starts the process of trusting a device.</summary>
        /// <param name="deviceId">The device id.</param>
        /// <param name="request">Trust device parameters payload.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{deviceId}/trust")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [TrustDeviceRequiresOtp]
        public async Task<IActionResult> TrustDevice([FromRoute] string deviceId, [FromBody] TrustDeviceRequest request) {
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var device = await UserManager.GetDeviceByIdAsync(user, deviceId);
            if (device is null) {
                return NotFound();
            }
            var result = await UserManager.SetTrustedDevice(user, device, request.SwapDeviceId);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return NoContent();
        }

        /// <summary>Sets a device as untrusted.</summary>
        /// <param name="deviceId">The device id.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{deviceId}/untrust")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> UntrustDevice([FromRoute] string deviceId) {
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var device = await UserManager.GetDeviceByIdAsync(user, deviceId);
            if (device is null) {
                return NotFound();
            }
            var result = await UserManager.SetUntrustedDevice(user, device);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return NoContent();
        }

        /// <summary>Deletes the device.</summary>
        /// <param name="deviceId">The id of the device.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{deviceId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteDevice([FromRoute] string deviceId) {
            var user = await UserManager.GetUserAsync(User);
            if (user is null) {
                return NotFound();
            }
            var device = await UserManager.GetDeviceByIdAsync(user, deviceId);
            if (device is null) {
                return NotFound();
            }
            try {
                await PushNotificationService.UnRegister(device.Id.ToString());
            } catch (Exception exception) {
                Logger.LogError("An exception occurred when connection to Azure Notification Hubs. Exception is '{Exception}'. Inner Exception is '{InnerException}'.", exception.Message, exception.InnerException?.Message ?? "N/A");
            }
            await UserManager.RemoveDeviceAsync(user, device);
            return NoContent();
        }
    }
}

using System.Security.Claims;
using Humanizer;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.Server.Devices.Models;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Server.Devices;

internal static class DeviceHandlers
{
    internal static async Task<Results<Ok<ResultSet<DeviceInfo>>, NotFound>> GetDevices(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        [AsParameters] ListOptions options,
        [AsParameters] UserDeviceListFilter filter
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var devices = await userManager
            .UserDevices
            .Where(device => device.UserId == user.Id)
            .ApplyFilter(filter)
            .Select(DeviceInfoExtensions.ToDeviceInfo)
            .ToResultSetAsync(options);
        return TypedResults.Ok(devices);
    }

    internal static async Task<Results<Ok<DeviceInfo>, NotFound>> GetDeviceById(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        string deviceId
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var device = await userManager.GetDeviceByIdAsync(user, deviceId);
        if (device == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(DeviceInfoExtensions.ToDeviceInfo.Compile()(device));
    }

    internal static async Task<Results<CreatedAtRoute<DeviceInfo>, NotFound, ValidationProblem>> CreateDevice(
        ExtendedUserManager<User> userManager,
        IPushNotificationService pushNotificationService,
        ILogger<IPushNotificationService> logger,
        ClaimsPrincipal currentUser,
        RegisterDeviceRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var device = await userManager.GetDeviceByIdAsync(user, request.DeviceId);
        if (device is not null) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(request.DeviceId).Camelize(), $"A device with id {request.DeviceId} already exists."));
        }
        var id = Guid.NewGuid();
        var shouldEnablePushNotifications = !string.IsNullOrWhiteSpace(request.PnsHandle);
        if (shouldEnablePushNotifications) {
            try {
                await pushNotificationService.Register(id.ToString(), request.PnsHandle, request.Platform, user.Id, request.Tags?.ToArray());
            } catch (Exception exception) {
                logger.LogError("An exception occurred when connection to Azure Notification Hubs. Exception is '{Exception}'. Inner Exception is '{InnerException}'.", exception.Message, exception.InnerException?.Message ?? "N/A");
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
            Tags = request.Tags?.ToArray(),
            ClientType = request.ClientType ?? DeviceClientType.Native
        };
        var result = await userManager.CreateDeviceAsync(user, device);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        var response = DeviceInfoExtensions.ToDeviceInfo.Compile()(device);
        return TypedResults.CreatedAtRoute(response, nameof(GetDeviceById), new { deviceId = device.DeviceId });
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateDevice(
        ExtendedUserManager<User> userManager,
        IPushNotificationService pushNotificationService,
        ILogger<IPushNotificationService> logger,
        ClaimsPrincipal currentUser,
        string deviceId, UpdateDeviceRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var device = await userManager.GetDeviceByIdAsync(user, deviceId);
        if (device is null) {
            return TypedResults.NotFound();
        }
        var shouldEnablePushNotifications = !string.IsNullOrWhiteSpace(request.PnsHandle);
        var shouldUnRegisterDevice = device.IsPushNotificationsEnabled && !shouldEnablePushNotifications;
        var shouldRegisterDevice = (!device.IsPushNotificationsEnabled && shouldEnablePushNotifications) || (device.PnsHandle != request.PnsHandle && shouldEnablePushNotifications);
        try {
            if (shouldUnRegisterDevice) {
                await pushNotificationService.UnRegister(device.Id.ToString());
            }
            if (shouldRegisterDevice) {
                await pushNotificationService.Register(device.Id.ToString(), request.PnsHandle, device.Platform, user.Id, request.Tags?.ToArray());
            }
        } catch (Exception exception) {
            logger.LogError("An exception occurred when connection to Azure Notification Hubs. Exception is '{Exception}'. Inner Exception is '{InnerException}'.", exception.Message, exception.InnerException?.Message ?? "N/A");
            throw;
        }
        device.IsPushNotificationsEnabled = shouldEnablePushNotifications;
        device.Name = request.Name;
        device.Model = request.Model;
        device.OsVersion = request.OsVersion;
        device.Data = request.Data;
        device.PnsHandle = request.PnsHandle;
        device.Tags = request.Tags?.ToArray();
        var result = await userManager.UpdateDeviceAsync(user, device);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }
    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> TrustDevice(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        string deviceId,
        TrustDeviceRequest request
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var device = await userManager.GetDeviceByIdAsync(user, deviceId);
        if (device is null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.SetTrustedDevice(user, device, request.SwapDeviceId);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound, ValidationProblem>> UntrustDevice(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        string deviceId
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var device = await userManager.GetDeviceByIdAsync(user, deviceId);
        if (device is null) {
            return TypedResults.NotFound();
        }
        var result = await userManager.SetUntrustedDevice(user, device);
        if (!result.Succeeded) {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }
        return TypedResults.NoContent();
    }

    internal static async Task<Results<NoContent, NotFound>> DeleteDevice(
        ExtendedUserManager<User> userManager,
        ClaimsPrincipal currentUser,
        IPushNotificationService pushNotificationService,
        ILogger<IPushNotificationService> logger,
        string deviceId
    ) {
        var user = await userManager.GetUserAsync(currentUser);
        if (user is null) {
            return TypedResults.NotFound();
        }
        var device = await userManager.GetDeviceByIdAsync(user, deviceId);
        if (device is null) {
            return TypedResults.NotFound();
        }
        try {
            await pushNotificationService.UnRegister(device.Id.ToString());
        } catch (Exception exception) {
            logger.LogError("An exception occurred when connection to Azure Notification Hubs. Exception is '{Exception}'. Inner Exception is '{InnerException}'.", exception.Message, exception.InnerException?.Message ?? "N/A");
        }
        await userManager.RemoveDeviceAsync(user, device);
        return TypedResults.NoContent();
    }
}

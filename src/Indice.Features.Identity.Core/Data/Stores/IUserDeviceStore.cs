using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.Features.Identity.Core.Data.Stores;

/// <summary>Provides an abstraction for a store which maps users to devices.</summary>
/// <typeparam name="TUser">The user type.</typeparam>
public interface IUserDeviceStore<TUser> where TUser : User
{
    /// <summary>Returns an <see cref="IQueryable{Device}"/> collection of devices.</summary>
    public IQueryable<UserDevice> UserDevices { get; }
    /// <summary>Adds a new device to the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="device">The device to create.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
    Task<IdentityResult> CreateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default);
    /// <summary>Gets the devices registered by the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="filter">Contains filter when querying for user device list.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
    Task<IList<UserDevice>> GetDevicesAsync(TUser user, UserDeviceListFilter filter = null, CancellationToken cancellationToken = default);
    /// <summary>Gets the devices count registered by the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="filter">Contains filter when querying for user device list.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
    Task<int> GetDevicesCountAsync(TUser user, UserDeviceListFilter filter = null, CancellationToken cancellationToken = default);
    /// <summary>Gets the device registered by the specified user, using it's unique id.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="deviceId">The id of the device to look for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
    Task<UserDevice> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken = default);
    /// <summary>Updates the given device. If the device does not exists, it is automatically created.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="device">The device to update.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
    Task<IdentityResult> UpdateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default);
    /// <summary>Permanently deletes the specified device from the user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="device">The device to delete.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task RemoveDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken = default);
    /// <summary>Blocks all native devices for fingerprint or 4-pin login and requires credentials.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="requiresPassword">Boolean value for <see cref="UserDevice.RequiresPassword"/> field.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task<IdentityResult> SetNativeDevicesRequirePasswordAsync(TUser user, bool requiresPassword, CancellationToken cancellationToken = default);
    /// <summary>Sets the MFA session expiration date for all user's browser devices.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="expirationDate">The <see cref="DateTimeOffset"/> value to use as expiration date.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    Task<IdentityResult> SetBrowsersMfaSessionExpirationDate(TUser user, DateTimeOffset? expirationDate, CancellationToken cancellationToken = default);
}

using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Stores;

/// <summary>A store that manages the user registered devices.</summary>
public interface IUserDeviceStore
{
    /// <summary>Gets a device given the unique id.</summary>
    /// <param name="id">The id.</param>
    Task<DbUserDevice> GetById(Guid id);
    /// <summary>Gets a device given the device id.</summary>
    /// <param name="deviceId">The device id.</param>
    Task<DbUserDevice> GetByDeviceId(string deviceId);
    /// <summary>Updates the <see cref="DbUserDevice.Password"/> field of an existing device.</summary>
    /// <param name="device">The device to update.</param>
    /// <param name="passwordHash">The password hash.</param>
    Task UpdatePassword(DbUserDevice device, string passwordHash);
    /// <summary>Updates the <see cref="DbUserDevice.PublicKey"/> field for a device.</summary>
    /// <param name="device">The device to update.</param>
    /// <param name="publicKey">The new public key.</param>
    Task UpdatePublicKey(DbUserDevice device, string publicKey);
    /// <summary>Updates the <see cref="DbUserDevice.LastSignInDate"/> field for a device.</summary>
    /// <param name="device">The device to update.</param>
    Task UpdateLastSignInDate(DbUserDevice device);
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>
    /// A store that manages the user registered devices.
    /// </summary>
    public interface IUserDeviceStore
    {
        /// <summary>
        /// Gets all the registered devices for a given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        Task<List<UserDevice>> GetUserDevices(string userId);
        /// <summary>
        /// Gets a device by it's unique device id.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        Task<UserDevice> GetByDeviceId(string deviceId);
        /// <summary>
        /// Creates a new device in the underlying store, for the given user.
        /// </summary>
        /// <param name="device">The user device data.</param>
        Task CreateDevice(UserDevice device);
        /// <summary>
        /// Updates the password of an existing device.
        /// </summary>
        /// <param name="device">The device to update.</param>
        /// <param name="passwordHash">The password hash.</param>
        Task UpdateDevicePassword(UserDevice device, string passwordHash);
        /// <summary>
        /// Updates the public key for a device.
        /// </summary>
        /// <param name="device">The device to update.</param>
        /// <param name="publicKey">The new public key.</param>
        Task UpdateDevicePublicKey(UserDevice device, string publicKey);
    }
}

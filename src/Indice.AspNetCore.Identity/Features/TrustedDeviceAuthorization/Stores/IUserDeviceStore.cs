using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>A store that manages the user registered devices.</summary>
    public interface IUserDeviceStore
    {
        /// <summary>Gets a device given the unique id.</summary>
        /// <param name="id">The id.</param>
        Task<UserDevice> GetById(Guid id);
        /// <summary>Gets a device given the device id.</summary>
        /// <param name="deviceId">The device id.</param>
        Task<UserDevice> GetByDeviceId(string deviceId);
        /// <summary>Updates the <see cref="UserDevice.Password"/> field of an existing device.</summary>
        /// <param name="device">The device to update.</param>
        /// <param name="passwordHash">The password hash.</param>
        Task UpdatePassword(UserDevice device, string passwordHash);
        /// <summary>Updates the <see cref="UserDevice.PublicKey"/> field for a device.</summary>
        /// <param name="device">The device to update.</param>
        /// <param name="publicKey">The new public key.</param>
        Task UpdatePublicKey(UserDevice device, string publicKey);
        /// <summary>Updates the <see cref="UserDevice.LastSignInDate"/> field for a device.</summary>
        /// <param name="device">The device to update.</param>
        Task UpdateLastSignInDate(UserDevice device);
    }
}

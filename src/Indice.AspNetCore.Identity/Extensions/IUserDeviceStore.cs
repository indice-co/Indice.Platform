using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary>Provides an abstraction for a store which maps users to devices.</summary>
    /// <typeparam name="TUser">The user type.</typeparam>
    public interface IUserDeviceStore<TUser> where TUser : User
    {
        /// <summary>Returns an <see cref="IQueryable{Device}"/> collection of devices.</summary>
        public IQueryable<UserDevice> UserDevices { get; }
        /// <summary>The maximum number of devices a user can register.</summary>
        public int? MaxAllowedRegisteredDevices { get; }
        /// <summary>The default number of devices a user can register.</summary>
        public int? DefaultAllowedRegisteredDevices { get; }
        /// <summary>Adds a new device to the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        Task<IdentityResult> AddDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken);
        /// <summary>Get the devices registered by the specified user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
        Task<IList<UserDevice>> GetDevicesAsync(TUser user, CancellationToken cancellationToken);
        /// <summary>Get the device registered by the specified user, using it's unique id.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="deviceId">The id of the device to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
        Task<UserDevice> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken);
        /// <summary>Updates the given device. If the device does not exists, it is automatically created.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to update (or create).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
        Task<IdentityResult> UpdateDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken);
        /// <summary>Sets the maximum number of devices a user can register.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="maxDevicesCount">The number of devices.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
        Task<IdentityResult> SetMaxDevicesCountAsync(TUser user, int maxDevicesCount, CancellationToken cancellationToken);
        /// <summary>Permanently deletes the specified device from the user.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task RemoveDeviceAsync(TUser user, UserDevice device, CancellationToken cancellationToken);
        /// <summary>Blocks devices for fingerprint or 4-pin login and requires credentials.</summary>
        /// <param name="user">The user instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        Task SetDevicesRequirePasswordAsync(TUser user, CancellationToken cancellationToken);
    }
}

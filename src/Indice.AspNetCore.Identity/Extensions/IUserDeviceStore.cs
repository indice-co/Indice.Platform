using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Provides an abstraction for a store which maps users to devices.
    /// </summary>
    /// <typeparam name="TUser">The user type.</typeparam>
    public interface IUserDeviceStore<TUser> where TUser : User
    {
        /// <summary>
        /// Adds a new device to the specified user.
        /// </summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        Task<IdentityResult> AddDeviceAsync(TUser user, Device device, CancellationToken cancellationToken);
        /// <summary>
        /// Get the devices registed by the specified user.
        /// </summary>
        /// <param name="user">The user instance.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user devices.</returns>
        Task<IList<Device>> GetDevicesAsync(TUser user, CancellationToken cancellationToken);
        /// <summary>
        /// Get the device registed by the specified user, using it's unique id.
        /// </summary>
        /// <param name="user">The user instance.</param>
        /// <param name="deviceId">The id of the device to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
        Task<Device> GetDeviceByIdAsync(TUser user, string deviceId, CancellationToken cancellationToken);
        /// <summary>
        /// Updates the given device. If the device does not exists, it is automatically created.
        /// </summary>
        /// <param name="user">The user instance.</param>
        /// <param name="device">The device to update (or create).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user device, if any.</returns>
        Task<IdentityResult> UpdateDeviceAsync(TUser user, Device device, CancellationToken cancellationToken);
    }
}

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
        Task<IEnumerable<UserDevice>> GetUserDevices(string userId);
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
    }
}

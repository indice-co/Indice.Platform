using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// A store that manages the user registered devices.
    /// </summary>
    public interface IUserDeviceStore
    {
        /// <summary>
        /// Gets a device by it's unique device id.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        Task<UserDevice> GetByDeviceId(string deviceId);
    }
}

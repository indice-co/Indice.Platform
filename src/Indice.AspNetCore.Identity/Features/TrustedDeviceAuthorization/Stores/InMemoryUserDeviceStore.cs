using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>
    /// An implementation of <see cref="IUserDeviceStore"/> that stores user devices in-memory.
    /// </summary>
    public class InMemoryUserDeviceStore : IUserDeviceStore
    {
        private readonly IList<UserDevice> _userDevices = new List<UserDevice>();

        /// <inheritdoc />
        public Task<IEnumerable<UserDevice>> GetUserDevices(string userId) => Task.FromResult(_userDevices.Where(x => x.UserId == userId));

        /// <inheritdoc />
        public Task<UserDevice> GetByDeviceId(string deviceId) {
            var userDevice = _userDevices.SingleOrDefault(x => x.DeviceId == deviceId);
            return Task.FromResult(userDevice);
        }

        /// <inheritdoc />
        public Task CreateDevice(UserDevice device) {
            _userDevices.Add(device);
            return Task.CompletedTask;
        }
    }
}

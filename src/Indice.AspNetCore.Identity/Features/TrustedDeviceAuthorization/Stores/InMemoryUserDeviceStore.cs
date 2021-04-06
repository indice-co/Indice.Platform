using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// An implementation of <see cref="IUserDeviceStore"/> that stores user devices in-memory.
    /// </summary>
    public class InMemoryUserDeviceStore : IUserDeviceStore
    {
        private readonly IEnumerable<UserDevice> _userDevices = new List<UserDevice>();

        /// <inheritdoc />
        public Task<UserDevice> GetByDeviceId(string deviceId) {
            var userDevice = _userDevices.SingleOrDefault(x => x.DeviceId == deviceId);
            return Task.FromResult(userDevice);
        }
    }
}

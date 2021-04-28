using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>
    /// An implementation of <see cref="IUserDeviceStore"/> that stores user devices in a relational database, using Entity Framework Core.
    /// </summary>
    public class UserDeviceStoreEntityFrameworkCore : IUserDeviceStore
    {
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;

        /// <summary>
        /// Creates a new instance of <see cref="UserDeviceStoreEntityFrameworkCore"/>.
        /// </summary>
        /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
        public UserDeviceStoreEntityFrameworkCore(ExtendedIdentityDbContext<User, Role> dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task CreateDevice(UserDevice device) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
            }
            _dbContext.UserDevices.Add(device);
            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public Task<UserDevice> GetByDeviceId(string deviceId) {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return default;
            }
            return _dbContext.UserDevices.SingleOrDefaultAsync(x => x.DeviceId == deviceId);
        }

        /// <inheritdoc />
        public Task<List<UserDevice>> GetUserDevices(string userId) {
            if (string.IsNullOrWhiteSpace(userId)) {
                return default;
            }
            return _dbContext.UserDevices.Where(x => x.UserId == userId).ToListAsync();
        }

        /// <inheritdoc />
        public async Task SetDevicePublicKey(string deviceId, string publicKey) {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId), $"Parameters {nameof(deviceId)} cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(publicKey)) {
                throw new ArgumentNullException(nameof(publicKey), $"Parameters {nameof(publicKey)} cannot be null.");
            }
            var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.DeviceId == deviceId);
            if (device == null) {
                return;
            }
            device.PublicKey = publicKey;
            await _dbContext.SaveChangesAsync();
        }
    }
}

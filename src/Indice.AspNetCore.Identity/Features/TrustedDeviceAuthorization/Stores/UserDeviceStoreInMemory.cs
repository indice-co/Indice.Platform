using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>
    /// An implementation of <see cref="IUserDeviceStore"/> that stores user devices in-memory.
    /// </summary>
    public class UserDeviceStoreInMemory : IUserDeviceStore
    {
        private readonly IList<UserDevice> _userDevices = new List<UserDevice>();

        /// <inheritdoc />
        public Task<List<UserDevice>> GetUserDevices(string userId) => Task.FromResult(_userDevices.Where(x => x.UserId == userId).ToList());

        /// <inheritdoc />
        public Task<UserDevice> GetByDeviceId(string deviceId) {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId), $"Parameter {nameof(deviceId)} cannot be null or empty.");
            }
            var userDevice = _userDevices.SingleOrDefault(x => x.DeviceId == deviceId);
            return Task.FromResult(userDevice);
        }

        /// <inheritdoc />
        public Task<IdentityResult> CreateDevice(UserDevice device) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
            }
            var existingDevice = _userDevices.SingleOrDefault(x => x.DeviceId == device.DeviceId || x.Id == device.Id);
            if (existingDevice != null) {
                throw new ArgumentException("Device already exists.");
            }
            _userDevices.Add(device);
            return Task.FromResult(IdentityResult.Success);
        }

        /// <inheritdoc />
        public Task UpdatePassword(UserDevice device, string passwordHash) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
            }
            var foundDevice = _userDevices.Single(x => x.Id == device.Id);
            foundDevice.Password = passwordHash;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdatePublicKey(UserDevice device, string publicKey) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
            }
            var foundDevice = _userDevices.Single(x => x.Id == device.Id);
            foundDevice.PublicKey = publicKey;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdateLastSignInDate(UserDevice device) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
            }
            var foundDevice = _userDevices.Single(x => x.Id == device.Id);
            foundDevice.LastSignInDate = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }
    }
}

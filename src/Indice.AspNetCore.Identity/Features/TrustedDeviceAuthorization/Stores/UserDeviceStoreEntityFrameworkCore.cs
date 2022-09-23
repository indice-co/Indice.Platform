using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>An implementation of <see cref="IUserDeviceStore"/> that stores user devices in a relational database, using Entity Framework Core.</summary>
    public class UserDeviceStoreEntityFrameworkCore : IUserDeviceStore
    {
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly IPlatformEventService _eventService;
        private readonly IConfiguration _configuration;

        /// <summary>Creates a new instance of <see cref="UserDeviceStoreEntityFrameworkCore"/>.</summary>
        /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
        /// <param name="eventService">Models the event mechanism used to raise events inside the platform.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public UserDeviceStoreEntityFrameworkCore(
            ExtendedIdentityDbContext<User, Role> dbContext,
            IPlatformEventService eventService,
            IConfiguration configuration
        ) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public Task<List<UserDevice>> GetUserDevices(string userId) {
            if (string.IsNullOrWhiteSpace(userId)) {
                return default;
            }
            return _dbContext.UserDevices.Where(x => x.UserId == userId).ToListAsync();
        }

        /// <inheritdoc />
        public Task<UserDevice> GetByDeviceId(string deviceId) {
            if (string.IsNullOrWhiteSpace(deviceId)) {
                return default;
            }
            return _dbContext.UserDevices.SingleOrDefaultAsync(x => x.DeviceId == deviceId);
        }

        /// <inheritdoc />
        public async Task<IdentityResult> CreateDevice(UserDevice device) {
            GuardDevice(device);
            var maxDevicesCountClaim = await _dbContext.UserClaims.FirstOrDefaultAsync(x => x.ClaimType == BasicClaimTypes.MaxDevicesCount);
            int? userMaxDevicesCount = null;
            if (maxDevicesCountClaim is not null && int.TryParse(maxDevicesCountClaim.ClaimValue, out var parsedUserMaxDevicesClaim)) {
                userMaxDevicesCount = parsedUserMaxDevicesClaim;
            }
            var defaultAllowedRegisteredDevices = _configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.User)}:Devices").GetValue<int?>(nameof(ExtendedUserStore.DefaultAllowedRegisteredDevices)) ??
                                                  _configuration.GetSection($"{nameof(UserOptions)}:Devices").GetValue<int?>(nameof(ExtendedUserStore.DefaultAllowedRegisteredDevices));
            var maxDevicesCount = userMaxDevicesCount ?? defaultAllowedRegisteredDevices ?? int.MaxValue;
            var user = device.User ??= await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == device.UserId);
            var numberOfUserDevices = await _dbContext.UserDevices.CountAsync(x => x.UserId == user.Id);
            if (maxDevicesCount == numberOfUserDevices) {
                return IdentityResult.Failed(new IdentityError {
                    Code = "MaxNumberOfDevices",
                    Description = "You have reached the maximum number of registered devices."
                });
            }
            _dbContext.UserDevices.Add(device);
            await _dbContext.SaveChangesAsync();
            var @event = new DeviceCreatedEvent(DeviceInfo.FromUserDevice(device), SingleUserInfo.FromUser(user));
            await _eventService.Publish(@event);
            return IdentityResult.Success;
        }

        /// <inheritdoc />
        public async Task UpdateDevicePassword(UserDevice device, string passwordHash) {
            GuardDevice(device);
            device.Password = passwordHash;
            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task UpdateDevicePublicKey(UserDevice device, string publicKey) {
            GuardDevice(device);
            device.PublicKey = publicKey;
            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task UpdateLastSignInDate(UserDevice device) {
            GuardDevice(device);
            device.LastSignInDate = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        private static void GuardDevice(UserDevice device) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
            }
        }
    }
}

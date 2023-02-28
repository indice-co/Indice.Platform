using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Stores;

/// <summary>An implementation of <see cref="IUserDeviceStore"/> that stores user devices in a relational database, using Entity Framework Core.</summary>
public class UserDeviceStoreEntityFrameworkCore : IUserDeviceStore
{
    private readonly ExtendedIdentityDbContext<DbUser, DbRole> _dbContext;
    private readonly IPlatformEventService _eventService;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="UserDeviceStoreEntityFrameworkCore"/>.</summary>
    /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
    /// <param name="eventService">Models the event mechanism used to raise events inside the platform.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public UserDeviceStoreEntityFrameworkCore(
        ExtendedIdentityDbContext<DbUser, DbRole> dbContext,
        IPlatformEventService eventService,
        IConfiguration configuration
    ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<DbUserDevice> GetById(Guid id) {
        var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.Id == id);
        return device;
    }

    /// <inheritdoc />
    public async Task<DbUserDevice> GetByDeviceId(string deviceId) {
        var device = await _dbContext.UserDevices.SingleOrDefaultAsync(x => x.DeviceId == deviceId);
        return device;
    }

    /// <inheritdoc />
    public async Task UpdatePassword(DbUserDevice device, string passwordHash) {
        GuardDevice(device);
        device.Password = passwordHash;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdatePublicKey(DbUserDevice device, string publicKey) {
        GuardDevice(device);
        device.PublicKey = publicKey;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateLastSignInDate(DbUserDevice device) {
        GuardDevice(device);
        device.LastSignInDate = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private static void GuardDevice(DbUserDevice device) {
        if (device == null) {
            throw new ArgumentNullException(nameof(device), $"Parameter {nameof(device)} cannot be null.");
        }
    }
}

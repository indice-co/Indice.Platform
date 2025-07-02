#if NET9_0_OR_GREATER
using Duende.IdentityServer.EntityFramework;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.TokenCleanup;
/// <inheritdoc/>
public class ExtendedTokenCleanupService : TokenCleanupService
{
    private readonly OperationalStoreOptions _options;
    private readonly IPersistedGrantDbContext _persistedGrantDbContext;
    private readonly ILogger<ExtendedTokenCleanupService> _logger;

    /// <summary>
    /// Constructor for TokenCleanupService.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="persistedGrantDbContext"></param>
    /// <param name="operationalStoreNotification"></param>
    /// <param name="logger"></param>
    public ExtendedTokenCleanupService(
        OperationalStoreOptions options,
        IPersistedGrantDbContext persistedGrantDbContext,
        ILogger<ExtendedTokenCleanupService> logger,
        IOperationalStoreNotification operationalStoreNotification = null) :
        base(options, persistedGrantDbContext, logger, operationalStoreNotification) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.TokenCleanupBatchSize < 1) {
            throw new ArgumentException("Token cleanup batch size interval must be at least 1");
        }

        _persistedGrantDbContext = persistedGrantDbContext ?? throw new ArgumentNullException(nameof(persistedGrantDbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override async Task RemoveExpiredPersistedGrantsAsync(CancellationToken cancellationToken = default) {
        var found = int.MaxValue;
        while (found >= _options.TokenCleanupBatchSize) {
            found = await _persistedGrantDbContext.PersistedGrants
                .Where(x => x.Expiration < DateTime.UtcNow)
                .Take(_options.TokenCleanupBatchSize)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Removing {GrantCount} grants", found);
        }
    }

    /// <inheritdoc/>
    protected override async Task RemoveConsumedPersistedGrantsAsync(CancellationToken cancellationToken = default) {
        var found = int.MaxValue;

        var delay = TimeSpan.FromSeconds(_options.ConsumedTokenCleanupDelay);
        var consumedTimeThreshold = DateTime.UtcNow.Subtract(delay);

        while (found >= _options.TokenCleanupBatchSize) {
            var query = _persistedGrantDbContext.PersistedGrants
                .Where(x => x.ConsumedTime < consumedTimeThreshold)
                .OrderBy(pg => pg.ConsumedTime);

            var consumedGrants = await query
                .Take(_options.TokenCleanupBatchSize)
                .AsNoTracking()
                .ToArrayAsync(cancellationToken);

            found = consumedGrants.Length;

            if (found > 0) {
                _logger.LogInformation("Removing {GrantCount} consumed grants", found);

                var foundIds = consumedGrants.Select(pg => pg.Id).ToArray();

                var deleteCount = await query
                    .Where(pg =>
                        pg.ConsumedTime >= consumedGrants.First().ConsumedTime
                        && pg.ConsumedTime <= consumedGrants.Last().ConsumedTime)
                    .Where(pg => foundIds.Contains(pg.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleteCount != found) {

                    _logger.LogDebug("Tried to remove {GrantCount} consumed grants, but only {DeleteCount} " +
                        "was deleted. This indicates that another process has already removed the items.",
                        found, deleteCount);
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task RemoveDeviceCodesAsync(CancellationToken cancellationToken = default) {
        var found = int.MaxValue;

        while (found >= _options.TokenCleanupBatchSize) {
            found = await _persistedGrantDbContext.DeviceFlowCodes
                .Where(x => x.Expiration < DateTime.UtcNow)
                .OrderBy(x => x.Expiration)
                .ExecuteDeleteAsync();

            _logger.LogInformation("Removing {deviceCodeCount} device flow codes", found);
        }
    }
}
#endif // NET9_0_OR_GREATER
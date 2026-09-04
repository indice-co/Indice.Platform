using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.Guards;

/// <summary>Options for the <see cref="IActionRateLimiter"/> implementation.</summary>
public class ActionRateLimiterOptions
{
    /// <summary>The section name in configuration.</summary>
    public static readonly string Name = "ActionRateLimiter";
    /// <summary>Default max attempts within the active window.</summary>
    public const int DefaultMaxAttempts = 5;
    /// <summary>Default sliding window in hours.</summary>
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromHours(24);

    /// <summary>Maximum attempts allowed within the active window.</summary>
    public int MaxAttempts { get; set; } = DefaultMaxAttempts;
    /// <summary>Duration of the sliding window.</summary>
    public TimeSpan Window { get; set; } = DefaultWindow;
    /// <summary>Indicates whether the rate limiter is enabled.</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>Provides purpose-scoped attempt limiting operations per user.</summary>
public interface IActionRateLimiter
{
    /// <summary>Checks whether the given user and purpose have reached the configured limit.</summary>
    Task<bool> IsBlockedAsync(string userId, string purposeKey, CancellationToken cancellationToken = default);

    /// <summary>Records an attempt and returns the updated count for the active sliding window.</summary>
    Task<int> RecordAttemptAsync(string userId, string purposeKey, CancellationToken cancellationToken = default);
}

internal class NoOpActionRateLimiter : IActionRateLimiter { 
    public Task<bool> IsBlockedAsync(string userId, string purposeKey, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<int> RecordAttemptAsync(string userId, string purposeKey, CancellationToken cancellationToken = default) => Task.FromResult(0);
}

internal class ActionRateLimiter : IActionRateLimiter
{
    private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
    private readonly ActionRateLimiterOptions _options;

    public ActionRateLimiter(
        ExtendedIdentityDbContext<User, Role> dbContext,
        IOptions<ActionRateLimiterOptions> options
    ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<bool> IsBlockedAsync(string userId, string purposeKey, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purposeKey);

        var now = DateTimeOffset.UtcNow;
        var attempt = await _dbContext.UserActionAttempts
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(x => x.UserId == userId && x.PurposeKey == purposeKey, cancellationToken);

        if (attempt is null || now > attempt.ResetDate) {
            return false;
        }
        var maxAttempts = _options.MaxAttempts > 0 ? _options.MaxAttempts : ActionRateLimiterOptions.DefaultMaxAttempts;
        return attempt.Count >= maxAttempts;
    }

    public async Task<int> RecordAttemptAsync(string userId, string purposeKey, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(purposeKey);

        var window = _options.Window > TimeSpan.Zero ? _options.Window : ActionRateLimiterOptions.DefaultWindow;

        for (var i = 0; i < 2; i++) {
            var now = DateTimeOffset.UtcNow;
            var attempt = await _dbContext.UserActionAttempts
                                          .SingleOrDefaultAsync(x => x.UserId == userId && x.PurposeKey == purposeKey, cancellationToken);

            if (attempt is null) {
                attempt = new UseRateCounter {
                    UserId = userId,
                    PurposeKey = purposeKey,
                    Count = 1,
                    ResetDate = now.Add(window),
                    LastUpdate = now
                };
                _dbContext.UserActionAttempts.Add(attempt);
            } else if (now > attempt.ResetDate) {
                attempt.Count = 1;
                attempt.ResetDate = now.Add(window);
                attempt.LastUpdate = now;
            } else {
                attempt.Count++;
                attempt.ResetDate = now.Add(window);
                attempt.LastUpdate = now;
            }

            try {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return attempt.Count;
            } catch (DbUpdateConcurrencyException) when (i == 0) {
                _dbContext.ChangeTracker.Clear();
            } catch (DbUpdateException) when (i == 0) {
                _dbContext.ChangeTracker.Clear();
            }
        }

        throw new DbUpdateException($"Could not record user action attempt for '{userId}' and purpose '{purposeKey}'.");
    }
}

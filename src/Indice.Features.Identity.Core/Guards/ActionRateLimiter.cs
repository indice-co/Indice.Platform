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

/// <summary>Provides action-scoped attempt limiting operations per user.</summary>
public interface IActionRateLimiter
{
    /// <summary>Attempts to record an action and returns whether the action is allowed by the configured limit.</summary>
    /// <returns>True if the action is allowed; otherwise, false.</returns>
    Task<bool> CheckAndAdvanceAsync(string userId, string actionName, CancellationToken cancellationToken = default);
    /// <summary>Resets the action counter for the given user.</summary>
    /// <returns>True if the action counter was successfully reset; otherwise, false.</returns>
    Task<bool> ResetActionCounterAsync(string userId, CancellationToken cancellationToken = default);
}

internal class NoOpActionRateLimiter : IActionRateLimiter
{

    /// <inheritdoc/>
    public Task<bool> CheckAndAdvanceAsync(string userId, string actionName, CancellationToken cancellationToken = default) => Task.FromResult(true);
    /// <inheritdoc/>
    public Task<bool> ResetActionCounterAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
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

    /// <inheritdoc/>
    public async Task<bool> CheckAndAdvanceAsync(string userId, string actionName, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        var maxAttempts = _options.MaxAttempts > 0 ? _options.MaxAttempts : ActionRateLimiterOptions.DefaultMaxAttempts;
        var window = _options.Window > TimeSpan.Zero ? _options.Window : ActionRateLimiterOptions.DefaultWindow;
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 2; i++) {
            var attempt = await _dbContext.UserRateCounters
                                          .SingleOrDefaultAsync(x => x.UserId == userId && x.ActionName == actionName, cancellationToken);
            if (attempt is null) {
                attempt = UserRateCounter.Create(userId, actionName, now, window);
                _dbContext.UserRateCounters.Add(attempt);
            }
            attempt.Increment(now);
            if (attempt.IsMaxedOut(maxAttempts)) {
                return false;
            } else if (attempt.IsResetDue(now)) {
                attempt.Reset(now, window);
            }

            try {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            } catch (DbUpdateConcurrencyException) when (i == 0) {
                _dbContext.ChangeTracker.Clear();
            } catch (DbUpdateException) when (i == 0) {
                _dbContext.ChangeTracker.Clear();
            }
        }
        //something went wrong with the concurrency, we will not allow the action to be performed
        return false;
    }
    public async Task<bool> ResetActionCounterAsync(string userId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        try {
            await _dbContext.UserRateCounters.Where(x => x.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            return true;
        } catch (DbUpdateException) {
            return false;
        }
    }
}
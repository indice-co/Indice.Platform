using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs.EntityFrameworkCore;

/// <summary>An implementation of <see cref="ISignInLogStore"/>, using Entity Framework Core as a persistence mechanism.</summary>
internal class SignInLogStoreEntityFrameworkCore : ISignInLogStore
{
    private readonly SignInLogDbContext _dbContext;
    private readonly SignInLogOptions _signInLogOptions;

    /// <summary>Creates a new instance of <see cref="SignInLogStoreEntityFrameworkCore"/> class.</summary>
    /// <param name="dbContext">The <see cref="SignInLogDbContext"/> passing the configured options.</param>
    /// <param name="signInLogOptions">Options for configuring the IdentityServer sign in logs mechanism.</param>
    public SignInLogStoreEntityFrameworkCore(
        SignInLogDbContext dbContext,
        IOptions<SignInLogOptions> signInLogOptions
    ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _signInLogOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
    }

    /// <inheritdoc />
    public async Task<int> Cleanup(CancellationToken cancellationToken = default) {
        var query = _dbContext
            .SignInLogs
            .Where(x => EF.Functions.DateDiffDay(x.CreatedAt, DateTimeOffset.UtcNow) > _signInLogOptions.Cleanup.RetentionDays)
            .Take(_signInLogOptions.Cleanup.BatchSize);
        return await query.ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CreateAsync(SignInLogEntry logEntry, CancellationToken cancellationToken = default) =>
        CreateManyAsync(new List<SignInLogEntry> { logEntry }, cancellationToken);

    /// <inheritdoc />
    public async Task CreateManyAsync(IEnumerable<SignInLogEntry> logEntries, CancellationToken cancellationToken = default) {
        _dbContext.SignInLogs.AddRange(logEntries.Select(logEntry => logEntry.ToDbSignInLogEntry()));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions options, SignInLogEntryFilter filter, CancellationToken cancellationToken = default) {
        IQueryable<Data.DbSignInLogEntry> query = _dbContext.SignInLogs;
        if (filter is not null) {
            if (filter.From.HasValue) {
                query = query.Where(log => log.CreatedAt >= filter.From.Value);
            }
            if (filter.To.HasValue) {
                query = query.Where(log => log.CreatedAt < filter.To.Value.AddDays(1));
            }
            if (filter.Succeeded.HasValue) {
                query = query.Where(log => log.Succeeded == filter.Succeeded.Value);
            }
            if (filter.SignInType.HasValue) {
                query = query.Where(log => log.SignInType == filter.SignInType.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.Subject)) {
                query = query.Where(log => log.SubjectId == filter.Subject || log.SubjectName == filter.Subject);
            }
            if (!string.IsNullOrWhiteSpace(filter.ActionName)) {
                query = query.Where(log => log.ActionName == filter.ActionName);
            }
        }
        return await query.Select(ObjectMapping.ToSignInLogEntry).ToResultSetAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model, CancellationToken cancellationToken = default) {
        var query = _dbContext.SignInLogs.Where(x => x.Id == id);
        return await query.ExecuteUpdateAsync(updates => updates.SetProperty(x => x.Review, model.Review), cancellationToken);
    }
}

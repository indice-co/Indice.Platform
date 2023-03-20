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
    public Task<int> Cleanup(CancellationToken cancellationToken = default) => _dbContext
        .SignInLogs
        .Where(x => EF.Functions.DateDiffDay(x.CreatedAt, DateTimeOffset.UtcNow) > _signInLogOptions.Cleanup.RetentionDays)
        .OrderBy(x => x.CreatedAt)
        .Take(_signInLogOptions.Cleanup.BatchSize)
        .ExecuteDeleteAsync(cancellationToken);

    /// <inheritdoc />
    public async Task CreateAsync(SignInLogEntry logEntry) {
        _dbContext.SignInLogs.Add(logEntry.ToDbSignInLogEntry());
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions options) {
        var query = _dbContext.SignInLogs;
        return await query.Select(ObjectMapping.ToSignInLogEntry).ToResultSetAsync(options);
    }

    /// <inheritdoc />
    public Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model) => _dbContext
        .SignInLogs
        .Where(x => x.Id == id)
        .ExecuteUpdateAsync(updates => updates
            .SetProperty(x => x.Review, model.Review)
        );
}

using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.SignInLogs.EntityFrameworkCore;

/// <summary>An implementation of <see cref="ISignInLogService"/>, using Entity Framework Core as a persistence mechanism.</summary>
public class SignInLogServiceEntityFrameworkCore : ISignInLogService
{
    private readonly SignInLogDbContext _dbContext;

    /// <summary>Creates a new instance of <see cref="SignInLogServiceEntityFrameworkCore"/> class.</summary>
    /// <param name="dbContext">The <see cref="SignInLogDbContext"/> passing the configured options.</param>
    public SignInLogServiceEntityFrameworkCore(SignInLogDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

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
        .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.MarkForReview, model.MarkForReview));
}

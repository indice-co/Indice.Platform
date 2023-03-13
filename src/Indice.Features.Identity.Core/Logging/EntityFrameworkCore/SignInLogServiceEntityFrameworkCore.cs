using Indice.Features.Identity.Core.Logging.Abstractions;
using Indice.Features.Identity.Core.Logging.Models;
using Indice.Types;

namespace Indice.Features.Identity.Core.Logging.EntityFrameworkCore;

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
    public async Task<ResultSet<SignInLogEntry>> ListAsync(ListOptions<SignInLogEntryFilter> options) {
        var query = _dbContext.SignInLogs;
        if (options.Filter is not null) {
        }
        return await query.Select(ObjectMapping.ToSignInLogEntry).ToResultSetAsync(options);
    }
}

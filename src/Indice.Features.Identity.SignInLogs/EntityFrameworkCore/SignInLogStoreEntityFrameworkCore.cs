using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Indice.Globalization;
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
        var retentionDays = _signInLogOptions.Cleanup.RetentionDays;
        var query = _dbContext.SignInLogs.AsQueryable();
        switch (_dbContext.Database.ProviderName) {
            case "Npgsql.EntityFrameworkCore.PostgreSQL":
                // DateDiffDay is SQL Server only. Comparing against UTC midnight keeps the same day boundary semantics.
                var cutoff = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-retentionDays), TimeSpan.Zero);
                query = query.Where(x => x.CreatedAt < cutoff);
                break;
            case "Microsoft.EntityFrameworkCore.SqlServer":
            default:
                query = query.Where(x => EF.Functions.DateDiffDay(x.CreatedAt, DateTimeOffset.UtcNow) > retentionDays);
                break;
        }
        return await query.Take(_signInLogOptions.Cleanup.BatchSize).ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task CreateAsync(SignInLogEntry logEntry, CancellationToken cancellationToken = default) =>
        CreateManyAsync([logEntry], cancellationToken);

    /// <inheritdoc />
    public async Task CreateManyAsync(IEnumerable<SignInLogEntry> logEntries, CancellationToken cancellationToken = default) {
        _dbContext.SignInLogs.AddRange(logEntries.ToDbSignInLogEntries());
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
            if (!string.IsNullOrWhiteSpace(filter.ApplicationId)) {
                query = query.Where(log => log.ApplicationId == filter.ApplicationId);
            }
            if (!string.IsNullOrWhiteSpace(filter.SessionId)) {
                query = query.Where(log => log.SessionId == filter.SessionId);
            }
            if (filter.MarkForReview is bool markForReview) {
                query = query.Where(log => log.Review == markForReview);
            }
        }
        return await query.Select(ObjectMapping.ToSignInLogEntry).ToResultSetAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> UpdateAsync(Guid id, SignInLogEntryRequest model, CancellationToken cancellationToken = default) {
        var query = _dbContext.SignInLogs.Where(x => x.Id == id);
        return await query.ExecuteUpdateAsync(updates => updates.SetProperty(x => x.Review, model.Review), cancellationToken);
    }


    /// <inheritdoc />
    public async Task<SignInLocationSet> GetSignInLocationsAsync(SignInLogMapFilter filter, CancellationToken cancellationToken = default) {
        var rangeStart = filter.TimeFrame switch {
            SeriesTimeFrame.Last24Hours => DateTimeOffset.UtcNow.AddHours(-24),
            SeriesTimeFrame.Last7Days => DateTimeOffset.UtcNow.Date.AddDays(-7),
            SeriesTimeFrame.Last30Days => DateTimeOffset.UtcNow.Date.AddDays(-30),
            SeriesTimeFrame.Last90Days => DateTimeOffset.UtcNow.Date.AddDays(-90),
            SeriesTimeFrame.Last12Months => DateTimeOffset.UtcNow.Date.AddMonths(-12),
            _ => DateTimeOffset.UtcNow.Date.AddDays(-7)
        };

        var query = _dbContext.SignInLogs.Where(x => x.CreatedAt >= rangeStart && x.CountryIsoCode != null && x.Coordinates != null)
                             .GroupBy(x => new { CountryIsoCode = x.CountryIsoCode!, DisplayName = x.Location!, Lat = x.Coordinates!.Y, Lon = x.Coordinates!.X })
                             .Select(group => new {
                                 group.Key.CountryIsoCode,
                                 group.Key.DisplayName,
                                 group.Key.Lat,
                                 group.Key.Lon,
                                 Count = group.Count()
                             })
                             .OrderByDescending(x => x.Count)
                             .Select(x => new SignInLogLocation(x.CountryIsoCode,
                                 x.DisplayName,
                                 new GeoPoint(x.Lat, x.Lon),
                                 x.Count));
        var items = await query.ToListAsync(cancellationToken);
        var set = new SignInLocationSet(items, items.Count);
        set.CountryLegend = set.Items.GroupBy(x => x.CountryCode)
                                     .Select(x => 
                                            new SignInLogLocation(
                                                x.Key, 
                                                CountryInfo.GetCountryByNameOrCode(x.Key).Name, 
                                                x.MaxBy(location => location.Count)!.Location, 
                                                x.Sum(location => location.Count))
                                            )
                                     .OrderByDescending(x => x.Count)
                                     .ToList();
        return set;
    }

    /// <inheritdoc />
    public async Task<ResultSet<SignInDailyCount>> GetSignInSeriesAsync(SignInSeriesFilter filter, CancellationToken cancellationToken = default) {
        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;
        var days = filter.TimeFrame switch {
            SeriesTimeFrame.Last24Hours => 2,
            SeriesTimeFrame.Last7Days => 7,
            SeriesTimeFrame.Last90Days => 90,
            SeriesTimeFrame.Last12Months => 365,
            _ => 30
        };
        var rangeStart = new DateTimeOffset(today.AddDays(1 - days), TimeSpan.Zero);
        var counts = await _dbContext.SignInLogs
            .Where(x => x.CreatedAt >= rangeStart)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(group => new {
                Date = group.Key,
                Succeeded = group.Count(x => x.Succeeded),
                Failed = group.Count(x => !x.Succeeded),
                UniqueUsers = group.Where(x => x.SubjectId != null).Select(x => x.SubjectId).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.Date, cancellationToken);
        var items = Enumerable.Range(0, days)
            .Select(offset => rangeStart.UtcDateTime.Date.AddDays(offset))
            .Select(date => counts.TryGetValue(date, out var count)
                ? new SignInDailyCount(date, count.Succeeded, count.Failed, count.UniqueUsers)
                : new SignInDailyCount(date, 0, 0, 0))
            .ToList();
        return new ResultSet<SignInDailyCount>(items, items.Count);
    }
}

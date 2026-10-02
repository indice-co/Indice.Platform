using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <inheritdoc/>
public class MessageStatsVerificationService : IMessageStatsVerificationService
{
    private readonly AnalyticsOptions _options;
    private CampaignsDbContext DbContext { get; }
    private readonly MessageStatsWriter _writer;
    private readonly ILogger<MessageStatsVerificationService> _logger;

    /// <summary>
    /// Constructs the service.
    /// </summary>
    /// <param name="options">Configuration for the analytics feature.</param>
    /// <param name="dbContext">Database context for accessing the message events.</param>
    /// <param name="writer">The writer of the send statistics.</param>
    /// <param name="logger">Logger for logging events.</param>
    public MessageStatsVerificationService(IOptions<AnalyticsOptions> options, CampaignsDbContext dbContext, MessageStatsWriter writer, ILogger<MessageStatsVerificationService> logger) {
        _options = options.Value;
        DbContext = dbContext;
        _writer = writer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task VerifyAsync(CancellationToken cancellationToken = default) => VerifyAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

    /// <summary>Verifies the complete days before the given day.</summary>
    /// <param name="today">The running UTC day. It is not verified, because its latest sends may still wait in memory to be added.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    internal async Task VerifyAsync(DateOnly today, CancellationToken cancellationToken) {
        if (!_options.Enabled || !_options.Stats.Enabled) {
            return;
        }
        for (var daysAgo = _options.Stats.VerifyLookbackDays; daysAgo >= 1; daysAgo--) {
            var day = today.AddDays(-daysAgo);
            // The recount runs before the lock is taken.
            var counts = await RecountAsync(day, cancellationToken);
            var drifts = await _writer.CorrectDayAsync(day, counts, cancellationToken);
            if (drifts is null) {
                _logger.LogWarning("The lock '{LockName}' was not acquired. The send statistics of {Day} were not verified.", MessageStatsWriter.LockName, day);
                continue;
            }
            foreach (var drift in drifts) {
                _logger.LogWarning("Send statistics corrected: {Granularity} {PeriodStart}, channel {Channel}, campaign type {CampaignTypeId}, from {Stored} to {Recounted}.",
                    drift.Granularity, drift.PeriodStart, drift.Channel, drift.CampaignTypeId, drift.Stored, drift.Recounted);
            }
            _logger.LogInformation("The send statistics of {Day} were verified. Rows corrected: {Corrections}.", day, drifts.Count);
        }
    }

    /// <summary>Counts the successful sends of a UTC day from the saved message events. Global campaigns are left out.</summary>
    private async Task<List<MessageStatCount>> RecountAsync(DateOnly day, CancellationToken cancellationToken) {
        var dayStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var query = from messageEvent in DbContext.MessageEvents.Where(MessageStatsEligibility.IsSuccessfulSend)
                    where messageEvent.CreatedOn >= dayStart && messageEvent.CreatedOn < dayEnd
                    join campaign in DbContext.Campaigns on messageEvent.CampaignId equals campaign.Id into campaigns
                    from campaign in campaigns.DefaultIfEmpty()
                    where campaign == null || !campaign.IsGlobal
                    group messageEvent by new { messageEvent.Channel, campaign!.TypeId } into eventGroup
                    select new { eventGroup.Key.Channel, eventGroup.Key.TypeId, Sends = eventGroup.Count() };
        var groups = await query.ToListAsync(cancellationToken);
        var typeIds = groups.Where(x => x.TypeId.HasValue).Select(x => x.TypeId!.Value).Distinct().ToList();
        var typeNames = await DbContext.MessageTypes.Where(x => typeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        return groups.Select(x => new MessageStatCount {
            Day = day,
            Channel = MessageStatsEligibility.ToChannel(x.Channel),
            // A campaign that has no type, or that cannot be found, is counted as unclassified.
            CampaignTypeId = x.TypeId ?? Guid.Empty,
            CampaignTypeName = x.TypeId.HasValue ? typeNames.GetValueOrDefault(x.TypeId.Value) : null,
            Count = x.Sends
        }).ToList();
    }
}

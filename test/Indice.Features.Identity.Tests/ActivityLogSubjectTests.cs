using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Enrichers;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.Server;
using Xunit;

namespace Indice.Features.Identity.Tests;

/// <summary>
/// Covers the activity-log subject behaviour: the converter populating the subject from the event
/// context, and the aggregator running filters relative to enrichment (pre vs post).
/// </summary>
public class ActivityLogSubjectTests
{
    private static UserEventContext CreateUserContext(string id, string userName) =>
        UserEventContext.InitializeFromUser(new User { Id = id, UserName = userName });

    [Fact]
    public void Converter_Sets_Subject_From_Event_Context_For_AccountLocked() {
        var converter = new IdentityEventsActivityLogConverter();
        var user = CreateUserContext("user-123", "john.doe");

        var entry = converter.Convert(new AccountLockedEvent(user));

        Assert.NotNull(entry);
        Assert.Equal("user-123", entry!.SubjectId);
        Assert.Equal("john.doe", entry.SubjectName);
    }

    [Fact]
    public void Converter_Leaves_Subject_Null_For_Admin_Initiated_Event() {
        var converter = new IdentityEventsActivityLogConverter();
        var user = CreateUserContext("user-123", "john.doe");

        // UserDeletedEvent is admin-initiated; the actor must come from the authenticated user (enricher), not the event.
        var entry = converter.Convert(new UserDeletedEvent(user));

        Assert.NotNull(entry);
        Assert.Null(entry!.SubjectId);
        Assert.Null(entry.SubjectName);
    }

    [Fact]
    public async Task Aggregator_Runs_PostEnrichment_Filter_After_Enrichers() {
        // A synchronous enricher fills the subject; a post-enrichment filter discards when the subject is empty.
        // Because the enricher now runs before the post-filter, the entry must survive.
        var enricher = new FakeEnricher(e => e.SubjectId = "resolved-subject", ActivityLogEnricherRunType.Synchronous);
        var filter = new FakeFilter(e => string.IsNullOrWhiteSpace(e.SubjectId), ActivityLogFilterPhase.PostEnrichment);
        var aggregator = new ActivityLogEntryEnricherAggregator([enricher], [filter]);

        var result = await aggregator.EnrichAsync(new ActivityLogEntry(), ActivityLogEnricherRunType.Synchronous);

        Assert.True(enricher.WasRun);
        Assert.True(result.Succeeded);
        Assert.False(result.IsDiscarded);
    }

    [Fact]
    public async Task Aggregator_Runs_PreEnrichment_Filter_Before_Enrichers() {
        // A pre-enrichment filter discards on data already on the entry; the enricher must not run.
        var enricher = new FakeEnricher(_ => { }, ActivityLogEnricherRunType.Synchronous);
        var filter = new FakeFilter(e => e.Category == "drop", ActivityLogFilterPhase.PreEnrichment);
        var aggregator = new ActivityLogEntryEnricherAggregator([enricher], [filter]);

        var result = await aggregator.EnrichAsync(new ActivityLogEntry { Category = "drop" }, ActivityLogEnricherRunType.Synchronous);

        Assert.False(enricher.WasRun);
        Assert.True(result.IsDiscarded);
        Assert.False(result.Succeeded);
    }

    private sealed class FakeEnricher(Action<ActivityLogEntry> enrich, ActivityLogEnricherRunType runType) : IActivityLogEntryEnricher
    {
        public bool WasRun { get; private set; }
        public int Order => 1;
        public ActivityLogEnricherRunType RunType => runType;
        public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
            WasRun = true;
            enrich(logEntry);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeFilter(Func<ActivityLogEntry, bool> discard, ActivityLogFilterPhase phase) : IActivityLogEntryFilter
    {
        public ActivityLogFilterPhase Phase => phase;
        public Task<bool> Discard(ActivityLogEntry logEntry) => Task.FromResult(discard(logEntry));
    }
}

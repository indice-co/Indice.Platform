using Indice.Features.Agents.Core.Services.Risk;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// Step 3 of the risk investigation: runs the evidence chain of playbook P-02 (disputed transaction evidence pack) from the anchor
/// event, in a fixed order, through the risk MCP server. No model is involved: the same transaction always produces the same queries,
/// which is what a report for a court needs. A query that fails marks its section incomplete in the report and the chain goes on;
/// only a broken connection to the server stops it.
/// </summary>
[SendsMessage(typeof(RiskCase))]
public sealed class EvidenceCollector : Executor<RiskCase, RiskCase>
{
    private static readonly TimeSpan SessionLoginLookback = TimeSpan.FromDays(1);
    private static readonly TimeSpan SessionLoginLookahead = TimeSpan.FromHours(1);
    private static readonly TimeSpan NotificationLookback = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan NotificationLookahead = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LinkedByTimeWindow = TimeSpan.FromMinutes(30);

    private readonly IRiskQueryClient _queries;
    private readonly RiskInvestigationOptions _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<EvidenceCollector> _logger;

    /// <summary>Creates a new <see cref="EvidenceCollector"/>.</summary>
    public EvidenceCollector(IRiskQueryClient queries, IOptions<RiskInvestigationOptions> options, ILogger<EvidenceCollector> logger) : base(nameof(EvidenceCollector)) {
        _queries = queries;
        _options = options.Value;
        _timeZone = _options.ResolveTimeZone();
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask<RiskCase> HandleAsync(RiskCase riskCase, IWorkflowContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(riskCase);
        var anchor = riskCase.AnchorEvent ?? throw new InvalidOperationException("The risk case has no anchor event.");
        var subjectId = anchor.Text("SubjectId") ?? throw new RiskQueryException("The anchor event has no subject.");
        var sessionId = anchor.Text("SessionId");
        var anchorTime = anchor.Time("CreatedAt") ?? throw new RiskQueryException("The anchor event has no time.");
        var anchorName = anchor.Text("Name") ?? string.Empty;
        var conversation = await context.GetConversationStateAsync(cancellationToken);

        await using var session = await _queries.OpenSessionAsync(conversation.ConversationId, cancellationToken);
        Task<RiskQueryResult> Run(string queryId, Dictionary<string, object?> parameters) => RunAsync(session, riskCase, queryId, parameters, cancellationToken);

        // 1. The step chain of the transaction in its session, with the score of each step.
        RiskQueryResult chain;
        if (sessionId is not null) {
            chain = await Run("Q-RISK-06", new() { ["SessionId"] = sessionId, ["BaseName"] = anchorName });
        } else {
            // Rare. Say that the link is by time and not by session, as the knowledge base requires.
            riskCase.Notes.Add($"The anchor event has no session id, so the surrounding events are linked by time (±{LinkedByTimeWindow.TotalMinutes:0} minutes around the anchor), not by session.");
            chain = await Run("Q-RISK-05", new() { ["SubjectId"] = subjectId, ["From"] = anchorTime - LinkedByTimeWindow, ["To"] = anchorTime + LinkedByTimeWindow });
        }
        var stepRows = chain.Succeeded ? chain.Rows : [];
        var firstStepTime = stepRows.Select(row => row.Time("CreatedAt")).Where(time => time is not null).DefaultIfEmpty(anchorTime).Min() ?? anchorTime;

        // 2. The score breakdown of each step that has a result, then the attempts of the day that never completed.
        foreach (var eventId in stepRows.Select(row => row.Text("Id")).Where(id => Guid.TryParse(id, out _)).Distinct().Take(_options.MaxStepEvents)) {
            await Run("Q-RISK-02", new() { ["EventId"] = Guid.Parse(eventId!) });
        }
        var dayStart = LocalMidnightUtc(anchorTime);
        await Run("Q-RISK-03", new() { ["SubjectId"] = subjectId, ["From"] = dayStart, ["To"] = dayStart.AddDays(1) });

        // 3. How the session was authenticated, and the devices registered to the customer to compare it with.
        if (sessionId is not null) {
            await Run("Q-SIGN-02", new() { ["SessionId"] = sessionId, ["From"] = anchorTime - SessionLoginLookback, ["To"] = anchorTime + SessionLoginLookahead });
        } else {
            riskCase.Notes.Add("The anchor event has no session id, so the login of the session could not be looked up.");
        }
        await Run("Q-ID-04", new() { ["SubjectId"] = subjectId });

        // 4. One-time passwords and notifications sent to the customer around the transaction, and card verification for card payments.
        var notificationWindow = new Dictionary<string, object?> { ["SubjectId"] = subjectId, ["From"] = firstStepTime - NotificationLookback, ["To"] = anchorTime + NotificationLookahead };
        await Run("Q-RISK-08", new(notificationWindow));
        if (anchorName.Contains("Card", StringComparison.OrdinalIgnoreCase)) {
            await Run("Q-RISK-09", new(notificationWindow));
        }

        // 5. Credential and profile changes before the transaction.
        await Run("Q-RISK-10", new() { ["SubjectId"] = subjectId, ["AnchorTime"] = anchorTime, ["DaysBefore"] = _options.PriorChangesDays });
        await Run("Q-ACT-01", new() { ["SubjectId"] = subjectId, ["From"] = anchorTime.AddDays(-_options.PriorChangesDays), ["To"] = anchorTime });

        // 6. What the customer usually does: where they connect from and with what.
        var usual = new Dictionary<string, object?> { ["SubjectId"] = subjectId, ["From"] = anchorTime.AddDays(-_options.UsualPatternDays), ["To"] = anchorTime };
        await Run("Q-RISK-19", new(usual));
        await Run("Q-SIGN-06", new(usual));

        // 7. Who the customer is.
        await Run("Q-ID-01", new() { ["SubjectId"] = subjectId });
        await Run("Q-ID-03", new() { ["SubjectId"] = subjectId });

        // 8. What the data covers, so the report can say what it cannot show.
        await Run("Q-RISK-17", new() { ["From"] = anchorTime.AddDays(-_options.UsualPatternDays), ["To"] = anchorTime.AddDays(1) });
        await Run("Q-SIGN-07", new());
        await Run("Q-ACT-05", new());

        _logger.LogInformation("Risk investigation: evidence for {Tun} collected in {Queries} queries ({Failed} failed).", riskCase.Request.Tun, riskCase.Results.Count, riskCase.Results.Count(r => !r.Succeeded));
        return riskCase;
    }

    private DateTimeOffset LocalMidnightUtc(DateTimeOffset instant) {
        var local = TimeZoneInfo.ConvertTime(instant, _timeZone).Date;
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), _timeZone), TimeSpan.Zero);
    }

    private static async Task<RiskQueryResult> RunAsync(IRiskQuerySession session, RiskCase riskCase, string queryId, Dictionary<string, object?> parameters, CancellationToken cancellationToken) {
        var result = await session.RunAsync(queryId, parameters, cancellationToken);
        riskCase.Results.Add(result);
        return result;
    }
}

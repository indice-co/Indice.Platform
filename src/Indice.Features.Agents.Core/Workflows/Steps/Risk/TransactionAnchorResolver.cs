using System.Globalization;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Services.Risk;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// Step 2 of the risk investigation: finds the risk event that carries the transaction number (<c>Q-RISK-07</c>, in a window that
/// widens once) and loads it (<c>Q-RISK-01</c>). That event is the anchor everything else hangs from. When nothing is found it checks
/// whether the Risk API was recording at all (<c>Q-RISK-17</c>), so the user hears the difference between "not collecting" and "not
/// found", and asks for another try.
/// </summary>
[SendsMessage(typeof(RiskCase))]
[SendsMessage(typeof(UserInputRequestPort.UserInputRequest))]
public sealed class TransactionAnchorResolver : Executor<RiskTraceRequest>
{
    private static readonly string[] StepSuffixes = ["SecondFactorSent", "RequiresThirdFactor", "ThirdFactorSent", "RequiresApproval"];

    private readonly IRiskQueryClient _queries;
    private readonly RiskInvestigationOptions _options;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<TransactionAnchorResolver> _logger;

    /// <summary>Creates a new <see cref="TransactionAnchorResolver"/>.</summary>
    public TransactionAnchorResolver(IRiskQueryClient queries, IOptions<RiskInvestigationOptions> options, ILogger<TransactionAnchorResolver> logger) : base(nameof(TransactionAnchorResolver)) {
        _queries = queries;
        _options = options.Value;
        _timeZone = _options.ResolveTimeZone();
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(RiskTraceRequest request, IWorkflowContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        var conversation = await context.GetConversationStateAsync(cancellationToken);
        var messages = RiskMessages.For(request.Language);
        var riskCase = new RiskCase { Request = request };
        var date = DateOnly.ParseExact(request.TransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        await using var session = await _queries.OpenSessionAsync(conversation.ConversationId, cancellationToken);

        List<Dictionary<string, System.Text.Json.JsonElement>>? matches = null;
        (DateTimeOffset From, DateTimeOffset To) widest = default;
        foreach (var days in _options.AnchorSearchWindowDays) {
            var from = date.AddDays(-((days - 1) / 2));
            var window = (From: LocalMidnightUtc(from), To: LocalMidnightUtc(from.AddDays(days)));
            widest = window;
            var result = await Run(session, riskCase, "Q-RISK-07", new Dictionary<string, object?> { ["Tun"] = request.Tun, ["From"] = window.From, ["To"] = window.To }, cancellationToken);
            if (!result.Succeeded) {
                throw new RiskQueryException($"Looking up transaction {request.Tun} failed: {result.Error}");
            }
            if (result.Rows.Count > 0) {
                matches = result.Rows;
                break;
            }
        }

        if (matches is null) {
            await AskAgainAsync(context, session, riskCase, request, messages, widest, cancellationToken);
            return;
        }

        var anchor = ChooseAnchor(matches, riskCase.Notes, evidenceNumber: riskCase.Results.Count);
        var anchorId = anchor.Text("Id") ?? throw new RiskQueryException($"The event found for {request.Tun} has no id.");
        var loaded = await Run(session, riskCase, "Q-RISK-01", new Dictionary<string, object?> { ["EventId"] = Guid.Parse(anchorId) }, cancellationToken);
        if (!loaded.Succeeded || loaded.Rows.Count != 1) {
            throw new RiskQueryException($"The event {anchorId} found for {request.Tun} could not be loaded: {loaded.Error ?? "no row returned"}.");
        }
        _logger.LogInformation("Risk investigation: TUN {Tun} resolved to event {EventId}.", request.Tun, anchorId);
        await context.SendMessageAsync(riskCase, cancellationToken);
    }

    // The first window can miss a transaction the user dated loosely. Say what the coverage check shows, then ask for another try.
    private async Task AskAgainAsync(IWorkflowContext context, IRiskQuerySession session, RiskCase riskCase, RiskTraceRequest request, RiskMessages messages, (DateTimeOffset From, DateTimeOffset To) widest, CancellationToken cancellationToken) {
        var coverage = await Run(session, riskCase, "Q-RISK-17", new Dictionary<string, object?> { ["From"] = widest.From, ["To"] = widest.To }, cancellationToken);
        var coverageNote = !coverage.Succeeded ? messages.CoverageUnknown : coverage.Rows.Count == 0 ? messages.CoverageNone : messages.CoverageSome;

        var state = await context.GetRiskStateAsync(cancellationToken);
        if (state.Attempts >= _options.MaxInputAttempts) {
            await context.Say(Id, $"{messages.NotFound(request.Tun, FormatDate(request.TransactionDate), coverageNote)}\n\n{messages.TooManyAttempts}", cancellationToken);
            return;
        }
        // Forget the date but keep the number: a transaction not found around a date is usually a wrong date.
        await context.SetRiskStateAsync(state with { TransactionDate = null, Attempts = state.Attempts + 1 }, cancellationToken);
        var question = messages.NotFound(request.Tun, FormatDate(request.TransactionDate), coverageNote);
        await context.Say(Id, question, cancellationToken);
        await context.SendMessageAsync(new UserInputRequestPort.UserInputRequest(question), cancellationToken);
    }

    /// <summary>
    /// Picks the event to anchor on. Only the final event of a transaction carries the TUN, so one match is the normal case. If several
    /// do, the earliest is used and the report says so: the reader must know the data was ambiguous.
    /// </summary>
    internal static Dictionary<string, System.Text.Json.JsonElement> ChooseAnchor(IReadOnlyList<Dictionary<string, System.Text.Json.JsonElement>> matches, List<string> notes, int evidenceNumber) {
        var ordered = matches
            .OrderBy(row => StepSuffixes.Any(suffix => (row.Text("Name") ?? string.Empty).EndsWith(suffix, StringComparison.Ordinal)) ? 1 : 0)
            .ThenBy(row => row.Time("CreatedAt") ?? DateTimeOffset.MaxValue)
            .ToList();
        if (matches.Count > 1) {
            var subjects = matches.Select(row => row.Text("SubjectId")).Distinct().Count();
            notes.Add($"{matches.Count} risk events carry this transaction number{(subjects > 1 ? $", belonging to {subjects} different subjects" : string.Empty)}. The earliest, {ordered[0].Text("Id")}, was used as the anchor; the others are listed in query E-{evidenceNumber}.");
        }
        return ordered[0];
    }

    private DateTimeOffset LocalMidnightUtc(DateOnly date) =>
        new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), _timeZone), TimeSpan.Zero);

    private static string FormatDate(string isoDate) =>
        DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : isoDate;

    private static async Task<RiskQueryResult> Run(IRiskQuerySession session, RiskCase riskCase, string queryId, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken) {
        var result = await session.RunAsync(queryId, parameters, cancellationToken);
        riskCase.Results.Add(result);
        return result;
    }
}

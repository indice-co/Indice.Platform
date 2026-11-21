using System.Globalization;
using System.Text.RegularExpressions;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Security;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// Step 1 of the risk investigation: reads the request, finds the transaction number and the date, and decides which report is
/// wanted. The model only <em>extracts</em>; everything it returns is checked here before it can reach a database: the number
/// must match the configured shape and appear in what the user wrote, the date must be real and not in the future. Missing details
/// are asked for, a few times at most. The step also re-checks that the caller may use the agent, whatever the router decided.
/// </summary>
[SendsMessage(typeof(UserInputRequestPort.UserInputRequest))]
[SendsMessage(typeof(RiskTraceRequest))]
[SendsMessage(typeof(RiskUnsupportedRequest))]
public sealed class RiskRequestAnalyzer : Executor<ChatMessage>
{
    /// <summary>The only report type produced so far: the disputed-transaction evidence pack.</summary>
    public const string SupportedPlaybook = "P-02";

    private const string DateFormat = "yyyy-MM-dd";
    private const int KnowledgeExcerpts = 4;
    private const int ExcerptLength = 500;

    private static readonly (string Id, string Name)[] Playbooks = [
        ("P-01", "Explain one risk event (why it scored high or medium)"),
        ("P-02", "Disputed transaction evidence pack for a court, a prosecutor or a customer dispute"),
        ("P-03", "Account-takeover investigation"),
        ("P-04", "Session reconstruction and customer timeline"),
        ("P-05", "Device investigation"),
        ("P-06", "IP address investigation"),
        ("P-07", "Periodic risk summary for an audit or a regulator"),
        ("P-08", "Authority data request about one customer")
    ];

    private readonly IChatClient _chatClient;
    private readonly ModelsOptions _models;
    private readonly RiskInvestigationOptions _options;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly AgentsClaimsPrincipalSelector _claimsPrincipalSelector;
    private readonly AgentInfoRegistry _registry;
    private readonly IDocumentsService _documents;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RiskRequestAnalyzer> _logger;
    private readonly Regex _tunPattern;
    private readonly TimeZoneInfo _timeZone;

    /// <summary>Creates a new <see cref="RiskRequestAnalyzer"/>.</summary>
    public RiskRequestAnalyzer(
        [FromKeyedServices(nameof(AgentsModelRole.Reasoning))] IChatClient chatClient,
        IOptions<ModelsOptions> models,
        IOptions<RiskInvestigationOptions> options,
        IPromptTemplateRenderer prompts,
        AgentsClaimsPrincipalSelector claimsPrincipalSelector,
        AgentInfoRegistry registry,
        IDocumentsService documents,
        IEmbeddingGenerator<string, Embedding<float>> embeddings,
        TimeProvider timeProvider,
        ILogger<RiskRequestAnalyzer> logger) : base(nameof(RiskRequestAnalyzer)) {
        _chatClient = chatClient;
        _models = models.Value;
        _options = options.Value;
        _prompts = prompts;
        _claimsPrincipalSelector = claimsPrincipalSelector;
        _registry = registry;
        _documents = documents;
        _embeddings = embeddings;
        _timeProvider = timeProvider;
        _logger = logger;
        _tunPattern = new Regex(_options.TunPattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        _timeZone = _options.ResolveTimeZone();
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        // The first pass stores the conversation; a pass after a question to the user keeps the original message.
        if (await context.TryGetConversationStateAsync(cancellationToken) is null) {
            var conversationId = message.AdditionalProperties?[nameof(ConversationState.ConversationId)]?.ToString()
                                 ?? throw new InvalidOperationException("The chat message carries no conversation id.");
            await context.SetConversationStateAsync(new ConversationState(message, conversationId), cancellationToken);
        }

        var state = await context.GetRiskStateAsync(cancellationToken);
        var user = _claimsPrincipalSelector();
        if (_registry.Find(AgentsConstants.AgentNames.Risk, user) is null) {
            _logger.LogWarning("Risk investigation refused: caller {UserId} may not use the agent.", user?.FindSubjectId());
            await context.Say(Id, RiskMessages.For(state.Language, _options.DefaultLanguage).Refused, cancellationToken);
            return;
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone).DateTime);
        var text = message.Text?.Trim() ?? string.Empty;
        var analysis = text.Length == 0
            ? new RiskRequestAnalysis(null, null, null, null, null)
            : await AnalyzeAsync(text, state, today, cancellationToken);

        var messages = RiskMessages.For(analysis.Language, state.Language ?? _options.DefaultLanguage);
        var playbook = analysis.PlaybookId?.Trim().ToUpperInvariant();
        _logger.LogInformation("Risk request from {UserId}: playbook {Playbook}, language {Language}.", user?.FindSubjectId(), playbook ?? "(none)", messages.Language);

        if (!string.IsNullOrEmpty(playbook) && playbook != SupportedPlaybook && Playbooks.Any(p => p.Id == playbook)) {
            await context.SendMessageAsync(new RiskUnsupportedRequest(playbook, messages.Language), cancellationToken);
            return;
        }

        var tun = AcceptTun(analysis.Tun, text, out var rejectedTun) ?? state.Tun;
        var date = AcceptDate(analysis.TransactionDate, today) ?? state.TransactionDate;
        var next = state with {
            Tun = tun,
            TransactionDate = date,
            Language = messages.Language,
            Purpose = string.IsNullOrWhiteSpace(analysis.Purpose) ? state.Purpose : analysis.Purpose.Trim()
        };

        if (tun is null || date is null) {
            if (state.Attempts >= _options.MaxInputAttempts) {
                await context.SetRiskStateAsync(next, cancellationToken);
                await context.Say(Id, messages.TooManyAttempts, cancellationToken);
                return;
            }
            var question = Ask(messages, tun is null, date is null, rejectedTun);
            await context.SetRiskStateAsync(next with { Attempts = state.Attempts + 1 }, cancellationToken);
            await context.Say(Id, question, cancellationToken);
            await context.SendMessageAsync(new UserInputRequestPort.UserInputRequest(question), cancellationToken);
            return;
        }

        await context.SetRiskStateAsync(next, cancellationToken);
        await context.SendMessageAsync(new RiskTraceRequest(tun, date, messages.Language, next.Purpose), cancellationToken);
    }

    /// <summary>
    /// The transaction number the user gave, or <c>null</c> when there is none we can trust: the model returned nothing, the value
    /// does not have the configured shape, or it is not literally in what the user wrote (so the model made it up).
    /// </summary>
    /// <param name="candidate">What the model extracted.</param>
    /// <param name="userText">What the user wrote.</param>
    /// <param name="rejected">The value that was refused, so the user can be told which one.</param>
    internal string? AcceptTun(string? candidate, string userText, out string? rejected) {
        rejected = null;
        var tun = candidate?.Trim();
        if (string.IsNullOrEmpty(tun)) {
            return null;
        }
        if (!_tunPattern.IsMatch(tun) || !userText.Contains(tun, StringComparison.OrdinalIgnoreCase)) {
            rejected = tun.Length > 64 ? tun[..64] : tun;
            return null;
        }
        return tun;
    }

    /// <summary>The date the model extracted as <c>yyyy-MM-dd</c>, or <c>null</c> when it is missing, malformed, or in the future.</summary>
    internal static string? AcceptDate(string? candidate, DateOnly today) =>
        DateOnly.TryParseExact(candidate?.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date <= today
            ? date.ToString(DateFormat, CultureInfo.InvariantCulture)
            : null;

    private static string Ask(RiskMessages messages, bool missingTun, bool missingDate, string? rejectedTun) {
        var question = (missingTun, missingDate) switch {
            (true, true) => messages.AskTunAndDate,
            (true, false) => messages.AskTun,
            _ => messages.AskDate
        };
        return rejectedTun is null ? question : $"{messages.TunInvalid(rejectedTun)} {question}";
    }

    private async Task<RiskRequestAnalysis> AnalyzeAsync(string text, RiskInvestigationState state, DateOnly today, CancellationToken cancellationToken) {
        var collected = new Dictionary<string, string>();
        if (state.Tun is not null) collected["Tun"] = state.Tun;
        if (state.TransactionDate is not null) collected["TransactionDate"] = state.TransactionDate;
        if (state.Language is not null) collected["Language"] = state.Language;
        if (state.Purpose is not null) collected["Purpose"] = state.Purpose;

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.RiskRequestAnalyzer), new {
            today = today.ToString(DateFormat, CultureInfo.InvariantCulture),
            timeZone = _options.TimeZoneId,
            playbooks = Playbooks.Select(p => new { id = p.Id, name = p.Name }),
            collected = collected.Count > 0 ? collected : null,
            excerpts = await SearchKnowledgeAsync(text, cancellationToken)
        });
        var agent = _chatClient.AsAIAgent(options: new ChatClientAgentOptions {
            ChatOptions = chatOptions,
            Name = "DexRiskRequestAnalyzer"
        });
        var response = await agent.RunAsync<RiskRequestAnalysis>(text, cancellationToken: cancellationToken);
        return response.Result;
    }

    // Excerpts only help the model recognise the report type. A knowledge base that is down must not stop an investigation.
    private async Task<IReadOnlyList<string>> SearchKnowledgeAsync(string text, CancellationToken cancellationToken) {
        try {
            var vector = await _embeddings.GenerateVectorAsync(text, cancellationToken: cancellationToken);
            var filters = new RetrievalFilters { Category = _options.KnowledgeCategory, IncludeRestricted = true };
            var chunks = await _documents.SearchAsync(vector, filters, KnowledgeExcerpts, 0.2, cancellationToken);
            return [.. chunks.Select(chunk => Truncate($"{chunk.HeadingPath}: {chunk.Content}", ExcerptLength))];
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            _logger.LogWarning(exception, "The antifraud knowledge base could not be searched; analysing the request without excerpts.");
            return [];
        }
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length] + "…";
}

using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Classifies the user's question (intent + category + language) and decides whether it is in scope.
/// On out-of-scope the workflow routes to <see cref="OutOfScopeResponder"/> via a conditional edge; otherwise
/// downstream steps receive validated <see cref="Intent"/> and <see cref="RetrievalFilters"/>.
/// </summary>
public sealed class IntentClassifier : Executor<ChatMessage, IntentOutput>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;
    private readonly SystemOneDecisionService _systemOne;
    private readonly AgentMessageLocalizer _localizer;

    private const string InScopeQuestion = "in_scope";
    private const string CategoryQuestion = "category";
    private const string LanguageQuestion = "language";

    /// <summary>Creates a new <see cref="IntentClassifier"/>.</summary>
    public IntentClassifier(
        [FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        ConversationStoreChatHistoryProvider historyProvider,
        SystemOneDecisionService systemOne, AgentMessageLocalizer localizer) : base("IntentClassifier") {
        _options = options.Value;
        _systemOne = systemOne;
        _localizer = localizer;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("IntentClassifier", new {
            categories = _options.Taxonomy.Categories,
            languages = _options.Taxonomy.Languages,
        });
        _agent = chatClient.AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                Name = "DexIntentClassifier",
                ChatHistoryProvider = historyProvider,
                // Chat completions is stateless; the echoed request ConversationId must not be treated as server-side history.
                ThrowOnChatHistoryProviderConflict = false,
            });
    }

    /// <inheritdoc/>
    public override async ValueTask<IntentOutput> HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        
        var question = message.Text;
        var conversationId = message.AdditionalProperties![nameof(ConversationState.ConversationId)]!.ToString()!;
        var conversationState = new ConversationState(message, conversationId);
        await context.SetConversationStateAsync(conversationState, cancellationToken);

        // System-one first; null means it is off, failed or unsure — the Reasoning-model agent below classifies instead.
        var systemOneOutput = await ClassifyWithSystemOneAsync(question, conversationId, context, cancellationToken);
        if (systemOneOutput is not null) {
            return systemOneOutput;
        }

        var agentSession = await _agent.CreateSessionAsync(cancellationToken);
        
        ConversationStoreChatHistoryProvider.SetSessionId(agentSession, Guid.Parse(conversationId));
        var response = await _agent.RunAsync<IntentResult>(question, agentSession, cancellationToken: cancellationToken);
        if (response.Usage is not null) {
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new UsageContent(response.Usage)])), cancellationToken);
        }
        var result = response.Result;

        //For now ommit category and language. We must first identify how to correctrly use them based on the Bussiness requirements that will come.
        //var category = _options.Taxonomy.Categories.Contains(result.Category ?? "", StringComparer.OrdinalIgnoreCase) ? result.Category : null;
        //var language = _options.Taxonomy.Languages.Contains(result.Language ?? "", StringComparer.OrdinalIgnoreCase) ? result.Language : null;
        string? category = null, language = null;
        var intent = new Intent {
            Type = string.IsNullOrWhiteSpace(result.Type) ? "question" : result.Type,
            Category = category,
            Language = language,
            IsInScope = result.IsInScope,
            OutOfScopeReason = result.OutOfScopeReason,
        };
        await context.SetIntentStateAsync(new IntentState(intent, new RetrievalFilters { Category = category, Language = language }), cancellationToken);
        return new IntentOutput (
            intent,
            new RetrievalFilters { Category = category, Language = language }
        );
    }

    private async Task<IntentOutput?> ClassifyWithSystemOneAsync(string question, string conversationId, IWorkflowContext context, CancellationToken cancellationToken) {
        if (!_systemOne.IsEnabled) {
            return null;
        }
        var decision = await _systemOne.DecideAsync(BuildSystemOneRequest(question, conversationId), cancellationToken);
        var intent = decision is null ? null : ToIntent(decision);
        if (intent is null) {
            return null;
        }
        if (decision!.Usage is not null) {
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new UsageContent(decision.Usage)])), cancellationToken);
        }
        // Retrieval filters stay unset until the business rules for category/language filtering are defined (see HandleAsync).
        var filters = new RetrievalFilters();
        await context.SetIntentStateAsync(new IntentState(intent, filters), cancellationToken);
        return new IntentOutput(intent, filters);
    }

    private SystemOneDecisionRequest BuildSystemOneRequest(string question, string conversationId) {
        // Config binding appends Dex:Taxonomy lists to the code defaults, so they can hold duplicates.
        var categories = _options.Taxonomy.Categories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var languages = _options.Taxonomy.Languages.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new SystemOneDecisionRequest {
            ConversationId = Guid.TryParse(conversationId, out var id) ? id : null,
            Message = question,
            ContextPrompt = nameof(AgentsConstants.PromptDefaults.IntentClassifierSystemOne),
            Questions = {
                [InScopeQuestion] = SystemOneQuestion.Noul("Is the latest message, read in the context of the conversation, answerable from the assistant's knowledge base?", required: true),
                [CategoryQuestion] = SystemOneQuestion.Choice("Which category does the latest message belong to?", categories.ToDictionary(category => category, category => category)),
                [LanguageQuestion] = SystemOneQuestion.Choice("Which language (ISO 639-1 code) is the latest message written in?", languages.ToDictionary(language => language, language => language)),
            }
        };
    }

    private Intent? ToIntent(SystemOneDecision decision) {
        var isInScope = decision.Get(InScopeQuestion)!.IsTrue;
        var categoryAnswer = decision.Get(CategoryQuestion);
        // The category only matters (it drives the purpose_of_agent branch) for an in-scope message; there an unsure one goes to the LLM.
        if (isInScope && categoryAnswer is not { IsConfident: true }) {
            return null;
        }
        var languageAnswer = decision.Get(LanguageQuestion);
        return new Intent {
            Type = "question",
            Category = isInScope ? categoryAnswer!.Choice : null,
            Language = languageAnswer is { IsConfident: true } ? languageAnswer.Choice : null,
            IsInScope = isInScope,
            OutOfScopeReason = isInScope ? null : _localizer.OutOfScopeReply,
        };
    }

    private sealed class IntentResult
    {
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Language { get; set; }
        public bool IsInScope { get; set; }
        public string? OutOfScopeReason { get; set; }
    }
}
/// <summary>Output payload of <c>IntentClassifier</c>.</summary>
/// <param name="Intent">The classified intent.</param>
/// <param name="Filters">Retrieval filters derived from the intent (category, language).</param>
public record IntentOutput(Intent Intent, RetrievalFilters Filters);
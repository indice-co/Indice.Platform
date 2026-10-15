using System.Text.Json;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services.DataResolver;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from the configured MCP service.
/// The MCP service key is fixed, while the model decides which discovered tool to call.
/// </summary>
internal sealed class DataRetrieverStep : Executor<ChatMessage, OperationState>
{
    private const string McpServiceKey = "cases";

    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _promptRenderer;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly CustomerDataResolverFactory _resolverFactory;
    private readonly string _model;
    private readonly ILogger<DataRetrieverStep> _logger;

    /// <summary>Creates a new <see cref="DataRetrieverStep"/>.</summary>
    public DataRetrieverStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices(McpServiceKey)] IMcpClientFactory mcpClientFactory,
        CustomerDataResolverFactory resolverFactory,
        ILogger<DataRetrieverStep> logger) : base(nameof(DataRetrieverStep)) {
        _logger = logger;
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _promptRenderer = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
        _resolverFactory = resolverFactory ?? throw new ArgumentNullException(nameof(resolverFactory));
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationState> HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(message);

        // Persist a ConversationState snapshot so downstream shared steps (e.g. OtpAgent)
        // can use existing state extension helpers.
        //await context.SetConversationStateAsync(new ConversationState(message, message.AdditionalProperties![nameof(ConversationState.ConversationId)]!.ToString()!), cancellationToken);

        var userInput = message.Text ?? string.Empty;

        if (message.AdditionalProperties?.TryGetValue(nameof(ChatTopic), out var topic) is true) {
            var chatTopic = topic switch {
                ChatTopic t => t,
                JsonElement je => je.Deserialize<ChatTopic>(JsonSerializerOptions.Web),
                _ => null
            };
            if (!string.IsNullOrEmpty(chatTopic?.ReferenceId)) {
                userInput = chatTopic.ReferenceId;
            }
        }
        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
        if (mcpTools.Count == 0) {
            _logger.LogError("No MCP tools discovered for service {McpServiceKey}.", McpServiceKey);
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }
        // Render the verification prompt using template
        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _promptRenderer.Render(nameof(AgentsConstants.PromptDefaults.DataRetriever));
        // Wrap MCP tools so the raw output is captured here and never sent back to the model.
        var capturingTools = mcpTools.Select(t => new McpCapturingToolFunction(t)).ToList();
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. capturingTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexCaseRetrieverAgent"
            });

        var prompt = $"""
            Retrieve the case information.
            Query: {userInput}
            """;

        var tokens = await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        // Use the raw MCP tool output, not the LLM re-generated JSON, which can corrupt values.
        var rawResult = capturingTools.Select(t => t.Result).LastOrDefault(r => r is not null);
        var caseNode = rawResult is null ? null : WorkflowExtensions.ExtractDataFromToolResults([new ChatMessage(ChatRole.Tool, [new FunctionResultContent(string.Empty, rawResult)])]);
        if (caseNode is null) {
            throw new InvalidOperationException($"Case retrieval agent returned no case data for query '{userInput}'.");
        }
        var caseType = caseNode["caseType"]?["code"]?.GetValue<string>();
        var caseData = caseNode["data"];
        var resolver = _resolverFactory.Create(caseType);
        var caseId = caseNode["id"]?.GetValue<string>();
        var phoneNumber = resolver.ExtractPhoneNumber(caseData);
        var email = resolver.ExtractEmail(caseData);
        var verificationValue = resolver.ExtractChallengeValue(caseData);
        var validationResult = resolver.Validate(caseData);
        if (!validationResult.Succeeded) {
            _logger.LogError("Case data validation failed for case {CaseId}: {ValidationError}", caseId, validationResult.ErrorMessage);
            throw new InvalidOperationException($"Case data validation failed: {validationResult.ErrorMessage}");
        }

        await context.SetOperatorStateAsync(new CustomerState() {
            Data = caseNode?.Deserialize<JsonElement>() ?? default,
            DataType = caseType,
            ReferenceId = caseId,
            PhoneNumber = phoneNumber,
            Email = email,
            ChallengeValue = verificationValue
        }, cancellationToken);

        return OperationState.Next(nameof(AuthenticationChallengeStep));
    }


}
/// <summary>
/// Output of the CaseDataRetriever step containing the retrieved case data and related context.
/// </summary>
/// <param name="CaseId">The unique identifier for the case.</param>
/// <param name="PhoneNumber">The phone number from case data for OTP delivery (primary channel).</param>
/// <param name="Email">The email from case data (fallback channel if phone unavailable).</param>
/// <param name="VerificationValue">The value the user must use to verify their identity.</param>
public record CaseRetrievalOutput(
    string CaseId,
    string? PhoneNumber,
    string? Email,
    string? VerificationValue);

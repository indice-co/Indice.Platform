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
public class DataRetrieverStep : Executor<ChatMessage, OperationState>
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

        var rawData = await GetCustomerRawData(message, cancellationToken);
        var caseType = rawData["caseType"]?["code"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(caseType))
            throw new InvalidOperationException("Case type not found in case data.");
        
        var dataResolver = _resolverFactory.Create(caseType);
        if (!dataResolver.TryResolve(rawData, out var customerData, out var error)) {
            _logger.LogError("Case data validation failed with errors {ValidationError}", error);
            throw new InvalidOperationException($"Case data validation failed: {error}");
        }
        await context.SetOperatorStateAsync(customerData, cancellationToken: cancellationToken);
        return OperationState.Next(nameof(AuthenticationChallengeStep));
    }

    private async Task<System.Text.Json.Nodes.JsonNode> GetCustomerRawData(ChatMessage message, CancellationToken cancellationToken) {
        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken: cancellationToken);
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

        var userInput = message.Text ?? string.Empty;
        if (message.TryGetAdditionalProperty<ChatTopic>(typeof(ChatTopic).Name, out var chatTopic) && !string.IsNullOrEmpty(chatTopic.ReferenceId)) {
            userInput = chatTopic.ReferenceId;
        }
        var prompt = $"""
            Retrieve the case information.
            Query: {userInput}
            """;
        await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        // Use the raw MCP tool output, not the LLM re-generated JSON, which can corrupt values.
        if (!capturingTools.TryGetToolResult(out var caseNode, out var error) || caseNode is null) {
            throw new InvalidOperationException($"Case retrieval agent failed: {error}");
        }
        return caseNode;
    }
}
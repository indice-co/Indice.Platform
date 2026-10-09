using System.Text.Json;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from the configured MCP service.
/// The MCP service key is fixed, while the model decides which discovered tool to call.
/// </summary>
[SendsMessage(typeof(OperationRequestPort.OperationRequest))]
[SendsMessage(typeof(ChatMessage))]
internal sealed class OperationSelectionStep : Executor<ChatMessage>
{
    private const string McpServiceKey = "cases";

    private readonly AzureOpenAIClient _openAIClient;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly ModelsOptions _models;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly string _model;
    /// <summary>Creates a new <see cref="OperationSelectionStep"/>.</summary>
    public OperationSelectionStep(
         AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices(McpServiceKey)] IMcpClientFactory mcpClientFactory,
        AgentMessageLocalizer messageLocalizer) : base(nameof(OperationSelectionStep)) {
        _messageLocalizer = messageLocalizer;
        _openAIClient = openAIClient;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
        _model = options.Value.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        ChatMessage message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        // Persist a ConversationState snapshot so downstream shared steps (e.g. OtpAgent)
        // can use existing state extension helpers.
        await context.SetConversationStateAsync(new ConversationState(message, message.AdditionalProperties![nameof(ConversationState.ConversationId)]!.ToString()!), cancellationToken);

        //var userInput = message.Text ?? string.Empty;
        if (message.AdditionalProperties.TryGetValue<ChatTopic>(nameof(ChatTopic), out var additional) && !string.IsNullOrEmpty(additional.ReferenceType)) {
            await context.Say(Id, _messageLocalizer.OperatorWelcomeKnownCase);
            await context.SendMessageAsync(message, cancellationToken);
            return;
        }

        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }
        // Render the verification prompt using template
        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = AgentsConstants.PromptDefaults.CaseTypeRetriever;
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexCaseRetrieverAgent"
            });

        var response = await agent.RunAsync<List<CaseTypePartial>>("Retrieve all case types.", cancellationToken: cancellationToken);
        var caseTypes = response.Result;
        await context.Say(Id, _messageLocalizer.OperatorWelcomeUknownCase);
        await context.SendMessageAsync(new OperationRequestPort.OperationRequest([.. caseTypes.Select(x => new OperationRequestPort.AllowedOperation(x.Code, x.Title ?? x.Code))]), cancellationToken);
    }
    /// <summary>
    /// Represents a partial case type.
    /// </summary>
    /// <param name="Code">The case type code.</param>
    /// <param name="Title">The case type title.</param>
    /// <param name="Description">The case type description.</param>
    internal record CaseTypePartial(string Code, string? Title, string? Description);
}
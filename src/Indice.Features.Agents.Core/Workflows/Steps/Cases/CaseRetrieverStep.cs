using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Globalization;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Step 1 of the Cases workflow: Retrieves case data from the configured MCP service.
/// The MCP service key is fixed, while the model decides which discovered tool to call.
/// </summary>
public sealed class CaseRetrieverStep : Executor<ConversationState, CaseRetrievalOutput>
{
    private const string McpServiceKey = "Cases";

    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _promptRenderer;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly ICaseDataExtractor _caseDataExtractor;
    private readonly string _model;

    /// <summary>Creates a new <see cref="CaseRetrieverStep"/>.</summary>
    public CaseRetrieverStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry,
        ICaseDataExtractor caseDataExtractor) : base(nameof(CaseRetrieverStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _promptRenderer = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
        _caseDataExtractor = caseDataExtractor;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<CaseRetrievalOutput> HandleAsync(
        ConversationState state,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(state);

        // Persist a ConversationState snapshot so downstream shared steps (e.g. OtpAgent)
        // can use existing state extension helpers.
        await context.SetConversationStateAsync(new ConversationState(state.Message, state.ConversationId), cancellationToken);

        var userInput = state.Message.Text ?? string.Empty;

        if (!_options.Mcp.Services.ContainsKey(McpServiceKey)) {
            throw new InvalidOperationException($"MCP service '{McpServiceKey}' is not configured under Dex:Mcp:Services.");
        }

        var mcpTools = await _mcpToolsRegistry.GetToolsAsync(McpServiceKey, cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }
        // Render the verification prompt using template
        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _promptRenderer.Render(nameof(AgentsConstants.PromptDefaults.CaseRetriever)); 
        chatOptions.Instructions = """
            You are a case retrieval assistant.
            Use the available tool get_case_data_id from the case-retrieval MCP service to fetch case data.
            Decide which tool to call based on the user's query.
            Extract the case GUID from the messages and query the case data.
            Return the case information json as string
            """;
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

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

        var response = await agent.RunAsync<string>(prompt, cancellationToken: cancellationToken);
        var rawPayload = response.Result?.Trim();
        if (string.IsNullOrWhiteSpace(rawPayload)) {
            throw new InvalidOperationException("Case retrieval agent returned empty payload.");
        }

        var normalizedPayload = ExtractJson(rawPayload);
        JsonNode caseData;
        try {
            caseData = JsonNode.Parse(normalizedPayload)
                ?? throw new InvalidOperationException("Case retrieval agent returned invalid JSON payload.");
        } catch (Exception ex) {
            throw new InvalidOperationException("Failed to parse case retrieval payload as JSON.", ex);
        }

        var caseId = _caseDataExtractor.ExtractCaseId(caseData);
        var phoneNumber = _caseDataExtractor.ExtractPhoneNumber(caseData);
        var email = _caseDataExtractor.ExtractEmail(caseData);
        var verificationValue = _caseDataExtractor.ExtractVerificationValue(caseData);
        if (string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email)) {
            throw new InvalidOperationException("No phone number or email found in case data for OTP delivery.");
        }

        return new CaseRetrievalOutput(
            CaseData: caseData,
            CaseId: caseId,
            PhoneNumber: phoneNumber,
            Email: email,
            VerificationValue: verificationValue);
    }

    private static string ExtractJson(string value) {
        if (value.StartsWith("```") && value.Contains("\n")) {
            var firstLineEnd = value.IndexOf('\n');
            var lastFence = value.LastIndexOf("```");
            if (firstLineEnd >= 0 && lastFence > firstLineEnd) {
                return value[(firstLineEnd + 1)..lastFence].Trim();
            }
        }
        return value;
    }
}

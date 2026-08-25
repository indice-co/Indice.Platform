using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
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
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly string _model;

    /// <summary>Creates a new <see cref="CaseRetrieverStep"/>.</summary>
    public CaseRetrieverStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry) : base(nameof(CaseRetrieverStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
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

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = """
            You are a case retrieval assistant.
            Use the available tool get_case_data_id from the case-retrieval MCP service to fetch case data.
            Decide which tool to call based on the user's query.
            Extract the case GUID from the messages and query the case data.
            Return the case object as returned
            """;
        //You must return ONLY the final case JSON object and nothing else.
        //    Required fields in the final JSON: CaseId, PhoneNumber, Email, PlateNumber.
        //    Include all additional case fields returned by tools.
        chatOptions.Tools = [..(chatOptions.Tools ?? []), ..mcpTools];

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
            and return only the response json as string
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

        var caseId = caseData["id"]?.GetValue<string>()
            ?? throw new InvalidOperationException("CaseId not found in case data.");
        var phoneNumber = caseData["data"]["phoneNumber"]?.GetValue<string>();
        var email = caseData["data"]["email"]?.GetValue<string>();
        var verificationValue = caseData["data"]["carPlate"]?.GetValue<string>();
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

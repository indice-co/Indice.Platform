using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Services;
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
internal sealed class DataRetrieverStep : Executor<ConversationState, CaseRetrievalOutput>
{
    private const string McpServiceKey = "Cases";

    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _promptRenderer;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly ICustomerDataResolver _caseDataExtractor;
    private readonly string _model;

    /// <summary>Creates a new <see cref="DataRetrieverStep"/>.</summary>
    public DataRetrieverStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices(McpServiceKey)] IMcpClientFactory mcpClientFactory,
        ICustomerDataResolver caseDataExtractor) : base(nameof(DataRetrieverStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _promptRenderer = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
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
        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }
        // Render the verification prompt using template
        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _promptRenderer.Render(nameof(AgentsConstants.PromptDefaults.CaseRetriever));
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


        JsonNode caseData;
        try {
            caseData = JsonNode.Parse(rawPayload)
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
        var validationResult = _caseDataExtractor.Validate(caseData);
        if (!validationResult.Succeeded) {
            throw new InvalidOperationException($"Case data validation failed: {validationResult.ErrorMessage}");
        }

        return new CaseRetrievalOutput(
            CaseData: caseData,
            CaseId: caseId,
            PhoneNumber: phoneNumber,
            Email: email,
            VerificationValue: verificationValue);
    }
}
/// <summary>
/// Output of the CaseDataRetriever step containing the retrieved case data and related context.
/// </summary>
/// <param name="CaseData">The complete case data as a JsonNode for flexible schema support.</param>
/// <param name="CaseId">The unique identifier for the case.</param>
/// <param name="PhoneNumber">The phone number from case data for OTP delivery (primary channel).</param>
/// <param name="Email">The email from case data (fallback channel if phone unavailable).</param>
/// <param name="VerificationValue">The value the user must use to verify their identity.</param>
public record CaseRetrievalOutput(
    JsonNode CaseData,
    string CaseId,
    string? PhoneNumber,
    string? Email,
    string? VerificationValue);

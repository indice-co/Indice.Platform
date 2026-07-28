using System.Text;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// LLM-powered step that handles the full OTP verification flow for the Cases workflow.
/// Replaces the mechanical <c>OtpSender</c> + <c>OtpValidator</c> pair.
/// <para>
/// The step attaches all tools discovered from the configured <c>"otp"</c> MCP service and
/// delegates the entire multi-turn conversation (ask phone → send OTP → ask code → validate)
/// to the model, guided by the <c>CasesOtpAgent</c> prompt template. The prompt receives the
/// phone number and email already retrieved from the case data so the LLM can act immediately
/// without asking the user for contact details it already has.
/// </para>
/// </summary>
public sealed class OtpAgent : Executor<UserInputValidationOutput, RagPipelineOutput>
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpAgent"/>.</summary>
    public OtpAgent(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry) : base(nameof(OtpAgent)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _prompts = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<RagPipelineOutput> HandleAsync(
        UserInputValidationOutput validationData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        var caseData = validationData.OwnershipVerificationData.CaseRetrievalData;

        // Fetch OTP tools from the "otp" MCP server at runtime — no compile-time coupling.
        var mcpTools = await _mcpToolsRegistry.GetToolsAsync("otp", cancellationToken);

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        // Render prompt template, injecting case contact details so the LLM uses them directly
        // instead of asking the user for information already retrieved from the case data.
        chatOptions.Instructions = _prompts.Render("CasesOtpAgent", new {
            phoneNumber = caseData.PhoneNumber,
            email = caseData.Email,
            caseId = caseData.CaseId,
        });
        if (mcpTools.Count > 0) {
            chatOptions.Tools = [..(chatOptions.Tools ?? []), ..mcpTools];
        }

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpAgent"
            });

        // The conversation state holds the user's current message; this is the entry point
        // into the multi-turn OTP flow managed autonomously by the LLM + MCP tools.
        var conversationState = await context.GetConversationStateAsync(cancellationToken);
        var prompt = conversationState.Message.Text ?? string.Empty;

        // Stream the answer so the client receives live deltas.
        var answer = new StringBuilder();
        UsageDetails? usage = null;
        await foreach (var update in agent.RunStreamingAsync(prompt, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
                await context.AddEventAsync(new AnswerDeltaEvent(update.Text), cancellationToken);
            }
            foreach (var usageContent in update.Contents.OfType<UsageContent>()) {
                (usage ??= new UsageDetails()).Add(usageContent.Details);
            }
        }
        if (usage is not null) {
            await context.AddEventAsync(new Events.UsageEvent(usage, _model), cancellationToken);
        }

        return new RagPipelineOutput { Answer = answer.ToString() };
    }
}

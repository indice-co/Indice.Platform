using System.Text;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Sends an OTP using MCP tools and produces a challenge prompt.
/// The workflow pauses after this step and waits for the user OTP input on a request port.
/// </summary>
public sealed class OtpCodeSendStep : Executor<UserInputValidationOutput, OtpChallengeOutput>
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpCodeSendStep"/>.</summary>
    public OtpCodeSendStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry) : base(nameof(OtpCodeSendStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _prompts = prompts;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpChallengeOutput> HandleAsync(
        UserInputValidationOutput validationData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(validationData);
        var caseData = validationData.OwnershipVerificationData.CaseRetrievalData;

        // Fetch OTP tools from the Identity MCP server at runtime.
        var mcpTools = await _mcpToolsRegistry.GetToolsAsync("Identity", cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException("No MCP tools discovered for service 'Identity'.");
        }

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderInstructions), new {
            phoneNumber = caseData.PhoneNumber,
            email = caseData.Email,
            caseId = caseData.CaseId,
        });
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpAgent"
            });

        // Execute only the send leg now; OTP code collection is done by the workflow host via RequestPort.
        var sendPrompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeSenderPrompt), new {
            phoneNumber =  caseData.PhoneNumber,
            securityToken = caseData.CaseId,
        });
        _ = await agent.RunAsync<string>(sendPrompt, cancellationToken: cancellationToken);
        var maskedPhone = MaskPhone(caseData.PhoneNumber);
        return new OtpChallengeOutput(
            ValidationData: validationData,
            Prompt: $"I sent a verification code to {maskedPhone}. Please enter the OTP you received.",
            PhoneNumber: caseData.PhoneNumber,
            Email: caseData.Email,
            CaseId: caseData.CaseId,
            FailedAttempts: 0,
            MaxFailedAttempts: 2);
    }

    private static string MaskPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone)) {
            return "your registered phone";
        }
        var digits = System.Text.RegularExpressions.Regex.Replace(phone, @"\D", "");
        return digits.Length < 4 ? "your registered phone" : $"***{digits[^4..]}";
    }
}
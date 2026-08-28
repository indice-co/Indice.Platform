using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Verifies a user-provided OTP code using MCP tools and produces the terminal response.
/// </summary>
public sealed class OtpCodeValidatorStep : Executor<OtpCodeResponse, OtpValidationOutput>
{
    private const string McpServiceKey = "Identity";

    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpCodeValidatorStep"/>.</summary>
    public OtpCodeValidatorStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry,
        AgentMessageLocalizer messageLocalizer,
        IPromptTemplateRenderer prompts) : base(nameof(OtpCodeValidatorStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
        _messageLocalizer = messageLocalizer;
        _prompts = prompts;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpValidationOutput> HandleAsync(
        OtpCodeResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(response);

        var code = response.Code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code)) {
            var blankCodeMessage = _messageLocalizer.OtpInputValidationEmpty;
            await context.AddEventAsync(new AnswerDeltaEvent(blankCodeMessage), cancellationToken);
            return new OtpValidationOutput(
                OtpResponse: response,
                IsValid: false,
                Message: blankCodeMessage,
                ShouldRetry: true,
                ShouldResendOtp: false,
                FailedAttempts: response.Challenge.FailedAttempts,
                MaxFailedAttempts: response.Challenge.MaxFailedAttempts);
        }

        var mcpTools = await _mcpToolsRegistry.GetToolsAsync(McpServiceKey, cancellationToken);
        if (mcpTools.Count == 0) {
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorInstructions));
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpCodeValidatorAgent"
            });

        var prompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorPrompt), new {
            code,
            caseId = response.Challenge.CaseId,
            phoneNumber = response.Challenge.PhoneNumber
        });
        var result = await agent.RunAsync<string>(prompt, cancellationToken: cancellationToken);
        var payload = result.Result ?? string.Empty;



        OtpVerificationResultPayload verification;
        try {
            verification = OtpVerificationResultPayload.Deserialize(payload);

        } catch {
            verification = new OtpVerificationResultPayload(false, $"MCP results is not valid:{payload}", false, false, false, 0);
        }

        if (verification.Success) {
            var successMessage = _messageLocalizer.OtvpVerificationSuccessMessage;
            await context.AddEventAsync(new AnswerDeltaEvent(successMessage), cancellationToken);
            return new OtpValidationOutput(
                OtpResponse: response,
                IsValid: true,
                Message: successMessage,
                ShouldRetry: false,
                ShouldResendOtp: false,
                FailedAttempts: response.Challenge.FailedAttempts,
                MaxFailedAttempts: response.Challenge.MaxFailedAttempts);
        }

        var verifyMessage = _messageLocalizer.OtvpVerificationFailedMessage;
        var failedAttempts = response.Challenge.FailedAttempts;
        var maxFailedAttempts = response.Challenge.MaxFailedAttempts;
        var shouldRetry = !verification.IsRateLimited && (failedAttempts <= maxFailedAttempts);
        var finalMessage = shouldRetry
            ? _messageLocalizer.InvalidOtpRetryMessage(Math.Max(maxFailedAttempts - failedAttempts + 1, 0))
            : _messageLocalizer.InvalidOtpMaxAttemptsReachedMessage;
        if (!shouldRetry) {
            await context.AddEventAsync(new AnswerDeltaEvent(finalMessage), cancellationToken);
        }
        return new OtpValidationOutput(
            OtpResponse: response,
            IsValid: false,
            Message: finalMessage,
            ShouldRetry: shouldRetry,
            ShouldResendOtp: false, //TODO: review if we can identify this..
            FailedAttempts: failedAttempts,
            MaxFailedAttempts: maxFailedAttempts);
    }

    private static bool IsExpiredMessage(string value) =>
        value.Contains("expire", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("expired", StringComparison.OrdinalIgnoreCase);
}

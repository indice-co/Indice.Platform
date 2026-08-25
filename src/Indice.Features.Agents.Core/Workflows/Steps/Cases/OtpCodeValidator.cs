using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Mcp;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Verifies a user-provided OTP code using MCP tools and produces the terminal response.
/// </summary>
public sealed class OtpCodeValidator : Executor<OtpCodeResponse, OtpValidationOutput>
{
    private const string McpServiceKey = "Identity";

    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpToolsRegistry _mcpToolsRegistry;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpCodeValidator"/>.</summary>
    public OtpCodeValidator(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        IMcpToolsRegistry mcpToolsRegistry) : base(nameof(OtpCodeValidator)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpToolsRegistry = mcpToolsRegistry;
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
            var blankCodeMessage = "I didn't receive an OTP code. Please enter the OTP sent to your phone.";
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
        chatOptions.Instructions = """
            You are an OTP verifier.
            You MUST call the VerifyTotp tool exactly once.
            Use:
            - securityToken: get securityToken from prompt
            - purpose: "Velmar totp"
            - phoneNumber: get phoneNumber from prompt
            - email: null
            - user: null
            - code: user code from the prompt
            Return true if response indicates TOTP was verified successfully.
            """;
        chatOptions.Tools = [..(chatOptions.Tools ?? []), ..mcpTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpCodeValidatorAgent"
            });

        var prompt = $"Verify this OTP code: {code}, with securityToken:{response.Challenge.CaseId}, phoneNumber: {response.Challenge.PhoneNumber}";
        var result = await agent.RunAsync<string>(prompt, cancellationToken: cancellationToken);
        var payload = ExtractJson(result.Result ?? string.Empty);

        var isValid = false;
        var verifyMessage = "OTP verification failed. Please try again.";
        try {
            var node = JsonNode.Parse(payload) ?? throw new InvalidOperationException("Empty JSON payload.");
            isValid = node["isValid"]?.GetValue<bool>() ?? false;
            verifyMessage = node["message"]?.GetValue<string>() ?? verifyMessage;
        } catch {
            // Be conservative: if the model doesn't return valid JSON, treat as verification failure.
            isValid = false;
        }

        if (isValid) {
            await context.AddEventAsync(new AnswerDeltaEvent(verifyMessage), cancellationToken);
            return new OtpValidationOutput(
                OtpResponse: response,
                IsValid: true,
                Message: verifyMessage,
                ShouldRetry: false,
                ShouldResendOtp: false,
                FailedAttempts: response.Challenge.FailedAttempts,
                MaxFailedAttempts: response.Challenge.MaxFailedAttempts);
        }

        var isExpired = IsExpiredMessage(verifyMessage) || IsExpiredMessage(payload);
        var failedAttempts = response.Challenge.FailedAttempts + (isExpired ? 0 : 1);
        var maxFailedAttempts = response.Challenge.MaxFailedAttempts;
        var shouldRetry = isExpired || failedAttempts <= maxFailedAttempts;
        var finalMessage = shouldRetry
            ? (isExpired
                ? "Your OTP has expired. I will send you a new code now."
                : $"That OTP was not valid. Please try again ({Math.Max(maxFailedAttempts - failedAttempts + 1, 0)} attempt(s) left).")
            : "The OTP code is invalid. You have reached the maximum number of attempts. Please start again.";

        if (!shouldRetry) {
            await context.AddEventAsync(new AnswerDeltaEvent(finalMessage), cancellationToken);
        }

        return new OtpValidationOutput(
            OtpResponse: response,
            IsValid: false,
            Message: finalMessage,
            ShouldRetry: shouldRetry,
            ShouldResendOtp: isExpired,
            FailedAttempts: failedAttempts,
            MaxFailedAttempts: maxFailedAttempts);
    }

    private static bool IsExpiredMessage(string value) =>
        value.Contains("expire", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("expired", StringComparison.OrdinalIgnoreCase);

    private static string ExtractJson(string value) {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("```") && trimmed.Contains('\n')) {
            var firstLineEnd = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```");
            if (firstLineEnd >= 0 && lastFence > firstLineEnd) {
                return trimmed[(firstLineEnd + 1)..lastFence].Trim();
            }
        }
        return trimmed;
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

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
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly string _model;

    /// <summary>Creates a new <see cref="OtpCodeValidatorStep"/>.</summary>
    public OtpCodeValidatorStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices("id")] IMcpClientFactory mcpClientFactory,
        AgentMessageLocalizer messageLocalizer,
        IPromptTemplateRenderer prompts) : base(nameof(OtpCodeValidatorStep)) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
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

        // Fetch OTP tools from the Identity MCP server at runtime.
        var registry = await _mcpClientFactory.CreateAsync();
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
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
        var payload = result.Text;

        response.Code = string.Empty; // Clear the code from the response for security reasons.

        OtpVerificationResultPayload verification;
        try {
            verification = OtpVerificationResultPayload.Deserialize(payload);

        } catch (JsonException) {
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

        var failedAttempts = response.Challenge.FailedAttempts + 1;
        var maxFailedAttempts = response.Challenge.MaxFailedAttempts;
        var shouldRetry = !verification.IsRateLimited && failedAttempts <= maxFailedAttempts;
        var finalMessage = shouldRetry
            ? _messageLocalizer.InvalidOtpRetryMessage(Math.Max(maxFailedAttempts - failedAttempts, 0))
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
}

/// <summary>
/// Workflow event emitted by LLM agent steps for each streamed text token.
/// Surfaced as an SSE <c>delta</c> frame by the streaming runner; ignored by the non-streaming runner.
/// </summary>
/// <param name="Text">The text delta emitted by the model.</param>
public sealed class AnswerDeltaEvent(string Text) : WorkflowEvent
{
    /// <summary>The text delta emitted by the model.</summary>
    public string Text { get; } = Text;
}

/// <summary>
/// Response payload delivered to the OTP verification request port when the user submits the received OTP code.
/// </summary>
public class OtpCodeResponse
{
    /// <summary>The pending OTP challenge emitted by the send step..</summary>
    public OtpChallengeOutput Challenge { get; }
    /// <summary>The OTP code provided by the user.</summary>
    public string Code { get; set; }
    /// <summary>Creates a new <see cref="OtpCodeResponse"/>.</summary>
    public OtpCodeResponse(OtpChallengeOutput challenge, string code)
    {
        Challenge = challenge;
        Code = code;
    }
}

/// <summary>
/// Output of OTP code verification, preserving full workflow context for downstream steps.
/// </summary>
/// <param name="OtpResponse">The OTP response envelope containing challenge and case context.</param>
/// <param name="IsValid">Whether the OTP was successfully verified.</param>
/// <param name="Message">User-facing verification message.</param>
/// <param name="ShouldRetry">Whether the workflow should ask for OTP input again.</param>
/// <param name="ShouldResendOtp">Whether a new OTP should be sent before asking again.</param>
/// <param name="FailedAttempts">Number of invalid OTP attempts so far.</param>
/// <param name="MaxFailedAttempts">Maximum invalid OTP attempts allowed.</param>
public record OtpValidationOutput(
    OtpCodeResponse OtpResponse,
    bool IsValid,
    string Message,
    bool ShouldRetry,
    bool ShouldResendOtp,
    int FailedAttempts,
    int MaxFailedAttempts);



/// <summary>
/// Raw OTP verification payload returned by the MCP verification tool.
/// </summary>
public sealed record OtpVerificationResultPayload(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("isRateLimited")] bool IsRateLimited,
    [property: JsonPropertyName("isInvalidCode")] bool IsInvalidCode,
    [property: JsonPropertyName("isInvalidFormat")] bool IsInvalidFormat,
    [property: JsonPropertyName("totpLifetime")] int TotpLifetime)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes a JSON OTP verification payload.
    /// </summary>
    /// <param name="json">The JSON payload to parse.</param>
    /// <returns>A strongly typed OTP verification payload.</returns>
    public static OtpVerificationResultPayload Deserialize(string json) =>
        JsonSerializer.Deserialize<OtpVerificationResultPayload>(json, SerializerOptions)
        ?? throw new InvalidOperationException("Empty JSON payload.");
}

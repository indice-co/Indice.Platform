using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
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
/// Verifies a user-provided OTP code using MCP tools and produces the terminal response.
/// </summary>
[SendsMessage(typeof(OtpRequestPort.OtpRequest))]
[SendsMessage(typeof(OperationState))]
[YieldsOutput(typeof(OperationState))]
public class OtpCodeValidatorStep : Executor<OtpRequestPort.OtpResponse>
{
    private const string McpServiceKey = "Identity";

    private readonly int _maxValidationAttempts;
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly string _model;
    private readonly ILogger<OtpCodeValidatorStep> _logger;

    /// <summary>Creates a new <see cref="OtpCodeValidatorStep"/>.</summary>
    public OtpCodeValidatorStep(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<CustomerWorkflowOptions> customerWorkflowOptions,
        IOptions<ModelsOptions> models,
        UserClaimsAIContextProvider userClaimsProvider,
        [FromKeyedServices("id")] IMcpClientFactory mcpClientFactory,
        AgentMessageLocalizer messageLocalizer,
        IPromptTemplateRenderer prompts,
        ILogger<OtpCodeValidatorStep> logger) : base(nameof(OtpCodeValidatorStep)) {
        _logger = logger;
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _userClaimsProvider = userClaimsProvider;
        _mcpClientFactory = mcpClientFactory;
        _messageLocalizer = messageLocalizer;
        _prompts = prompts;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
        _maxValidationAttempts = customerWorkflowOptions.Value.MaxOtpValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        OtpRequestPort.OtpResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(response);

        if (string.IsNullOrWhiteSpace(response.Otp?.Trim())) {
            await context.Say(Id, _messageLocalizer.OtpInputValidationEmpty);
            await context.SendMessageAsync(new OtpRequestPort.OtpRequest(response.ChallengeCode, DateTime.UtcNow));
            return;
        }

        var workflowData = await context.GetOperatorStateAsync(cancellationToken);
        //var otpVerificationResult = new OtpVerificationResultPayload(true, null, false, false, false, 0);
        var otpVerificationResult = await ValidateOtp(response, workflowData, cancellationToken);
        if (otpVerificationResult.Success) {
            await context.SetApprovalStateAsync(null, cancellationToken);
            await context.Say(Id, _messageLocalizer.OtvpVerificationSuccessMessage);
            await context.SendMessageAsync(OperationState.Next(nameof(DataPresenterStep)));
            return;
        }

        var attempt = (await context.GetApprovalStateAsync(cancellationToken)) + 1;
        await context.SetApprovalStateAsync(attempt, cancellationToken);
        if (attempt >= _maxValidationAttempts) {
            await context.SetApprovalStateAsync(null, cancellationToken);
            await context.Say(Id, _messageLocalizer.InvalidOtpMaxAttemptsReachedMessage);
            await context.YieldOutputAsync(OperationState.End);
            return;
        }

        _logger.LogInformation("Invalid OTP for case {ReferenceId} (attempt {Attempt}/{MaxAttempts}, rate limited: {IsRateLimited}). Asking user to retry.", workflowData.ReferenceId, attempt, _maxValidationAttempts, otpVerificationResult.IsRateLimited);
        await context.Say(Id, _messageLocalizer.InvalidOtpRetryMessage(Math.Max(_maxValidationAttempts - attempt, 0)));
        await context.SendMessageAsync(new OtpRequestPort.OtpRequest(response.ChallengeCode, DateTime.UtcNow));

        //var shouldRetry = !verification.IsRateLimited && attempt<= _maxValidationAttempts;
        //var finalMessage = shouldRetry
        //    ? _messageLocalizer.InvalidOtpRetryMessage(Math.Max(_maxValidationAttempts - attempt, 0))

        //if(verification.IsRateLimited) {
        //    finalMessage = _messageLocalizer.InvalidOtpRateLimitMessage;
        //}
        //if (!shouldRetry) {
        //    await context.AddEventAsync(new AnswerDeltaEvent(finalMessage), cancellationToken);
        //}

    }

    private async Task<OtpVerificationResultPayload> ValidateOtp(OtpRequestPort.OtpResponse response, CustomerData workflowData, CancellationToken cancellationToken) {

        // Fetch OTP tools from the Identity MCP server at runtime.
        var registry = await _mcpClientFactory.CreateAsync(cancellationToken);
        var mcpTools = await registry.ListToolsAsync(options: null, cancellationToken);
        if (mcpTools.Count == 0) {
            _logger.LogError("No MCP tools discovered for service {McpServiceKey}.", McpServiceKey);
            throw new InvalidOperationException($"No MCP tools discovered for service '{McpServiceKey}'.");
        }

        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorInstructions));
        // Wrap MCP tools so the raw output is captured here and never sent back to the model.
        var capturingTools = mcpTools.Select(t => new McpCapturingToolFunction(t)).ToList();
        chatOptions.Tools = [.. (chatOptions.Tools ?? []), .. capturingTools];

        var agent = _openAIClient
            .GetChatClient(_model)
            .AsIChatClient()
            .AsAIAgent(options: new ChatClientAgentOptions {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexOtpCodeValidatorAgent"
            });

        var useSms = workflowData.UsesSmsForOtp;
        var prompt = _prompts.Render(nameof(AgentsConstants.PromptDefaults.OtpCodeValidatorPrompt), new {
            otp = response.Otp.Trim(),
            caseId = response.ChallengeCode,
            phoneNumber = useSms ? workflowData.PhoneNumber : null,
            email = useSms ? null : workflowData.Email
        });
        await agent.RunAsync(prompt, cancellationToken: cancellationToken);
        // Use the raw MCP tool output, not the LLM re-generated JSON, which can corrupt values.
        if (capturingTools.TryGetToolResult(out var otpVerificationResult, out var error)) {
            try {
                return otpVerificationResult is null ? new OtpVerificationResultPayload(false, "MCP result is not valid: null", false, false, false, 0)
                    : OtpVerificationResultPayload.Deserialize(otpVerificationResult.ToJsonString());
            } catch (JsonException ex) {
                _logger.LogError(ex, "OTP validation tool returned an invalid payload for case {ReferenceId}.", workflowData.ReferenceId);
                return new OtpVerificationResultPayload(false, $"MCP result is not valid: {otpVerificationResult}", false, false, false, 0);
            }
        }
        _logger.LogError("Case data retrieval failed for service {McpServiceKey}: {Error}", McpServiceKey, error);
        throw new InvalidOperationException($"Case data retrieval failed for service '{McpServiceKey}': {error}");
    }
}



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
